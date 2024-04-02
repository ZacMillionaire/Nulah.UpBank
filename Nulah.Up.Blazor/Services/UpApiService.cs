using System.Linq.Expressions;
using Marten;
using Marten.Linq.LastModified;
using Marten.Pagination;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.Models.Accounts;
using Nulah.UpApi.Lib.Models.Transactions;

namespace Nulah.Up.Blazor.Services;

public class UpApiService
{
	private readonly UpBankApi _upBankApi;
	private readonly IDocumentStore _documentStore;

	public event EventHandler? AccountsUpdating;
	public event EventHandler<IReadOnlyList<Account>>? AccountsUpdated;

	public UpApiService(UpBankApi upBankApi, IDocumentStore documentStore)
	{
		_upBankApi = upBankApi;
		_documentStore = documentStore;
	}

	/// <summary>
	/// The first ever call to this method will cache accounts to the underlying database
	/// </summary>
	/// <returns></returns>
	public async Task<IReadOnlyList<Account>> GetAccounts()
	{
		try
		{
			AccountsUpdating?.Invoke(this, EventArgs.Empty);
			await using var session = _documentStore.LightweightSession();
			var existingAccounts = await LoadAccountsFromCacheAsync(session);

			if (existingAccounts.Count != 0)
			{
				AccountsUpdated?.Invoke(this, existingAccounts);
				return existingAccounts;
			}

			var accounts = await _upBankApi.GetAccounts();

			if (accounts is { Success: true, Response: not null })
			{
				await _documentStore.BulkInsertAsync(accounts.Response.Data, BulkInsertMode.OverwriteExisting);
				await session.SaveChangesAsync();

				AccountsUpdated?.Invoke(this, accounts.Response.Data);
				return accounts.Response.Data;
			}

			AccountsUpdated?.Invoke(this, new List<Account>());
			return new List<Account>();
		}
		catch
		{
			throw;
		}
	}

	public async Task<Account> GetAccount(string accountId)
	{
		try
		{
			await using var session = _documentStore.LightweightSession();
			var existingAccount = await session.Query<Account>().FirstOrDefaultAsync(x =>
				x.Id == accountId
				// only return an account if it's "fresh" which is a modified date less than a day old (currently)
				// TODO: configure this
				&& x.ModifiedBefore(DateTime.UtcNow.AddDays(-1))
			);

			if (existingAccount != null)
			{
				return existingAccount;
			}

			var accounts = await _upBankApi.GetAccount(accountId);

			if (accounts is { Success: true, Response: not null })
			{
				session.Insert(accounts.Response.Data);
				await session.SaveChangesAsync();

				return accounts.Response.Data;
			}

			return new Account();
		}
		catch
		{
			throw;
		}
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
	public async Task<IPagedList<Transaction>> GetTransactionsForAccountFromCache(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20, int pageNumber = 1)
	{
		try
		{
			await using var session = _documentStore.LightweightSession();
			var existingAccounts = await LoadTransactionsFromCacheAsync(session, pageSize, pageNumber, BuildTransactionQuery(accountId, since, until));

			return existingAccounts;
		}
		catch
		{
			throw;
		}
	}

	public async Task<IReadOnlyList<Transaction>> CacheTransactionsForAccount(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20)
	{
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
		var nextTransactions = await _upBankApi.GetNextTransactionPage(transactionResponse);
		if (nextTransactions is { Success: true, Response: not null })
		{
			transactions.AddRange(nextTransactions.Response.Data);
			await RetrieveNextPageOfTransactions(nextTransactions.Response, transactions);
		}
	}

	private Expression<Func<Transaction, bool>> BuildTransactionQuery(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null)
	{
		Expression<Func<Transaction, bool>>? baseFunc = null;

		if (!string.IsNullOrWhiteSpace(accountId))
		{
			baseFunc = baseFunc.And(x => x.Relationships.Account.Data.Id == accountId);
		}

		if (since.HasValue)
		{
			baseFunc = baseFunc.And(x => since.Value.ToUniversalTime() <= x.Attributes.CreatedAt);
		}

		if (until.HasValue)
		{
			baseFunc = baseFunc.And(x => x.Attributes.CreatedAt <= until.Value.ToUniversalTime());
		}

		// Return an "empty" expression if we have a criteria object, but no criteria to act on
		baseFunc ??= x => true;

		if (baseFunc.CanReduce)
		{
			baseFunc.Reduce();
		}

		return baseFunc;
	}

	private Task<IReadOnlyList<Account>> LoadAccountsFromCacheAsync(IDocumentSession documentSession)
	{
		return documentSession.Query<Account>().ToListAsync();
	}

	private Task<IPagedList<Transaction>> LoadTransactionsFromCacheAsync(IDocumentSession documentSession, int pageSize = 20, int pageNumber = 1, Expression<Func<Transaction, bool>>? expression = null)
	{
		return documentSession.Query<Transaction>()
			.Where(expression ?? (x => true))
			.OrderByDescending(x => x.Attributes.CreatedAt)
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