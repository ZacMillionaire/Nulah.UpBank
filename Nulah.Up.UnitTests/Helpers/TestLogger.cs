using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Nulah.Up.IntegrationTests.Helpers;

public sealed class XUnitTestLogger : ILogger
{
	private readonly string _name;
	private readonly ITestOutputHelper _output;
	public XUnitTestLogger(string name, ITestOutputHelper output) => (_name, _output) = (name, output);

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		_output.WriteLine($"[{eventId.Id,2}: {logLevel,-12}] {_name} - {formatter(state, exception)}");
	}
}

public sealed class XUnitTestLoggerProvider : ILoggerProvider
{
	private readonly ConcurrentDictionary<string, XUnitTestLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
	private readonly ITestOutputHelper _output;

	public XUnitTestLoggerProvider(ITestOutputHelper output) => _output = output;

	public ILogger CreateLogger(string categoryName) =>
		_loggers.GetOrAdd(
			categoryName,
			name =>
				new XUnitTestLogger(name, _output)
		);

	public void Dispose()
	{
		_loggers.Clear();
	}
}

public static class XUnitTestLoggerExtensions
{
	public static ILoggingBuilder AddColorConsoleLogger(this ILoggingBuilder builder, ITestOutputHelper? output)
	{
		//builder.AddConfiguration();

		builder.Services.AddSingleton(output);
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, XUnitTestLoggerProvider>());

		return builder;
	}
}