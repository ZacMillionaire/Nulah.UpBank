using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nulah.Up.IntegrationTests.Helpers;
using Nulah.Up.IntegrationTests.Mocks;
using Nulah.UpApi.Lib.Controllers;
using Nulah.UpApi.Lib.Models;
using Weasel.Core;
using Xunit.Abstractions;

namespace Nulah.Up.IntegrationTests.AccountTests;

[CollectionDefinition("integration")]
public class HostFixture : ICollectionFixture<TestHostBuilder>
{
	// never instantiated, used only for CollectionDefinition in xUnit
}

public class TestHostBuilder
{
	public readonly IHost Host;

	public TestHostBuilder()
	{
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("testsettings.json", false)
			.Build();

		var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();

		host.ConfigureAppConfiguration((context, builder) =>
		{
			builder
				.AddJsonFile("testsettings.json", false);
		});

		host.ConfigureServices((context, collection) =>
		{
			collection.AddMarten(options =>
				{
					// Establish the connection string to your Marten database
					options.Connection(configuration.GetConnectionString("Postgres"));
					options.DisableNpgsqlLogging = true;

					options.UseSystemTextJsonForSerialization();
					options.AutoCreateSchemaObjects = AutoCreate.All;

					options.Schema.For<UpAccount>().Metadata(x =>
					{
						x.LastModified.MapTo(y => y.ModifiedAt);
					});

					// options.Schema.For<UpTransaction>()
					// 	.Index(x => x.AccountId, x => x.Name = "account_id")
					// 	.Index(x => x.Category.Id, x => x.Name = "category_id");
					//
					// options.Schema.For<UpCategory>()
					// 	.Index(x => x.ParentCategoryId, x => x.Name = "parent_category_id");
				})
				// .ApplyAllDatabaseChangesOnStartup()
				// .OptimizeArtifactWorkflow()
				.UseLightweightSessions();
		});

		Host = host.Build();
	}
}

public abstract class Fixture
{
	private ITestOutputHelper? _helper;
	private readonly TestHostBuilder _testHostBuilder;
	protected IHost Host => _testHostBuilder.Host;

	protected Fixture(TestHostBuilder testHost)
	{
		_testHostBuilder = testHost;
	}

	/// <summary>
	/// Sets any dependencies required by fixtures
	/// </summary>
	/// <param name="outputHelper"></param>
	public void SetFixtureDefaults(ITestOutputHelper? outputHelper)
	{
		_helper = outputHelper;
	}

	protected ILogger<TLogTarget> CreateLogger<TLogTarget>()
	{
		if (_helper == null)
		{
			throw new Exception("Logger has not been set on base fixture");
		}

		return LoggerFactory.Create(builder =>
				builder.AddColorConsoleLogger(_helper)
			)
			.CreateLogger<TLogTarget>();
	}
}

public class AccountFixture : Fixture
{
	public AccountController CreateController(TestUpBankApi testUpBankApi) => new AccountController(
		testUpBankApi,
		Host.Services.GetRequiredService<IDocumentStore>(),
		CreateLogger<AccountController>()
	);

	public AccountFixture(TestHostBuilder testHost) : base(testHost)
	{
	}
}