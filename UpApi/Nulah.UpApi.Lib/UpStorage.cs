using Marten;
using Marten.Linq.LastModified;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;

namespace Nulah.UpApi.Lib;

public class UpStorage : IUpStorage
{
	private readonly IDocumentStore _documentStore;
	private readonly ILogger<UpStorage> _logger;

	public UpStorage(IDocumentStore documentStore, ILogger<UpStorage> logger)
	{
		_documentStore = documentStore;
		_logger = logger;
	}

	/// <summary>
	/// Returns an account by Id, or null if not found
	/// </summary>
	/// <param name="accountId"></param>
	/// <returns></returns>
	public async Task<UpAccount?> GetAccountFromCacheAsync(string accountId)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			_logger.LogInformation("Retrieving account by Id {accountId}", accountId);
			return await session.Query<UpAccount>()
				.FirstOrDefaultAsync(x =>
					x.Id == accountId
					// Only return an account if it's "fresh" which is a modified date less than a day old (currently).
					// This date is MartedDb specific, so if we find no account
					// TODO: configure this
					&& x.ModifiedBefore(DateTime.UtcNow.AddDays(1))
				);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to retrieve account. {ex}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Returns all accounts from the cache.
	/// </summary>
	/// <returns></returns>
	public async Task<IReadOnlyList<UpAccount>> LoadAccountsFromCacheAsync()
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			return await session.Query<UpAccount>()
				.OrderByDescending(x => x.AccountType)
				.ThenBy(x => x.Id)
				.ToListAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load accounts. {ex}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Saves the given accounts to storage
	/// </summary>
	/// <param name="accounts"></param>
	public async Task SaveAccountsToCacheAsync(IEnumerable<UpAccount> accounts)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			session.Store(accounts);

			_logger.LogDebug("Saving changes");
			await session.SaveChangesAsync();

			_logger.LogInformation("Accounts saved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save accounts. {ex}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Saves the given account to storage
	/// </summary>
	/// <param name="accounts"></param>
	public async Task SaveAccountToCacheAsync(UpAccount accounts)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			session.Store(accounts);

			_logger.LogDebug("Saving changes");
			await session.SaveChangesAsync();
			
			_logger.LogInformation("Account saved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save account. {ex}", ex.Message);
			throw;
		}
	}
}