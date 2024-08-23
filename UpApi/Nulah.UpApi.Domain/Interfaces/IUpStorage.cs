using System.Linq.Expressions;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Domain.Models.Transactions;
using Nulah.UpApi.Domain.Models.Transactions.Criteria;

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
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1. Must be greater than 0.</param>
	/// <param name="queryCriteria">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheByCriteriaAsync(
		int pageSize,
		int pageNumber = 1,
		TransactionQueryCriteria? queryCriteria = null);

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

	/// <summary>
	/// Returns all categories from the cache - does not populate parent categories.
	/// </summary>
	/// <returns></returns>
	Task<IReadOnlyList<UpCategory>> LoadCategoriesFromCacheAsync();

	/// <summary>
	/// Saves the given categories to storage
	/// </summary>
	/// <param name="categories"></param>
	Task SaveCategoriesToCacheAsync(IEnumerable<UpCategory> categories);

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheAsync(Expression<Func<UpTransaction, bool>>? queryExpression = null);

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheAsync(TransactionQueryCriteria? queryExpression = null);
}