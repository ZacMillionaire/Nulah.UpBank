using System.Linq.Expressions;
using Marten;
using Marten.Linq.LastModified;
using Marten.Pagination;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Domain.Models.Transactions;

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

			_logger.LogInformation("Retrieving transactions");
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

	#endregion
}