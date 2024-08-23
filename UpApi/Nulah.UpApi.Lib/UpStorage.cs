using System.Linq.Expressions;
using Marten;
using Marten.Linq.LastModified;
using Marten.Pagination;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Domain.Models.Transactions;
using Nulah.UpApi.Domain.Models.Transactions.Criteria;
using Nulah.UpApi.Lib.Controllers;

namespace Nulah.UpApi.Lib;

public class UpStorage : IUpStorage
{
	private const int DefaultPageSize = 25;
	private readonly IDocumentStore _documentStore;
	private readonly ILogger<UpStorage> _logger;

	public UpStorage(IDocumentStore documentStore, ILogger<UpStorage> logger)
	{
		_documentStore = documentStore;
		_logger = logger;
	}

	#region Accounts

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

	#endregion

	#region Transactions

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1. Must be greater than 0.</param>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	public async Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheAsync(
		int pageSize = DefaultPageSize,
		int pageNumber = 1,
		Expression<Func<UpTransaction, bool>>? queryExpression = null)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			// Comment copy and pasted from refactoring. I'm leaving it here to confuse me later, but this may be a feature
			// as it's "planned" https://github.com/up-banking/api/issues/99. Of course they move very slow with this API
			// because I'm 99% sure they've all but put it on maintenance mode internally but we can hope!
			// Also this comment is here as I might shift over to EF still, as MartenDb is super convenient for dev but EF
			// gives me better query support in the future.
			// TODO: investigate moving to EF for storage of transaction information so we can reduce zero sum transaction pairs
			// eg: a cover will generate a pair that can be matched by CreatedBy that count as no difference and can potentially be excluded.
			// For now a criteria will be used to simply exclude uncategorisable transactions, as a future API update (if one ever happens...),
			// could expose where a transaction was covered and these uncategorisable transactions may not come through, or may have additional
			// metadata that can allow me to better display them.

			_logger.LogInformation("Retrieving transaction page {pageNumber}, paged by {pagesize}", pageNumber, pageSize);
			return await session.Query<UpTransaction>()
				.Where(queryExpression ?? (x => true))
				.OrderByDescending(x => x.CreatedAt)
				.ToPagedListAsync(pageNumber, pageSize);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to retrieve transactions: {exceptionMessage}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1. Must be greater than 0.</param>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	public async Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheByCriteriaAsync(
		int pageSize = DefaultPageSize,
		int pageNumber = 1,
		TransactionQueryCriteria? queryExpression = null)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			// Comment copy and pasted from refactoring. I'm leaving it here to confuse me later, but this may be a feature
			// as it's "planned" https://github.com/up-banking/api/issues/99. Of course they move very slow with this API
			// because I'm 99% sure they've all but put it on maintenance mode internally but we can hope!
			// Also this comment is here as I might shift over to EF still, as MartenDb is super convenient for dev but EF
			// gives me better query support in the future.
			// TODO: investigate moving to EF for storage of transaction information so we can reduce zero sum transaction pairs
			// eg: a cover will generate a pair that can be matched by CreatedBy that count as no difference and can potentially be excluded.
			// For now a criteria will be used to simply exclude uncategorisable transactions, as a future API update (if one ever happens...),
			// could expose where a transaction was covered and these uncategorisable transactions may not come through, or may have additional
			// metadata that can allow me to better display them.

			_logger.LogInformation("Retrieving transaction page {pageNumber}, paged by {pagesize}", pageNumber, pageSize);
			return await session.Query<UpTransaction>()
				.Where(BuildTransactionQuery(queryExpression))
				.OrderByDescending(x => x.CreatedAt)
				.ToPagedListAsync(pageNumber, pageSize);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to retrieve transactions: {exceptionMessage}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	public async Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheAsync(Expression<Func<UpTransaction, bool>>? queryExpression = null)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			// Comment copy and pasted from refactoring. I'm leaving it here to confuse me later, but this may be a feature
			// as it's "planned" https://github.com/up-banking/api/issues/99. Of course they move very slow with this API
			// because I'm 99% sure they've all but put it on maintenance mode internally but we can hope!
			// Also this comment is here as I might shift over to EF still, as MartenDb is super convenient for dev but EF
			// gives me better query support in the future.
			// TODO: investigate moving to EF for storage of transaction information so we can reduce zero sum transaction pairs
			// eg: a cover will generate a pair that can be matched by CreatedBy that count as no difference and can potentially be excluded.
			// For now a criteria will be used to simply exclude uncategorisable transactions, as a future API update (if one ever happens...),
			// could expose where a transaction was covered and these uncategorisable transactions may not come through, or may have additional
			// metadata that can allow me to better display them.

			_logger.LogInformation("Retrieving transactions unpaged");
			return await session.Query<UpTransaction>()
				.Where(queryExpression ?? (x => true))
				.OrderByDescending(x => x.CreatedAt)
				.ToListAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to retrieve transactions: {exceptionMessage}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="queryExpression">
	/// Defaults to ((UpTransaction)x => true) if null, returning all transactions unfiltered
	/// </param>
	/// <returns></returns>
	public async Task<IEnumerable<UpTransaction>> LoadTransactionsFromCacheAsync(TransactionQueryCriteria? queryExpression = null)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			// Comment copy and pasted from refactoring. I'm leaving it here to confuse me later, but this may be a feature
			// as it's "planned" https://github.com/up-banking/api/issues/99. Of course they move very slow with this API
			// because I'm 99% sure they've all but put it on maintenance mode internally but we can hope!
			// Also this comment is here as I might shift over to EF still, as MartenDb is super convenient for dev but EF
			// gives me better query support in the future.
			// TODO: investigate moving to EF for storage of transaction information so we can reduce zero sum transaction pairs
			// eg: a cover will generate a pair that can be matched by CreatedBy that count as no difference and can potentially be excluded.
			// For now a criteria will be used to simply exclude uncategorisable transactions, as a future API update (if one ever happens...),
			// could expose where a transaction was covered and these uncategorisable transactions may not come through, or may have additional
			// metadata that can allow me to better display them.

			_logger.LogInformation("Retrieving transactions unpaged");
			return await session.Query<UpTransaction>()
				.Where(BuildTransactionQuery(queryExpression))
				.OrderByDescending(x => x.CreatedAt)
				.ToListAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to retrieve transactions: {exceptionMessage}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Returns a stat object for stored transactions
	/// </summary>
	/// <returns></returns>
	public async Task<TransactionCacheStats> GetTransactionStats()
	{
		// This is copy pasted after the refactoring from TransactionController
		// I still think its all gross.
		// At least it still works until I revisit this feature in the frontend.
		await using var session = _documentStore.LightweightSession();

		var statQueryBatch = session.CreateBatchQuery();
		var transactionsCached = statQueryBatch.Query<UpTransaction>().Count();
		var firstTransaction = statQueryBatch.Query<UpTransaction>().Min(x => x.CreatedAt);
		var latestTransaction = statQueryBatch.Query<UpTransaction>().Max(x => x.CreatedAt);

		// Wow this looks like a disgusting mess!
		// The answer is yes, this is me doing things with Marten that would be better off handled with EF.
		// No I refuse to learn my lesson and I'll mark my tombstone with a TODO shortly.
		// What this query basically does is collect all counts by category, but first lumps transactions that can't be
		// categorised (these are generally bonus, interest, or transfers or covers between accounts) as 'uncategorisable',
		// those that can be categorised but aren't categorised (because a user simply forgot or somehow caching failed to populate an id),
		// and then finally the CategoryId.
		// I've used double quotes for the column aliases to ensure the column names are the correct case to match with the type properties,
		// turning every row into json, and then letting Marten take the wheel to turn it into what we actually want
		// If god isn't dead this query is a testament to why he is.
		// TODO: Update this to something sane when I eventually separate things to EF
		var categoryStats = statQueryBatch.Query<CategoryStat>("""
		                                                       SELECT row_to_json(result)
		                                                       FROM (SELECT CASE
		                                                                        WHEN (d.data -> 'Category' -> 'Id') IS NULL AND d.data -> 'IsCategorizable' = 'false'
		                                                                            THEN 'uncategorisable'
		                                                                        WHEN (d.data -> 'Category' -> 'Id') IS NULL AND d.data -> 'IsCategorizable' = 'true'
		                                                                            THEN 'uncategorised'
		                                                                        ELSE (d.data -> 'Category' ->> 'Id')
		                                                           END               AS "CategoryId"
		                                                                  , CASE
		                                                                        WHEN (c.data -> 'Name') IS NULL AND d.data -> 'IsCategorizable' = 'false'
		                                                                            THEN 'Uncategorisable'
		                                                                        WHEN (c.data -> 'Name') IS NULL AND d.data -> 'IsCategorizable' = 'true'
		                                                                            THEN 'Uncategorised'
		                                                                        ELSE (c.data ->> 'Name')
		                                                               END           AS "Name"
		                                                                  , count(*) AS "Count"
		                                                             FROM mt_doc_uptransaction AS d
		                                                                      -- the id column for the category table/document is the category id from Up (ie - it's not a guid)
		                                                                      LEFT JOIN mt_doc_upcategory as c ON (d.data -> 'Category' ->> 'Id') = c.id
		                                                             GROUP BY "CategoryId", "Name") AS result
		                                                       """);
		await statQueryBatch.Execute();

		return new TransactionCacheStats()
		{
			Count = await transactionsCached,
			// If we have no transaction dates (due to nothing being cached), we'll need to return null, and we can't default the
			// above to null as marten cannot return null for scalar DateTime.
			// These queries are a bit gross as they're a batch query and we await on the result of each.
			// We also check if the date back is a min value and also return null in those cases.
			MostRecentTransactionDate = await latestTransaction is var lastDate && lastDate == DateTime.MinValue
				? null
				: lastDate,
			FirstTransactionDate = await firstTransaction is var firstDate && firstDate == DateTime.MinValue
				? null
				: firstDate,
			CategoryStats = await categoryStats
		};
	}

	/// <summary>
	/// Saves the given transactions to storage
	/// </summary>
	/// <param name="transactions"></param>
	public async Task SaveTransactionsToCacheAsync(IEnumerable<UpTransaction> transactions)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			session.Store(transactions);
			await session.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save transactions: {exceptionMessage}", ex.Message);
			throw;
		}
	}


	/// <summary>
	/// Creates a predicate for linq to sql to filter transactions as appropriate.
	/// <para>
	/// Calling this method with no criteria results in a query that will return all results, excluding transactions that cannot be categorised.
	/// </para>
	/// </summary>
	/// <param name="transactionQueryCriteria"></param>
	/// <returns></returns>
	private static Expression<Func<UpTransaction, bool>> BuildTransactionQuery(TransactionQueryCriteria? transactionQueryCriteria)
	{
		// TODO: move this to its own criteria class maybe?
		// Set criteria to a new instance if null is given
		transactionQueryCriteria ??= new TransactionQueryCriteria();

		Expression<Func<UpTransaction, bool>>? baseFunc = null;

		if (!string.IsNullOrWhiteSpace(transactionQueryCriteria.AccountId))
		{
			baseFunc = baseFunc.And(x => x.AccountId == transactionQueryCriteria.AccountId);
		}

		if (transactionQueryCriteria.Since.HasValue)
		{
			baseFunc = baseFunc.And(x => transactionQueryCriteria.Since.Value.ToUniversalTime() <= x.CreatedAt);
		}

		if (transactionQueryCriteria.Until.HasValue)
		{
			baseFunc = baseFunc.And(x => x.CreatedAt <= transactionQueryCriteria.Until.Value.ToUniversalTime());
		}

		// This defaults to false, so default criteria behaviour should return all transactions that are not covers.
		// A cover transaction is a zero-sum between 2 accounts where the parent is a users spending account.
		// This should not affect any cache stats as these do not use this method for query building.
		if (transactionQueryCriteria.ExcludeUncategorisableTransactions)
		{
			baseFunc = baseFunc.And(x => x.IsCategorizable);
		}

		if (transactionQueryCriteria.TransactionTypes.Count > 0)
		{
			Expression<Func<UpTransaction, bool>>? transactionTypeQuery = null;

			foreach (var transactionType in transactionQueryCriteria.TransactionTypes)
			{
				transactionTypeQuery = transactionTypeQuery.Or(x => x.InferredType == transactionType);
			}

			transactionTypeQuery ??= x => true;

			if (transactionTypeQuery.CanReduce)
			{
				transactionTypeQuery.Reduce();
			}

			baseFunc = baseFunc.And(transactionTypeQuery);
		}

		// Return an "empty" expression if we have a criteria object, but no criteria to act on
		baseFunc ??= x => true;

		if (baseFunc.CanReduce)
		{
			baseFunc.Reduce();
		}

		return baseFunc;
	}

	#endregion

	#region Categories

	/// <summary>
	/// Returns all categories from the cache - does not populate parent categories.
	/// </summary>
	/// <returns></returns>
	public async Task<IReadOnlyList<UpCategory>> LoadCategoriesFromCacheAsync()
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			return await session.Query<UpCategory>()
				.OrderBy(x => x.Name)
				.ToListAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to retrieve categories: {exceptionMessage}", ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Saves the given categories to storage
	/// </summary>
	/// <param name="categories"></param>
	public async Task SaveCategoriesToCacheAsync(IEnumerable<UpCategory> categories)
	{
		try
		{
			_logger.LogDebug("Starting lightweight session");
			await using var session = _documentStore.LightweightSession();

			session.Store(categories);
			await session.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save categories: {exceptionMessage}", ex.Message);
			throw;
		}
	}

	#endregion
}