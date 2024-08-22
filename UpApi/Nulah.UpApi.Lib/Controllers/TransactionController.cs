using System.Linq.Expressions;
using Marten;
using Marten.Pagination;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Api.Transactions;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Domain.Models.Transactions;
using Nulah.UpApi.Domain.Models.Transactions.Criteria;

namespace Nulah.UpApi.Lib.Controllers;

public class TransactionController
{
	private const int DefaultPageSize = 25;
	private readonly IUpBankApi _upBankApi;
	private readonly IDocumentStore _documentStore;
	private readonly CategoryController _categoryController;
	private readonly ILogger<TransactionController> _logger;


	public Action<TransactionController, EventArgs>? TransactionCacheStarted;
	public Action<TransactionController, EventArgs>? TransactionCacheFinished;
	public Action<TransactionController, string>? TransactionCacheMessage;

	public TransactionController(IUpBankApi upBankApi,
		IDocumentStore documentStore,
		CategoryController categoryController,
		ILogger<TransactionController> logger
	)
	{
		_upBankApi = upBankApi;
		_documentStore = documentStore;
		_categoryController = categoryController;
		_logger = logger;
	}

	/// <summary>
	/// Returns all transactions based on given parameters.
	/// </summary>
	/// <param name="criteria"></param>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1, must be greater than 0.</param>
	/// <returns></returns>
	public async Task<IPagedList<UpTransaction>> GetTransactions(TransactionQueryCriteria? criteria,
		int pageSize = DefaultPageSize,
		int pageNumber = 1)
	{
		await using var session = _documentStore.LightweightSession();
		// TODO: investigate moving to EF for storage of transaction information so we can reduce zero sum transaction pairs
		// eg: a cover will generate a pair that can be matched by CreatedBy that count as no difference and can potentially be excluded.
		// For now a criteria will be used to simply exclude uncategorisable transactions, as a future API update (if one ever happens...),
		// could expose where a transaction was covered and these uncategorisable transactions may not come through, or may have additional
		// metadata that can allow me to better display them.
		var existingAccounts = await LoadTransactionsFromCacheAsync(session, pageSize, pageNumber, BuildTransactionQuery(criteria));

		return existingAccounts;
	}

	/// <summary>
	/// Returns a stat object exposing various aspects of the transaction cache.
	/// </summary>
	/// <returns></returns>
	public async Task<TransactionCacheStats> GetTransactionCacheStats()
	{
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
	/// Returns a string representing a user friendly description of what transactions are being collected.
	/// </summary>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <returns></returns>
	private string GetSinceUntilString(DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = DefaultPageSize)
	{
		// TODO: this feels gross but I was getting sick of trying to mentally translate the temporal when developing
		if (since == null && until == null)
		{
			return $"Loading <strong>all transactions</strong> available until the first transaction ever made across all accounts with page size of {pageSize}";
		}

		if (since == null && until != null)
		{
			return $"Loading all transactions from before <strong>{since:dddd, dd MMMM, yyyy}</strong> until the first transaction ever made across all accounts with page size of {pageSize}";
		}

		if (since != null && until == null)
		{
			return $"Loading all transactions from <strong>{since:dddd, dd MMMM, yyyy}</strong> until midnight, <strong>{DateTime.Now:dddd, dd MMMM, yyyy}</strong> with page size of {pageSize}";
		}

		return $"Loading all transactions from <strong>{since:dddd, dd MMMM, yyyy}</strong> until <strong>{until:dddd, dd MMMM, yyyy}</strong> with page size of {pageSize}";
	}

	/// <summary>
	/// Caches all transactions given appropriate parameters.
	/// <para>
	/// Caching transactions by <paramref name="accountId"/> is not currently implemented
	/// </para>
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Minimum value is 1</param>
	/// <returns></returns>
	public async Task<IPagedList<UpTransaction>> CacheTransactions(string? accountId = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = DefaultPageSize)
	{
		try
		{
			TransactionCacheStarted?.Invoke(this, EventArgs.Empty);

			TransactionCacheMessage?.Invoke(this, GetSinceUntilString(since, until, pageSize));

			// Get categories for populating transaction objects.
			// If we had a relational database we wouldn't need to do this, however at present we're using Marten for
			// everything so we build the document ahead of time.
			// TODO: In the future I might look at separating these concerns and move to EF first with a proper schema, and then using Marten as a cache layer.
			TransactionCacheMessage?.Invoke(this, "Retrieving categories");
			var categories = await _categoryController.GetCategories();
			var categoryLookup = categories.ToDictionary(x => x.Id, x => x);

			// load transactions from the api
			var transactions = await GetTransactionsFromApi(null, categoryLookup, since, until, pageSize);
			TransactionCacheMessage?.Invoke(this, "All transactions loaded.");

			TransactionCacheMessage?.Invoke(this, $"Caching loaded transactions...");

			await using var session = _documentStore.LightweightSession();
			session.Store((IEnumerable<UpTransaction>)transactions);
			await session.SaveChangesAsync();

			TransactionCacheMessage?.Invoke(this, $"Cache complete! Cached {transactions.Count} transactions");

			var firstPageOfTransactions = await LoadTransactionsFromCacheAsync(session, pageSize);

			return firstPageOfTransactions;
		}
		catch
		{
			throw;
		}
		finally
		{
			TransactionCacheFinished?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Retrieves transactions from the API with the given filters.
	/// <para>
	/// If <paramref name="nextPage"/> is not null, all other parameters will be ignored.
	/// </para>
	/// </summary>
	/// <param name="nextPage"></param>
	/// <param name="categoryLookup"></param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <returns></returns>
	private async Task<List<UpTransaction>> GetTransactionsFromApi(string? nextPage = null,
		Dictionary<string, UpCategory>? categoryLookup = null,
		DateTimeOffset? since = null,
		DateTimeOffset? until = null,
		int pageSize = DefaultPageSize)
	{
		var transactions = new List<UpTransaction>();
		var apiResponse = await _upBankApi.GetTransactions(since, until, pageSize, nextPage);

		if (apiResponse is { Success: true, Response: not null })
		{
			TransactionCacheMessage?.Invoke(this, $"Loaded {apiResponse.Response.Data.Count} transactions from the api");
			transactions.AddRange(
				apiResponse.Response.Data
					.Select(x => new UpTransaction()
					{
						Id = x.Id,
						CreatedAt = x.Attributes.CreatedAt,
						Amount = x.Attributes.Amount,
						Cashback = x.Attributes.Cashback,
						Description = x.Attributes.Description,
						Message = x.Attributes.Message,
						RawText = x.Attributes.RawText,
						Status = x.Attributes.Status,
						AccountId = x.Relationships.Account?.Data?.Id,
						ForeignAmount = x.Attributes.ForeignAmount,
						HoldInfo = x.Attributes.HoldInfo,
						IsCategorizable = x.Attributes.IsCategorizable,
						RoundUp = x.Attributes.RoundUp,
						SettledAt = x.Attributes.SettledAt,
						CardPurchaseMethod = x.Attributes.CardPurchaseMethod,
						// This feels weird here, but this is the first point we can update these without re-enumerating
						// the list. Plus it's not really a big issue performance-wise (yet).
						Category = _categoryController.LookupCategory(x.Relationships.Category?.Data, categoryLookup),
						CategoryParent = _categoryController.LookupCategory(x.Relationships.ParentCategory?.Data, categoryLookup),
						Tags = x.Relationships.Tags?.Data ?? [],
						TransferAccountId = x.Relationships.TransferAccount?.Data?.Id,
						InferredType = CategoriseTransactionTypeFromDescription(x.Attributes.Description)
					})
			);

			if (!string.IsNullOrWhiteSpace(apiResponse.Response.Links.Next))
			{
				TransactionCacheMessage?.Invoke(this, "Loading next page of transactions...");
				// We still pass in the previous parameters if Up changes their API implementation
				transactions.AddRange(await GetTransactionsFromApi(apiResponse.Response.Links.Next, since: since, until: until, pageSize: pageSize));
			}
		}

		return transactions;
	}

	/// <summary>
	/// Creates a predicate for linq to sql to filter transactions as appropriate.
	/// <para>
	/// Calling this method with no criteria results in a query that will return all results, excluding transactions that cannot be categorised.
	/// </para>
	/// </summary>
	/// <param name="transactionQueryCriteria"></param>
	/// <returns></returns>
	private Expression<Func<UpTransaction, bool>> BuildTransactionQuery(TransactionQueryCriteria? transactionQueryCriteria)
	{
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

	/// <summary>
	/// Returns the logical type of a transaction from its <see cref="TransactionAttributes.Description"/>, which is currently the name of the merchant.
	///
	/// The type returned is inferred from various keywords and may not be correct if a merchant name happens to overlap.
	/// </summary>
	/// <param name="description"></param>
	/// <returns></returns>
	private TransactionType CategoriseTransactionTypeFromDescription(string description)
	{
		if (description.StartsWith("Cover to") || description.StartsWith("Cover from"))
		{
			return TransactionType.Cover;
		}

		if (description.StartsWith("Forward to") || description.StartsWith("Forward from"))
		{
			return TransactionType.Forward;
		}

		if (description.StartsWith("Transfer to") || description.StartsWith("Transfer from"))
		{
			return TransactionType.Transfer;
		}

		if (description.StartsWith("Interest"))
		{
			return TransactionType.Interest;
		}

		if (description.StartsWith("Bonus Payment"))
		{
			return TransactionType.Bonus;
		}

		return TransactionType.Transaction;
	}

	/// <summary>
	/// Returns all transactions from the cache by given 
	/// </summary>
	/// <param name="documentSession"></param>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1. Must be greater than 0.</param>
	/// <param name="queryExpression">
	/// Either generated from <see cref="BuildTransactionQuery"/> or passed in raw. Defaults to ((UpTransaction)x => true)
	/// if null.
	/// </param>
	/// <returns></returns>
	private Task<IPagedList<UpTransaction>> LoadTransactionsFromCacheAsync(IDocumentSession documentSession,
		int pageSize = DefaultPageSize,
		int pageNumber = 1,
		Expression<Func<UpTransaction, bool>>? queryExpression = null)
	{
		return documentSession.Query<UpTransaction>()
			.Where(queryExpression ?? (x => true))
			.OrderByDescending(x => x.CreatedAt)
			.ToPagedListAsync(pageNumber, pageSize);
	}
}