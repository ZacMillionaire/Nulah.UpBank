using Nulah.UpApi.Domain.Api;
using Nulah.UpApi.Domain.Api.Accounts;
using Nulah.UpApi.Domain.Api.Categories;
using Nulah.UpApi.Domain.Api.Transactions;

namespace Nulah.UpApi.Domain.Interfaces;

public interface IUpBankApi
{
	/// <summary>
	/// Returns all accounts for the currently authorised user.
	/// <para>
	/// Will return a failure if an access token has not been authorised.
	/// </para>
	/// </summary>
	/// <returns></returns>
	Task<ApiResponse<AccountsResponse>> GetAccounts(string? nextPage = null);

	Task<ApiResponse<AccountResponse>> GetAccount(string accountId);
	Task<ApiResponse<TransactionResponse>> GetTransactions(DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20, string? nextPage = null);

	/// <summary>
	/// Returns all categories from the Up API.
	/// <para>
	///	This is not currently a paginated API, but functionality exists if implementation changes in the future.
	/// </para>
	/// </summary>
	/// <param name="nextPage"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	Task<ApiResponse<CategoryResponse>> GetCategories(string? nextPage);

	Task<ApiResponse<TransactionResponse>> GetTransactionsByAccountId(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20);
	Task<ApiResponse<TransactionResponse>> GetNextTransactionPage(TransactionResponse transactionResponse);
}