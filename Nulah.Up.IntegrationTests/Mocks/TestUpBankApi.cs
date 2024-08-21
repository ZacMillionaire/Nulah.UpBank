using System.Text.Json;
using Nulah.UpApi.Domain.Api;
using Nulah.UpApi.Domain.Api.Accounts;
using Nulah.UpApi.Domain.Api.Categories;
using Nulah.UpApi.Domain.Api.Transactions;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.ApiModels;
using Nulah.UpApi.Lib.ApiModels.Accounts;

namespace Nulah.Up.IntegrationTests.Mocks;

public class TestUpBankApi : IUpBankApi
{
	private readonly JsonSerializerOptions _jsonSerialiserOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
	public Func<string?, Task<ApiResponse<AccountsResponse>>>? ReturnGetAccounts { get; set; }
	public Func<DateTimeOffset?, DateTimeOffset?, int, string?, Task<ApiResponse<TransactionResponse>>>? ReturnGetTransactions { get; set; }

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
		if (ReturnGetTransactions == null)
		{
			throw new Exception($"{nameof(ReturnGetTransactions)} is not set");
		}

		return await ReturnGetTransactions(since, until, pageSize, nextPage);
	}

	public async Task<ApiResponse<CategoryResponse>> GetCategories(string? nextPage)
	{
		// pretend we're async lol
		await Task.Yield();
		// categories are hardcoded as these are set by Upbank (and are probably unlikely to change given their track record with updating this list)
		return ApiResponse.FromSuccess(JsonSerializer.Deserialize<CategoryResponse>(
			"""
			{"Data":[{"Type":"categories","Id":"games-and-software","Attributes":{"Name":"Apps, Games & Software"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"car-insurance-and-maintenance","Attributes":{"Name":"Car Insurance, Rego & Maintenance"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"family","Attributes":{"Name":"Children & Family"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"good-life","Attributes":{"Name":"Good Life"},"Relationships":{"parent":{"data":null},"children":{"data":[{"type":"categories","id":"games-and-software"},{"type":"categories","id":"booze"},{"type":"categories","id":"events-and-gigs"},{"type":"categories","id":"hobbies"},{"type":"categories","id":"holidays-and-travel"},{"type":"categories","id":"lottery-and-gambling"},{"type":"categories","id":"pubs-and-bars"},{"type":"categories","id":"restaurants-and-cafes"},{"type":"categories","id":"takeaway"},{"type":"categories","id":"tobacco-and-vaping"},{"type":"categories","id":"tv-and-music"},{"type":"categories","id":"adult"}]}}},{"Type":"categories","Id":"groceries","Attributes":{"Name":"Groceries"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"booze","Attributes":{"Name":"Booze"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"clothing-and-accessories","Attributes":{"Name":"Clothing & Accessories"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"cycling","Attributes":{"Name":"Cycling"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"homeware-and-appliances","Attributes":{"Name":"Homeware & Appliances"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"personal","Attributes":{"Name":"Personal"},"Relationships":{"parent":{"data":null},"children":{"data":[{"type":"categories","id":"family"},{"type":"categories","id":"clothing-and-accessories"},{"type":"categories","id":"education-and-student-loans"},{"type":"categories","id":"fitness-and-wellbeing"},{"type":"categories","id":"gifts-and-charity"},{"type":"categories","id":"hair-and-beauty"},{"type":"categories","id":"health-and-medical"},{"type":"categories","id":"investments"},{"type":"categories","id":"life-admin"},{"type":"categories","id":"mobile-phone"},{"type":"categories","id":"news-magazines-and-books"},{"type":"categories","id":"technology"}]}}},{"Type":"categories","Id":"education-and-student-loans","Attributes":{"Name":"Education & Student Loans"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"events-and-gigs","Attributes":{"Name":"Events & Gigs"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"fuel","Attributes":{"Name":"Fuel"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"home","Attributes":{"Name":"Home"},"Relationships":{"parent":{"data":null},"children":{"data":[{"type":"categories","id":"groceries"},{"type":"categories","id":"homeware-and-appliances"},{"type":"categories","id":"internet"},{"type":"categories","id":"home-maintenance-and-improvements"},{"type":"categories","id":"pets"},{"type":"categories","id":"home-insurance-and-rates"},{"type":"categories","id":"rent-and-mortgage"},{"type":"categories","id":"utilities"}]}}},{"Type":"categories","Id":"internet","Attributes":{"Name":"Internet"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"fitness-and-wellbeing","Attributes":{"Name":"Fitness & Wellbeing"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"hobbies","Attributes":{"Name":"Hobbies"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"home-maintenance-and-improvements","Attributes":{"Name":"Maintenance & Improvements"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"parking","Attributes":{"Name":"Parking"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"transport","Attributes":{"Name":"Transport"},"Relationships":{"parent":{"data":null},"children":{"data":[{"type":"categories","id":"car-insurance-and-maintenance"},{"type":"categories","id":"cycling"},{"type":"categories","id":"fuel"},{"type":"categories","id":"parking"},{"type":"categories","id":"public-transport"},{"type":"categories","id":"car-repayments"},{"type":"categories","id":"taxis-and-share-cars"},{"type":"categories","id":"toll-roads"}]}}},{"Type":"categories","Id":"gifts-and-charity","Attributes":{"Name":"Gifts & Charity"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"holidays-and-travel","Attributes":{"Name":"Holidays & Travel"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"pets","Attributes":{"Name":"Pets"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"public-transport","Attributes":{"Name":"Public Transport"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"hair-and-beauty","Attributes":{"Name":"Hair & Beauty"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"lottery-and-gambling","Attributes":{"Name":"Lottery & Gambling"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"home-insurance-and-rates","Attributes":{"Name":"Rates & Insurance"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"car-repayments","Attributes":{"Name":"Repayments"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"health-and-medical","Attributes":{"Name":"Health & Medical"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"pubs-and-bars","Attributes":{"Name":"Pubs & Bars"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"rent-and-mortgage","Attributes":{"Name":"Rent & Mortgage"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"taxis-and-share-cars","Attributes":{"Name":"Taxis & Share Cars"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"investments","Attributes":{"Name":"Investments"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"restaurants-and-cafes","Attributes":{"Name":"Restaurants & Cafes"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"toll-roads","Attributes":{"Name":"Tolls"},"Relationships":{"parent":{"data":{"type":"categories","id":"transport"}},"children":{"data":[]}}},{"Type":"categories","Id":"utilities","Attributes":{"Name":"Utilities"},"Relationships":{"parent":{"data":{"type":"categories","id":"home"}},"children":{"data":[]}}},{"Type":"categories","Id":"life-admin","Attributes":{"Name":"Life Admin"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"takeaway","Attributes":{"Name":"Takeaway"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"mobile-phone","Attributes":{"Name":"Mobile Phone"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"tobacco-and-vaping","Attributes":{"Name":"Tobacco & Vaping"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"news-magazines-and-books","Attributes":{"Name":"News, Magazines & Books"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}},{"Type":"categories","Id":"tv-and-music","Attributes":{"Name":"TV, Music & Streaming"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"adult","Attributes":{"Name":"Adult"},"Relationships":{"parent":{"data":{"type":"categories","id":"good-life"}},"children":{"data":[]}}},{"Type":"categories","Id":"technology","Attributes":{"Name":"Technology"},"Relationships":{"parent":{"data":{"type":"categories","id":"personal"}},"children":{"data":[]}}}]}
			"""
			));
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