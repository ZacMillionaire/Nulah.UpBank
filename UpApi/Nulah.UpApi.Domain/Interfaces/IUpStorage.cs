using System.Linq.Expressions;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Domain.Models.Transactions;

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

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1. Must be greater than 0.</param>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheAsync(
		int pageSize,
		int pageNumber = 1,
		Expression<Func<UpTransaction, bool>>? queryExpression = null);

	/// <summary>
	/// Returns a stat object for stored transactions
	/// </summary>
	/// <returns></returns>
	Task<TransactionCacheStats> GetTransactionStats();

	/// <summary>
	/// Saves the given transactions to storage
	/// </summary>
	/// <param name="transactions"></param>
	Task SaveTransactionsToCacheAsync(IEnumerable<UpTransaction> transactions);
}