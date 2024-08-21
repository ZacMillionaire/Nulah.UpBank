using Nulah.Up.IntegrationTests.Helpers;
using Nulah.Up.IntegrationTests.Mocks;
using Nulah.UpApi.Domain.Api;
using Nulah.UpApi.Domain.Api.Accounts;
using Nulah.UpApi.Lib.ApiModels;
using Nulah.UpApi.Lib.ApiModels.Accounts;
using Xunit.Abstractions;

namespace Nulah.Up.IntegrationTests.AccountTests;

[Collection("integration")]
public class GetAccountsWithNoAccounts : TestBase<AccountFixture>
{
	public GetAccountsWithNoAccounts(AccountFixture accountFixture, ITestOutputHelper? outputHelper)
		: base(accountFixture, outputHelper)
	{
	}

	private readonly TestUpBankApi _upBankApi = new();

	[Fact]
	public async void ShouldReturn_Nothing()
	{
		_upBankApi.ReturnGetAccounts = GetEmptyAccounts;

		var controller = TestFixture.CreateController(_upBankApi);
		var accounts = await controller.GetAccounts();

		Assert.Equal(0, accounts.Count);
	}

	private async Task<ApiResponse<AccountsResponse>> GetEmptyAccounts(string? s)
	{
		return ApiResponse.FromSuccess(await _upBankApi.LoadTestDataFromFile<AccountsResponse>("./AccountTests/Data/EmptyAccountResponse.json"));
	}
}