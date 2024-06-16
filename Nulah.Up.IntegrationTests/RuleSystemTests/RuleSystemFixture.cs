using Marten;
using Microsoft.Extensions.DependencyInjection;
using Nulah.Up.IntegrationTests.AccountTests;
using Nulah.Up.IntegrationTests.Mocks;
using Nulah.UpApi.Lib.Controllers;

namespace Nulah.Up.IntegrationTests.RuleSystemTests;

public class RuleSystemFixture : Fixture
{
	public TransactionController CreateController(TestUpBankApi testUpBankApi) => new TransactionController(
		testUpBankApi,
		Host.Services.GetRequiredService<IDocumentStore>(),
		new CategoryController(
			testUpBankApi,
			Host.Services.GetRequiredService<IDocumentStore>(),
			CreateLogger<CategoryController>()
		),
		CreateLogger<TransactionController>()
	);

	public RuleSystemFixture(TestHostBuilder testHost) : base(testHost)
	{
	}
}