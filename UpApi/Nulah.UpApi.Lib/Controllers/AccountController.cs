using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;

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
	/// Returns the account summary by given <paramref name="accountId"/>
	/// </summary>
	/// <param name="accountId"></param>
	/// <returns></returns>
	public async Task<UpAccount?> GetAccount(string accountId)
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