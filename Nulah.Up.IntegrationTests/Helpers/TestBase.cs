using Nulah.Up.IntegrationTests.AccountTests;
using Xunit.Abstractions;

namespace Nulah.Up.IntegrationTests.Helpers;

public class TestBase<T> : IClassFixture<T>
	where T : Fixture
{
	protected readonly T TestFixture;
	protected readonly ITestOutputHelper? TestOutput;

	protected TestBase(T testFixture, ITestOutputHelper? output)
	{
		TestFixture = testFixture;
		TestOutput = output;

		testFixture.SetFixtureDefaults(output);
	}
}