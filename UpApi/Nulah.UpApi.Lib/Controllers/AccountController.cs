using System.Globalization;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Domain.Models.Transactions;
using Nulah.UpApi.Domain.Models.Transactions.Criteria;

namespace Nulah.UpApi.Lib.Controllers;

public class AccountController
{
	private readonly IUpBankApi _upBankApi;
	private readonly IUpStorage _upStorage;
	private readonly ILogger<AccountController> _logger;

	public Action<AccountController, EventArgs>? AccountsUpdating;
	public Action<AccountController, IReadOnlyList<UpAccount>>? AccountsUpdated;

	public AccountController(IUpBankApi upBankApi, IUpStorage upStorage, ILogger<AccountController> logger)
	{
		_upBankApi = upBankApi;
		_upStorage = upStorage;
		_logger = logger;
	}

	/// <summary>
	///	Retrieves all accounts from the Up Api.
	///
	/// If this is the first time called, no accounts are in the database, or <paramref name="bypassCache"/> is true,
	/// the accounts will be first retrieved from the Up Api then cached nad returned.
	/// Otherwise the results will be returned from the cache. 
	/// </summary>
	/// <param name="bypassCache"></param>
	/// <returns></returns>
	public async Task<IReadOnlyList<UpAccount>> GetAccounts(bool bypassCache = false)
	{
		_logger.LogInformation("Getting Accounts");
		AccountsUpdating?.Invoke(this, EventArgs.Empty);


		if (!bypassCache)
		{
			_logger.LogInformation("Attempting to load from cache");
			var existingAccounts = await _upStorage.LoadAccountsFromCacheAsync();

			// We duplicate code slightly here to retrieve accounts from the Api if _no_ accounts
			// exist in the database.
			// This means that if the user has no accounts ever (unlikely as an Up customer should always have a spending account)
			// this method will potentially hit the Api every single call.
			// I don't really care about that though, if the user has no accounts then the Up Api will be doing no
			// work to return nothing so ¯\_(ツ)_/¯
			if (existingAccounts.Count != 0)
			{
				_logger.LogInformation("Loaded {accounts} accounts from cache", existingAccounts.Count);
				AccountsUpdated?.Invoke(this, existingAccounts);
				return existingAccounts;
			}
		}

		var accounts = await GetAccountsFromApi();

		_logger.LogInformation("Saving {accounts} accounts to cache", accounts.Count);
		await _upStorage.SaveAccountsToCacheAsync(accounts);

		AccountsUpdated?.Invoke(this, accounts);
		return accounts;
	}

	private async Task<List<UpAccount>> GetAccountsFromApi(string? nextPage = null)
	{
		_logger.LogInformation("Retrieving accounts directly from the API");
		var accounts = new List<UpAccount>();
		var apiResponse = await _upBankApi.GetAccounts(nextPage);

		if (apiResponse is { Success: true, Response: not null })
		{
			_logger.LogDebug("Received page of accounts from API");
			accounts.AddRange(
				apiResponse.Response.Data
					.Select(x => new UpAccount()
					{
						Balance = x.Attributes.Balance,
						Id = x.Id,
						Type = x.Type,
						AccountType = x.Attributes.AccountType,
						CreatedAt = x.Attributes.CreatedAt,
						DisplayName = x.Attributes.DisplayName,
						OwnershipType = x.Attributes.OwnershipType
					})
			);

			if (!string.IsNullOrWhiteSpace(apiResponse.Response.Links.Next))
			{
				_logger.LogDebug("Received next page from the API");
				await GetAccountsFromApi(apiResponse.Response.Links.Next);
			}
		}

		_logger.LogInformation("Retrieved {accounts} from the API", accounts.Count);

		return accounts;
	}

	/// <summary>
	/// Returns the account summary by given <paramref name="accountId"/>, or null if not found
	/// </summary>
	/// <param name="accountId"></param>
	/// <returns></returns>
	public async Task<UpAccount?> GetAccount(string accountId)
	{
		return await GetAccountAsync(accountId);
	}

	/// <summary>
	/// Returns an aggregate of totals by day, given a start and end date.
	/// <para>
	/// If no account is found, an empty list is returned. This method is guaranteed to return a list, but it is not
	/// guaranteed to contain data if an account has no transactions within the given date range
	/// </para>
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="excludeUncategorisableTransactions">Defaults to true</param>
	/// <param name="transactionTypes"></param>
	/// <returns></returns>
	public async Task<List<TransactionDateAggregate>> GetAccountStats(string accountId,
		DateTimeOffset since,
		DateTimeOffset until,
		bool? excludeUncategorisableTransactions = true,
		IEnumerable<TransactionType>? transactionTypes = null)
	{
		var account = await GetAccountAsync(accountId);
		if (account != null)
		{
			var accountTransactions = await _upStorage.LoadTransactionsFromCacheAsync(new TransactionQueryCriteria()
			{
				Since = since,
				Until = until,
				AccountId = accountId,
				ExcludeUncategorisableTransactions = excludeUncategorisableTransactions ?? true,
				TransactionTypes = transactionTypes?.ToList() ?? []
			});

			var aggregate = accountTransactions.GroupBy(x =>
					x.CreatedAt.ToString("yyyy-MM-dd")
				)
				.Select(x => new TransactionDateAggregate
				{
					Date = DateTime.ParseExact(x.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture),
					Total = x.Sum(y => y.Amount.ValueInBaseUnits) / 100.0
				});

			return aggregate.ToList();
		}

		return new List<TransactionDateAggregate>();
	}

	/// <summary>
	/// Returns the details of an account by its Id, if nothing is found in the cache it will attempt to be cached from the Api.
	/// <para>
	/// If the account is not found in either the cache or the Api, null is returned.
	/// </para>
	/// </summary>
	/// <param name="accountId"></param>
	/// <returns></returns>
	private async Task<UpAccount?> GetAccountAsync(string accountId)
	{
		try
		{
			var account = await _upStorage.GetAccountFromCacheAsync(accountId);

			if (account == null)
			{
				var accounts = await _upBankApi.GetAccount(accountId);

				if (accounts is { Success: true, Response: not null, Response.Data: not null })
				{
					account = new UpAccount()
					{
						Balance = accounts.Response.Data.Attributes.Balance,
						Id = accounts.Response.Data.Id,
						Type = accounts.Response.Data.Type,
						AccountType = accounts.Response.Data.Attributes.AccountType,
						CreatedAt = accounts.Response.Data.Attributes.CreatedAt,
						DisplayName = accounts.Response.Data.Attributes.DisplayName,
						OwnershipType = accounts.Response.Data.Attributes.OwnershipType
					};

					await _upStorage.SaveAccountToCacheAsync(account);
				}
			}

			return account;
		}
		catch
		{
			throw;
		}
	}
}