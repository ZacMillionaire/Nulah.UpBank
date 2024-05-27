using System.Text.Json;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.ApiModels;
using Nulah.UpApi.Lib.ApiModels.Accounts;
using Nulah.UpApi.Lib.ApiModels.Categories;
using Nulah.UpApi.Lib.ApiModels.Transactions;

namespace Nulah.Up.IntegrationTests.Mocks;

public class TestUpBankApi : IUpBankApi
{
	private readonly JsonSerializerOptions _jsonSerialiserOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
	public Func<string?, Task<ApiResponse<AccountsResponse>>>? ReturnGetAccounts { get; set; }

	public async Task<T> LoadTestDataFromFile<T>(string fileLocation)
	{
		if (!File.Exists(fileLocation))
		{
			throw new Exception($"{fileLocation} not found.");
		}

		var deserializeAsync = await JsonSerializer.DeserializeAsync<T>(File.OpenRead(fileLocation), _jsonSerialiserOptions);
		return deserializeAsync;
	}

	public async Task<ApiResponse<AccountsResponse>> GetAccounts(string? nextPage = null)
	{
		if (ReturnGetAccounts == null)
		{
			throw new Exception($"{nameof(ReturnGetAccounts)} is not set");
		}

		return await ReturnGetAccounts(nextPage);
	}

	# region Not Implemented

	public async Task<ApiResponse<AccountResponse>> GetAccount(string accountId)
	{
		throw new NotImplementedException();
	}

	public async Task<ApiResponse<TransactionResponse>> GetTransactions(DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20, string? nextPage = null)
	{
		throw new NotImplementedException();
	}

	public async Task<ApiResponse<CategoryResponse>> GetCategories(string? nextPage)
	{
		throw new NotImplementedException();
	}

	public async Task<ApiResponse<TransactionResponse>> GetTransactionsByAccountId(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20)
	{
		throw new NotImplementedException();
	}

	public async Task<ApiResponse<TransactionResponse>> GetNextTransactionPage(TransactionResponse transactionResponse)
	{
		throw new NotImplementedException();
	}

	#endregion
}