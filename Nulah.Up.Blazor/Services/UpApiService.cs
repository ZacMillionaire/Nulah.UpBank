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

	public event EventHandler? AccountsUpdating;
	public event EventHandler<IReadOnlyList<UpAccount>>? AccountsUpdated;
	public event EventHandler? TransactionCacheStarted;
	public event EventHandler? TransactionCacheFinished;
	public event EventHandler<string>? TransactionCacheMessage;

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

	public async Task<IPagedList<UpTransaction>> GetTransactions(string? accountId = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20, int pageNumber = 1)
	{
		await using var session = _documentStore.LightweightSession();
		var existingAccounts = await LoadTransactionsFromCacheAsync(session, pageSize, pageNumber, BuildTransactionQuery(accountId, since, until));

		return existingAccounts;
	}

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
		var firstTransaction = session.Query<DateTime>("select MIN(d.data ->> 'CreatedAt')::timestamptz as data from public.mt_doc_uptransaction as d").FirstOrDefault();
		var latestTransaction = session.Query<DateTime>("select MAX(d.data ->> 'CreatedAt')::timestamptz as data from public.mt_doc_uptransaction as d").FirstOrDefault();

		return new TransactionCacheStats()
		{
			Count = transactionsCached,
			MostRecentTransactionDate = latestTransaction,
			FirstTransactionDate = firstTransaction
		};
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <param name="pageNumber">Minimum value is 1</param>
	/// <returns></returns>
	public async Task<IPagedList<UpTransaction>> CacheTransactions(string? accountId = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20)
	{
		try
		{
			TransactionCacheStarted?.Invoke(this, EventArgs.Empty);
			var sinceString = since != null ? since?.ToString("f") : "right now";
			var untilString = until != null ? until?.ToString("f") : "the very beginning";

			TransactionCacheMessage?.Invoke(this, $"Loading transactions since {sinceString} until {untilString} with page size of {pageSize}");
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
	private async Task<List<UpTransaction>> GetTransactionsFromApi(string? nextPage = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20)
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
						Category = x.Relationships.Category?.Data,
						CategoryParent = x.Relationships.ParentCategory?.Data,
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

	public async Task<IReadOnlyList<Transaction>> CacheTransactionsForAccount(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20)
	{
		// TODO: combine this into get transactions with a bool bypasscache
		throw new NotSupportedException();
		// TODO: this is mostly just testing code for now
		try
		{
			await using var session = _documentStore.LightweightSession();

			var transactionsForAccount = await _upBankApi.GetTransactionsByAccountId(accountId, since, until, pageSize);

			if (transactionsForAccount is { Success: true, Response: not null })
			{
				// retrieve any other pages of transactions
				await RetrieveNextPageOfTransactions(transactionsForAccount.Response, transactionsForAccount.Response.Data);

				await _documentStore.BulkInsertAsync(transactionsForAccount.Response.Data, BulkInsertMode.OverwriteExisting);
				await session.SaveChangesAsync();

				return transactionsForAccount.Response.Data;
			}

			return new List<Transaction>();
		}
		catch
		{
			throw;
		}
	}

	private async Task RetrieveNextPageOfTransactions(TransactionResponse transactionResponse, List<Transaction> transactions)
	{
		// TODO: reimplement this similar to retrieving accounts from the api
		throw new NotSupportedException();
		var nextTransactions = await _upBankApi.GetNextTransactionPage(transactionResponse);
		if (nextTransactions is { Success: true, Response: not null })
		{
			transactions.AddRange(nextTransactions.Response.Data);
			await RetrieveNextPageOfTransactions(nextTransactions.Response, transactions);
		}
	}

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

	private Task<IReadOnlyList<UpAccount>> LoadAccountsFromCacheAsync(IDocumentSession documentSession)
	{
		return documentSession.Query<UpAccount>()
			.OrderByDescending(x => x.AccountType)
			.ToListAsync();
	}

	private Task<IPagedList<UpTransaction>> LoadTransactionsFromCacheAsync(IDocumentSession documentSession, int pageSize = 20, int pageNumber = 1, Expression<Func<UpTransaction, bool>>? expression = null)
	{
		return documentSession.Query<UpTransaction>()
			.Where(expression ?? (x => true))
			.OrderByDescending(x => x.CreatedAt)
			.ToPagedListAsync(pageNumber, pageSize);
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
	/// Returns the combined expressions if <paramref name="expr1"/> is not null. Otherwise returns <paramref name="expr2"/>
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