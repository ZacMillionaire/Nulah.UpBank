using System.Linq.Expressions;
using Marten;
using Marten.Linq.LastModified;
using Marten.Pagination;
using Nulah.Up.Blazor.Models;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.Models.Accounts;
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
	/// <param name="accountId"></param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Defaults to 1, must be greater than 0.</param>
	/// <returns></returns>
	public async Task<IPagedList<UpTransaction>> GetTransactions(string? accountId = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = DefaultPageSize, int pageNumber = 1)
	{
		await using var session = _documentStore.LightweightSession();
		var existingAccounts = await LoadTransactionsFromCacheAsync(session, pageSize, pageNumber, BuildTransactionQuery(accountId, since, until));

		return existingAccounts;
	}

	/// <summary>
	/// Returns a stat object exposing various aspects of the transaction cache.
	/// </summary>
	/// <returns></returns>
	public async Task<TransactionCacheStats> GetTransactionCacheStats()
	{
		await using var session = _documentStore.LightweightSession();

		/*
		// Commented out as Min/Max currently cause deserialisation exceptions
		var statQueryBatch = session.CreateBatchQuery();
		var transactionsCached = statQueryBatch.Query<UpTransaction>().Count();
		var latestTransaction = statQueryBatch.Query<UpTransaction>().Max(x => x.CreatedAt);
		var firstTransaction = statQueryBatch.Query<UpTransaction>().Min(x => x.CreatedAt);
		await statQueryBatch.Execute();
		*/

		// shims to get a max result via Marten and sidestep any deserialisation exceptions.
		// Whenever I find a solution to those I'll be able to add these back into a query batch
		var transactionsCached = session.Query<UpTransaction>().Count();
		var firstTransaction = session.Query<DateTime>(
				"select MIN(d.data ->> 'CreatedAt')::timestamp as data from public.mt_doc_uptransaction as d"
			)
			.FirstOrDefault();
		var latestTransaction = session.Query<DateTime>(
				"select MAX(d.data ->> 'CreatedAt')::timestamp as data from public.mt_doc_uptransaction as d"
			)
			.FirstOrDefault();

		// If we have no transaction dates (due to nothing being cached), we'll need to return null, and we can't default the
		// above to null as marten cannot return null for scalar DateTime
		return new TransactionCacheStats()
		{
			Count = transactionsCached,
			MostRecentTransactionDate = latestTransaction == DateTime.MinValue 
				? null
				: latestTransaction,
			FirstTransactionDate = firstTransaction == DateTime.MinValue 
				? null
				: firstTransaction
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
			// load transactions from the api
			var transactions = await GetTransactionsFromApi(null, since, until, pageSize);
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
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <returns></returns>
	private async Task<List<UpTransaction>> GetTransactionsFromApi(string? nextPage = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = DefaultPageSize)
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
						Category = x.Relationships.Category?.Data == null
							? null
							: new UpCategory()
							{
								Id = x.Relationships.Category.Data.Id,
								Type = x.Relationships.Category.Data.Type
							},
						CategoryParent = x.Relationships.ParentCategory?.Data == null
							? null
							: new UpCategory()
							{
								Id = x.Relationships.ParentCategory.Data.Id,
								Type = x.Relationships.ParentCategory.Data.Type
							},
						Tags = x.Relationships.Tags?.Data ?? [],
						TransferAccountId = x.Relationships.TransferAccount?.Data?.Id
					})
			);

			if (!string.IsNullOrWhiteSpace(apiResponse.Response.Links.Next))
			{
				TransactionCacheMessage?.Invoke(this, "Loading next page of transactions...");
				// We still pass in the previous parameters if Up changes their API implementation
				transactions.AddRange(await GetTransactionsFromApi(apiResponse.Response.Links.Next, since, until, pageSize));
			}
		}

		return transactions;
	}

	/// <summary>
	/// Creates a predicate for linq to sql to filter transactions as appropriate.
	/// <para>
	/// Calling this method with no arguments results in a query that will return all results.
	/// </para>
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <returns></returns>
	private Expression<Func<UpTransaction, bool>> BuildTransactionQuery(string? accountId = null, DateTimeOffset? since = null, DateTimeOffset? until = null)
	{
		Expression<Func<UpTransaction, bool>>? baseFunc = null;

		if (!string.IsNullOrWhiteSpace(accountId))
		{
			baseFunc = baseFunc.And(x => x.AccountId == accountId);
		}

		if (since.HasValue)
		{
			baseFunc = baseFunc.And(x => since.Value.ToUniversalTime() <= x.CreatedAt);
		}

		if (until.HasValue)
		{
			baseFunc = baseFunc.And(x => x.CreatedAt <= until.Value.ToUniversalTime());
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

	public async Task<IReadOnlyList<UpCategory>> GetCategories(bool bypassCache = false)
	{
		var categories = await GetCategoriesInternal(bypassCache);

		// Populate categories with a parent - we can't do this from the database just yet.
		// Maybe in the future this may be possible but for now this is fine, and honestly we could probably
		// just do this before we cache records and combine this public with the internal below.
		foreach (var category in categories.Where(x => x.ParentCategoryId != null))
		{
			category.Parent = categories.FirstOrDefault(x => x.Id == category.ParentCategoryId);
		}

		return categories;
	}

	private async Task<IReadOnlyList<UpCategory>> GetCategoriesInternal(bool bypassCache = false)
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