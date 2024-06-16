using System.Text.Encodings.Web;
using System.Text.Json;
using Nulah.Up.IntegrationTests.Helpers;
using Nulah.Up.IntegrationTests.Mocks;
using Nulah.UpApi.Lib.ApiModels;
using Nulah.UpApi.Lib.ApiModels.Accounts;
using Nulah.UpApi.Lib.ApiModels.Shared;
using Nulah.UpApi.Lib.ApiModels.Transactions;
using Nulah.UpApi.Lib.Models;
using Xunit.Abstractions;

namespace Nulah.Up.IntegrationTests.RuleSystemTests;

[Collection("integration")]
public class GetTransactionsByDate : TestBase<RuleSystemFixture>
{
	public GetTransactionsByDate(RuleSystemFixture testFixture, ITestOutputHelper? output)
		: base(testFixture, output)
	{
		_upBankApi.ReturnGetTransactions = GetTransactions;
		var controller = TestFixture.CreateController(_upBankApi);
		var transactions = controller.CacheTransactions().Result;
	}

	private readonly TestUpBankApi _upBankApi = new();

	[Fact]
	public async void DateRange()
	{
		// var search = TestFixture.CreateController(_upBankApi)
		// 	.GetTransactions();
	}

	private async Task<ApiResponse<TransactionResponse>> GetTransactions(DateTimeOffset? since = null, DateTimeOffset? until = null, int pageSize = 20, string? nextPage = null)
	{
		// fix the test data in this to be correct transaction response form from the API - currently I think I accidentally serialised converted transactions lmao
		return ApiResponse.FromSuccess(await _upBankApi.LoadTestDataFromFile<TransactionResponse>("./RuleSystemTests/Data/TransactionForAccount.json"));
	}

	[Fact]
	public async void DoNothinglol()
	{
		/*
		// use this to anonymise data where the data from file is just a list of transactions and not a full response
		var a = await _upBankApi.LoadTestDataFromFile<List<UpTransaction>>("./RuleSystemTests/Data/StoredAccountSample.json");
		var accountId = Guid.NewGuid().ToString();
		var r = new Random();
		foreach (var upTransaction in a)
		{
			upTransaction.Id = $"{Guid.NewGuid()}";
			upTransaction.AccountId = accountId;
			var amount = r.Next(-10000, 10000);
			upTransaction.Amount = new MoneyObject()
			{
				ValueInBaseUnits = amount,
				Value = $"{amount / 100.0:f2}",
				CurrencyCode = "TEST"
			};

			if (upTransaction.HoldInfo is not null)
			{
				upTransaction.HoldInfo.Amount = upTransaction.Amount;
			}

			if (upTransaction.CardPurchaseMethod is not null)
			{
				upTransaction.CardPurchaseMethod.CardNumberSuffix = "1234";
			}

			// don't really care about these
			upTransaction.CreatedAt = DateTime.Now.AddMilliseconds(amount);
			upTransaction.SettledAt = upTransaction.CreatedAt;
			upTransaction.Description = $"{Guid.NewGuid()} - Description";
			upTransaction.RawText = $"{Guid.NewGuid()} - Raw text";
		}

		var c = JsonSerializer.Serialize(a,new JsonSerializerOptions()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			WriteIndented = true
		});
		*/
	}
}