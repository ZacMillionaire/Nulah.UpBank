using System.Linq.Expressions;
using Marten;
using Marten.Linq.LastModified;
using Marten.Pagination;
using Nulah.Up.Blazor.Models;
using Nulah.Up.Blazor.Models.Criteria;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.Models.Categories;
using Nulah.UpApi.Lib.Models.Transactions;

namespace Nulah.Up.Blazor.Services;

public class UpApiService
{
	private readonly UpBankApi _upBankApi;
	private readonly IDocumentStore _documentStore;
	private const int DefaultPageSize = 25;

	public event EventHandler? AccountsUpdating;
	public event EventHandler<IReadOnlyList<UpAccount>>? AccountsUpdated;
	public event EventHandler? TransactionCacheStarted;
	public event EventHandler? TransactionCacheFinished;
	public event EventHandler<string>? TransactionCacheMessage;
	public event EventHandler? CategoriesUpdating;
	public event EventHandler<IReadOnlyList<UpCategory>>? CategoriesUpdated;

	public UpApiService(UpBankApi upBankApi, IDocumentStore documentStore)
	{
		_upBankApi = upBankApi;
		_documentStore = documentStore;
	}

	#region Accounts

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
		AccountsUpdating?.Invoke(this, EventArgs.Empty);

		await using var session = _documentStore.LightweightSession();

		if (!bypassCache)
		{
			var existingAccounts = await LoadAccountsFromCacheAsync(session);

			// We duplicate code slightly here to retrieve accounts from the Api if _no_ accounts
			// exist in the database.
			// This means that if the user has no accounts ever (unlikely as an Up customer should always have a spending account)
			// this method will potentially hit the Api every single call.
			// I don't really care about that though, if the user has no accounts then the Up Api will be doing no
			// work to return nothing so ¯\_(ツ)_/¯
			if (existingAccounts.Count != 0)
			{
				AccountsUpdated?.Invoke(this, existingAccounts);
				return existingAccounts;
			}
		}

		var accounts = await GetAccountsFromApi();

		session.Store((IEnumerable<UpAccount>)accounts);
		await session.SaveChangesAsync();

		AccountsUpdated?.Invoke(this, accounts);
		return accounts;
	}

	private async Task<List<UpAccount>> GetAccountsFromApi(string? nextPage = null)
	{
		var accounts = new List<UpAccount>();
		var apiResponse = await _upBankApi.GetAccounts(nextPage);

		if (apiResponse is { Success: true, Response: not null })
		{
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
				await GetAccountsFromApi(apiResponse.Response.Links.Next);
			}
		}

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

	#endregion

	#region Transactions

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
			var categories = await GetCategories();
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
						Category = LookupCategory(x.Relationships.Category?.Data, categoryLookup),
						CategoryParent = LookupCategory(x.Relationships.ParentCategory?.Data, categoryLookup),
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

	#endregion

	#region Categories

	public async Task<IReadOnlyList<UpCategory>> GetCategories(bool bypassCache = false)
	{
		CategoriesUpdating?.Invoke(this, EventArgs.Empty);

		await using var session = _documentStore.LightweightSession();

		if (!bypassCache)
		{
			var existingCategories = await LoadCategoriesFromCacheAsync(session);

			// We duplicate code slightly here to retrieve categories from the Api if _no_ categories
			// exist in the database.
			if (existingCategories.Count != 0)
			{
				CategoriesUpdated?.Invoke(this, existingCategories);
				return existingCategories;
			}
		}

		var categories = await GetCategoriesFromApi();

		foreach (var category in categories.Where(x => x.ParentCategoryId != null))
		{
			category.Parent = categories.FirstOrDefault(x => x.Id == category.ParentCategoryId);
		}

		session.Store((IEnumerable<UpCategory>)categories);
		await session.SaveChangesAsync();

		CategoriesUpdated?.Invoke(this, categories);
		return categories;
	}


	private async Task<List<UpCategory>> GetCategoriesFromApi(string? nextPage = null)
	{
		var categories = new List<UpCategory>();
		var apiResponse = await _upBankApi.GetCategories(nextPage);

		if (apiResponse is { Success: true, Response: not null })
		{
			categories.AddRange(apiResponse.Response.Data
				.Select(x => new UpCategory()
				{
					Id = x.Id,
					Name = x.Attributes.Name,
					Type = x.Type,
					ParentCategoryId = x.Relationships?.parent.data?.id
				}));
		}

		return categories;
	}


	/// <summary>
	/// <para>
	/// Returns a category for transactions when caching. If <paramref name="rawCategory"/> is null, null is returned.
	/// </para>
	/// <para>
	///	If <paramref name="categoryLookup"/> is null or contains no elements, or the id from <paramref name="rawCategory"/> cannot be found
	/// as a key value, a category is returned with the id and type from <paramref name="rawCategory"/>.
	/// </para>
	/// <para>
	///	Otherwise the matched category by id is returned from <paramref name="categoryLookup"/>
	/// </para>
	/// </summary>
	/// <param name="rawCategory"></param>
	/// <param name="categoryLookup"></param>
	/// <returns></returns>
	private UpCategory? LookupCategory(Category? rawCategory = null, Dictionary<string, UpCategory>? categoryLookup = null)
	{
		// If we have no category return no category
		if (rawCategory == null)
		{
			return null;
		}

		// If we have no lookup dictionary, guess a category from the raw category given. We'll be missing some information
		// but that's still adequate
		if (categoryLookup == null || categoryLookup.Count == 0)
		{
			return new UpCategory()
			{
				Id = rawCategory.Id,
				Type = rawCategory.Type
			};
		}

		// Try find the category by id and return the populated and previously cached (hopefully) category with full details
		if (categoryLookup.TryGetValue(rawCategory.Id, out var categoryMatch))
		{
			return categoryMatch;
		}

		// otherwise our fallback is to create a category from the raw category given which contains the Id that we'll be indexing
		// and querying off anyway. Name is a nice to have essentially
		return new UpCategory()
		{
			Id = rawCategory.Id,
			Type = rawCategory.Type
		};
	}

	#endregion

	/// <summary>
	/// Returns all accounts from the cache.
	/// </summary>
	/// <param name="documentSession"></param>
	/// <returns></returns>
	private Task<IReadOnlyList<UpAccount>> LoadAccountsFromCacheAsync(IDocumentSession documentSession)
	{
		return documentSession.Query<UpAccount>()
			.OrderByDescending(x => x.AccountType)
			.ToListAsync();
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

	/// <summary>
	/// Returns all categories from the cache - does not populate parent categories.
	/// </summary>
	/// <param name="documentSession"></param>
	/// <returns></returns>
	private Task<IReadOnlyList<UpCategory>> LoadCategoriesFromCacheAsync(IDocumentSession documentSession)
	{
		return documentSession.Query<UpCategory>()
			.OrderBy(x => x.Name)
			.ToListAsync();
	}
}

// whatever I copy pasted from another project of mine because it's insane to make a single nuget package just for my own stuff and push that on to others (just yet)
/// <summary>
/// Helper methods for combining linq queries for EF conversions at a trivial level. Honestly I just wanted something lazy for my lazy specification system lol
/// <para>
/// Not guaranteed to return expressions that can be converted into database queries as it only works at an <see cref="Expression"/> level
/// </para>
/// </summary>
internal static class PredicateBuilder
{
	// from https://stackoverflow.com/questions/22569043/merge-two-linq-expressions/22569086#22569086
	internal static Expression<Func<T, bool>> True<T>()
	{
		return f => true;
	}

	internal static Expression<Func<T, bool>> False<T>()
	{
		return f => false;
	}

	/// <summary>
	/// Returns the combined expressions as a logical OrElse if <paramref name="expr1"/> is not null. Otherwise returns <paramref name="expr2"/>
	/// </summary>
	/// <param name="expr1"></param>
	/// <param name="expr2"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	internal static Expression<Func<T, bool>> Or<T>(
		this Expression<Func<T, bool>>? expr1,
		Expression<Func<T, bool>> expr2)
	{
		if (expr1 == null)
		{
			return expr2;
		}

		var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);
		return Expression.Lambda<Func<T, bool>>
			(Expression.OrElse(expr1.Body, secondBody), expr1.Parameters);
	}

	/// <summary>
	/// Returns the combined expressions as a logical AndAlso if <paramref name="expr1"/> is not null. Otherwise returns <paramref name="expr2"/>
	/// </summary>
	/// <param name="expr1"></param>
	/// <param name="expr2"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	internal static Expression<Func<T, bool>> And<T>(
		this Expression<Func<T, bool>>? expr1,
		Expression<Func<T, bool>> expr2)
	{
		if (expr1 == null)
		{
			return expr2;
		}

		var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);
		return Expression.Lambda<Func<T, bool>>
			(Expression.AndAlso(expr1.Body, secondBody), expr1.Parameters);
	}

	private static Expression Replace(this Expression expression,
		Expression searchEx, Expression replaceEx)
	{
		return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
	}

	private class ReplaceVisitor : ExpressionVisitor
	{
		private readonly Expression _from, _to;

		public ReplaceVisitor(Expression from, Expression to)
		{
			_from = from;
			_to = to;
		}

		public override Expression Visit(Expression? node)
		{
			// If node is null fall into whatever the base implementation is
			return node == _from || node == null
				? _to
				: base.Visit(node);
		}
	}
}