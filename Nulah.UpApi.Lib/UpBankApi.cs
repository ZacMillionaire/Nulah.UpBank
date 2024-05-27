using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Nulah.UpApi.Lib.ApiModels.Accounts;
using Nulah.UpApi.Lib.ApiModels;
using Nulah.UpApi.Lib.ApiModels.Categories;
using Nulah.UpApi.Lib.ApiModels.Converters;
using Nulah.UpApi.Lib.ApiModels.Enums;
using Nulah.UpApi.Lib.ApiModels.Transactions;

namespace Nulah.UpApi.Lib;

public class UpBankApi : IUpBankApi
{
	private readonly UpConfiguration _configuration;
	private readonly HttpClient _httpClient;
	private readonly JsonSerializerOptions _jsonSerialiserOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

	/// <summary>
	/// Will be set to true on the first call that calls <see cref="IsAuthorised"/> and succeeds in a successful response
	/// from the Api.
	/// </summary>
	private bool _accessTokenIsValid;

	public UpBankApi(UpConfiguration configuration, HttpClient httpClient)
	{
		_httpClient = httpClient;
		_configuration = configuration;

		// TODO: eventually move these back to attributes on their respective locations - this is just a temp fix to enable Marten to deserialise
		_jsonSerialiserOptions.Converters.Add(new ResponseEnumConverter<AccountOwnershipType>());
		_jsonSerialiserOptions.Converters.Add(new ResponseEnumConverter<AccountType>());
		_jsonSerialiserOptions.Converters.Add(new ResponseEnumConverter<CardPurchaseMethodType>());
		_jsonSerialiserOptions.Converters.Add(new ResponseEnumConverter<TransactionStatus>());
		_jsonSerialiserOptions.Converters.Add(new HttpStatusCodeConverter());
	}

	/// <summary>
	/// Returns all accounts for the currently authorised user.
	/// <para>
	/// Will return a failure if an access token has not been authorised.
	/// </para>
	/// </summary>
	/// <returns></returns>
	public async Task<ApiResponse<AccountsResponse>> GetAccounts(string? nextPage = null)
	{
		// TODO: see comments in this method to potentially change this call to a throw on fail method instead of return bool
		if (!await IsAuthorised())
		{
			throw new Exception("Api token is invalid");
		}

		var accounts = await HttpGet<AccountsResponse>(string.IsNullOrWhiteSpace(nextPage)
			? $"{_configuration.ApiBaseAddress}/accounts"
			: nextPage);

		return accounts;
	}

	public async Task<ApiResponse<AccountResponse>> GetAccount(string accountId)
	{
		// TODO: see comments in this method to potentially change this call to a throw on fail method instead of return bool
		if (!await IsAuthorised())
		{
			throw new Exception("Api token is invalid");
		}

		if (string.IsNullOrWhiteSpace(accountId))
		{
			return new ApiResponse<AccountResponse>();
		}

		var accountById = await HttpGet<AccountResponse>($"{_configuration.ApiBaseAddress}/accounts/{accountId}");

		return accountById;
	}

	public async Task<ApiResponse<TransactionResponse>> GetTransactions(DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20, string? nextPage = null)
	{
		// TODO: see comments in this method to potentially change this call to a throw on fail method instead of return bool
		// TODO: add override for account id - only thing that should change is the uri has the account api added into it, all other functionality should be identical
		if (!await IsAuthorised())
		{
			throw new Exception("Api token is invalid");
		}

		var queryDict = new Dictionary<string, object>();

		// Only populate the query dict if we have nothing for next page, otherwise all query parameters will be contained
		// within the supplied nextpage url
		if (string.IsNullOrWhiteSpace(nextPage))
		{
			queryDict.Add("page[size]", pageSize);

			if (since != null)
			{
				queryDict.Add("filter[since]", since);
			}

			if (until != null)
			{
				queryDict.Add("filter[until]", until);
			}
			// TODO: add remaining filters for category and tag
		}

		var accounts = await HttpGet<TransactionResponse>(string.IsNullOrWhiteSpace(nextPage)
			? $"{_configuration.ApiBaseAddress}/transactions"
			: nextPage, queryDict);

		return accounts;
	}

	/// <summary>
	/// Returns all categories from the Up API.
	/// <para>
	///	This is not currently a paginated API, but functionality exists if implementation changes in the future.
	/// </para>
	/// </summary>
	/// <param name="nextPage"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public async Task<ApiResponse<CategoryResponse>> GetCategories(string? nextPage)
	{
		if (!await IsAuthorised())
		{
			throw new Exception("Api token is invalid");
		}

		var categories = await HttpGet<CategoryResponse>(string.IsNullOrWhiteSpace(nextPage)
			? $"{_configuration.ApiBaseAddress}/categories"
			: nextPage);

		return categories;
	}

	public async Task<ApiResponse<TransactionResponse>> GetTransactionsByAccountId(string accountId, DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20)
	{
		// TODO: see comments in this method to potentially change this call to a throw on fail method instead of return bool
		if (!await IsAuthorised())
		{
			throw new Exception("Api token is invalid");
		}

		var queryDict = new Dictionary<string, object>()
		{
			{ "page[size]", pageSize }
		};

		if (since != null)
		{
			queryDict.Add("filter[since]", since);
		}

		if (until != null)
		{
			queryDict.Add("filter[until]", until);
		}

		var accountTransactions = await HttpGet<TransactionResponse>($"{_configuration.ApiBaseAddress}/accounts/{accountId}/transactions", queryDict);
		return accountTransactions;
	}

	// TODO: make the TransactionResponse generic so this method can be generic for accounts
	public async Task<ApiResponse<TransactionResponse>> GetNextTransactionPage(TransactionResponse transactionResponse)
	{
		if (transactionResponse.Links.Next != null)
		{
			var accountTransactions = await HttpGet<TransactionResponse>(transactionResponse.Links.Next);
			return accountTransactions;
		}

		return await Task.FromResult<ApiResponse<TransactionResponse>>(new ApiResponse<TransactionResponse>());
	}

	/// <summary>
	/// Validates the previously set access token, and will not re-validate if the token never changes.
	/// Will return instantly after the first successful validation.
	/// </summary>
	/// <returns></returns>
	private async Task<bool> IsAuthorised()
	{
		// Don't revalidate a previously validated access token
		if (_accessTokenIsValid)
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(_configuration.AccessToken))
		{
			throw new Exception("Access token cannot be empty");
		}

		// Validate the token
		var accessTokenValid = await ValidateAccessToken();

		// TODO: add tests for what happens on a failed to auth request - might be better change throw on failure instead of returning bool

		// store the result for future calls
		_accessTokenIsValid = accessTokenValid.Success;

		return _accessTokenIsValid;
	}

	private async Task<ApiResponse<PingResponse>> ValidateAccessToken()
	{
		var internalResponseWrapper = await HttpGet<PingResponse>($"{_configuration.ApiBaseAddress}/util/ping");

		return internalResponseWrapper;
	}

	private async Task<ApiResponse<T>> HttpGet<T>(string uri, Dictionary<string, object>? queryParams = null) where T : class
	{
		try
		{
			uri = AppendQueryParamsToUri(uri, queryParams);

			var request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri(uri)
			};

			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.AccessToken);

			var response = await _httpClient.SendAsync(request);
			var responseBody = await response.Content.ReadAsStreamAsync();
			//var a = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var deserialisedResponse = JsonSerializer.Deserialize<T>(responseBody, _jsonSerialiserOptions);

				return ApiResponse.FromSuccess(deserialisedResponse);
			}
			else
			{
				var deserialisedResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, _jsonSerialiserOptions);

				return ApiResponse.FromError<T>(deserialisedResponse);
			}
		}
		catch
		{
			throw;
		}
	}

	private string AppendQueryParamsToUri(string uri, Dictionary<string, object>? queryParams)
	{
		if (queryParams == null || queryParams.Count == 0)
		{
			return uri;
		}

		var queryStringBuilder = new StringBuilder();
		foreach (var queryParam in queryParams)
		{
			if (queryParam.Value is DateTimeOffset dateTimeOffsetValue)
			{
				// We encode _only_ the + in the date time string to ensure it plays nicely with what the Up Api expects...
				// wonder if they have an opening for an API dev
				queryStringBuilder.Append($"{queryParam.Key}={dateTimeOffsetValue.ToString("yyyy-MM-ddTHH:mm:sszzz").Replace("+", "%2B")}");
			}
			else
			{
				queryStringBuilder.Append($"{queryParam.Key}={queryParam.Value}");
			}

			queryStringBuilder.Append('&');
		}

		queryStringBuilder.Remove(queryStringBuilder.Length - 1, 1);
		uri = $"{uri}?{queryStringBuilder}";

		return uri;
	}
}

public class UpConfiguration
{
	public string? AccessToken { get; set; }
	public string ApiBaseAddress { get; set; } = "https://api.up.com.au/api/v1";
}