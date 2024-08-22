using Nulah.UpApi.Domain.Models;

namespace Nulah.UpApi.Domain.Interfaces;

public interface IUpStorage
{
	/// <summary>
	/// Returns all accounts from the cache.
	/// </summary>
	/// <returns></returns>
	Task<IReadOnlyList<UpAccount>> LoadAccountsFromCacheAsync();

	/// <summary>
	/// Saves the given accounts to storage
	/// </summary>
	/// <param name="accounts"></param>
	Task SaveAccountsToCacheAsync(IEnumerable<UpAccount> accounts);

	/// <summary>
	/// Saves the given account to storage
	/// </summary>
	/// <param name="accounts"></param>
	Task SaveAccountToCacheAsync(UpAccount accounts);

	/// <summary>
	/// Returns an account by Id, or null if not found
	/// </summary>
	/// <param name="accountId"></param>
	/// <returns></returns>
	Task<UpAccount?> GetAccountFromCacheAsync(string accountId);
}