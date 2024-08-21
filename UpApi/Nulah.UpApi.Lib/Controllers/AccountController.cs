using Marten;
using Marten.Linq.LastModified;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Models;

namespace Nulah.UpApi.Lib.Controllers;

public class AccountController
{
	private readonly IUpBankApi _upBankApi;
	private readonly IDocumentStore _documentStore;
	private readonly ILogger<AccountController> _logger;

	public Action<AccountController, EventArgs>? AccountsUpdating;
	public Action<AccountController, IReadOnlyList<UpAccount>>? AccountsUpdated;

	public AccountController(IUpBankApi upBankApi, IDocumentStore documentStore, ILogger<AccountController> logger)
	{
		_upBankApi = upBankApi;
		_documentStore = documentStore;
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

		await using var session = _documentStore.LightweightSession();

		if (!bypassCache)
		{
			_logger.LogInformation("Attempting to load from cache");
			var existingAccounts = await LoadAccountsFromCacheAsync(session);

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
		session.Store((IEnumerable<UpAccount>)accounts);
		await session.SaveChangesAsync();

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
	public async Task<UpAccount> GetAccount(string accountId)
	{
		try
		{
			await using var session = _documentStore.LightweightSession();
			var account = await session.Query<UpAccount>()
				.FirstOrDefaultAsync(x =>
					x.Id == accountId
					// only return an account if it's "fresh" which is a modified date less than a day old (currently)
					// TODO: configure this
					&& x.ModifiedBefore(DateTime.UtcNow.AddDays(1))
				);

			if (account == null)
			{
				var accounts = await _upBankApi.GetAccount(accountId);

				if (accounts is { Success: true, Response: not null })
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

					session.Store(account);
					await session.SaveChangesAsync();
				}
			}

			return account;
		}
		catch
		{
			throw;
		}
	}

	/// <summary>
	/// Returns all accounts from the cache.
	/// </summary>
	/// <param name="documentSession"></param>
	/// <returns></returns>
	private Task<IReadOnlyList<UpAccount>> LoadAccountsFromCacheAsync(IDocumentSession documentSession)
	{
		return documentSession.Query<UpAccount>()
			.OrderByDescending(x => x.AccountType)
			.ThenBy(x => x.Id)
			.ToListAsync();
	}
}