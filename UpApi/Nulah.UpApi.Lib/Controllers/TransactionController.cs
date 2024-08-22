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
	private readonly IUpStorage _upStorage;
	private readonly CategoryController _categoryController;
	private readonly ILogger<TransactionController> _logger;


	public Action<TransactionController, EventArgs>? TransactionCacheStarted;
	public Action<TransactionController, EventArgs>? TransactionCacheFinished;
	public Action<TransactionController, string>? TransactionCacheMessage;

	public TransactionController(IUpBankApi upBankApi,
		IUpStorage upStorage,
		CategoryController categoryController,
		ILogger<TransactionController> logger
	)
	{
		_upBankApi = upBankApi;
		_upStorage = upStorage;
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
		var existingAccounts = await _upStorage.LoadTransactionsFromCacheAsync(pageSize, pageNumber, BuildTransactionQuery(criteria));

		// TODO: create a wrapper for IPagedList that we control to reduce the dependency on MartenDB while still
		// having a valuable pagination
		return existingAccounts as IPagedList<UpTransaction>;
	}

	/// <summary>
	/// Returns a stat object exposing various aspects of the transaction cache.
	/// </summary>
	/// <returns></returns>
	public async Task<TransactionCacheStats> GetTransactionCacheStats()
	{
		return await _upStorage.GetTransactionStats();
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
	/// Caching transactions by <paramref name="accountId"/> is not currently implemented and will be implemented in future
	/// </para>
	/// </summary>
	/// <param name="accountId">Currently unimplemented</param>
	/// <param name="since"></param>
	/// <param name="until"></param>
	/// <param name="pageSize"></param>
	/// <returns></returns>
	public async Task<IEnumerable<UpTransaction>?> CacheTransactions(string? accountId = null, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = DefaultPageSize)
	{
		// TODO: implement saving transactions by accountId
		// TODO: remember how I was going to do that 
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

			await _upStorage.SaveTransactionsToCacheAsync(transactions);

			TransactionCacheMessage?.Invoke(this, $"Cache complete! Cached {transactions.Count} transactions");

			var firstPageOfTransactions = await _upStorage.LoadTransactionsFromCacheAsync(pageSize);

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

	/// <summary>
	/// Returns the logical type of a transaction from its <see cref="TransactionAttributes.Description"/>, which is currently the name of the merchant.
	///
	/// The type returned is inferred from various keywords and may not be correct if a merchant name happens to overlap.
	/// </summary>
	/// <param name="description"></param>
	/// <returns></returns>
	private TransactionType CategoriseTransactionTypeFromDescription(string description)
	{
		// TODO: this can maybe be moved to the constructor for an UpTransaction or as the get
		// for the category enum
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
}