using FakeItEasy;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;

namespace GameManager.Server.Tests.Extensions;
public static class LoggerExtensions
{
	public static void VerifyLogged<T>(this ILogger<T> logger, LogLevel level, string logMessage)
	{
		var (found, actualLevel, actualMessage) = logger.VerifyLog(logMessage);
		if (!found)
		{
			throw new XunitException($"No log message found containing '{logMessage}' at any loglevel");
		}

		if (actualLevel != level)
		{
			throw new XunitException(
				$"[{level}] {logMessage} [{actualLevel}] {actualMessage} Unexpected log level for log message");
		}
	}

	public static void VerifyNotLogged<T>(this ILogger<T> logger, LogLevel level, string logMessage)
	{
		var (found, actualLevel, actualMessage) = logger.VerifyLog(logMessage);
		if (found && actualLevel == level)
		{
			throw new XunitException(
				@$"Log message found containing '{logMessage}'
                    at level {level}{Environment.NewLine}Message: {actualMessage}");
		}
	}

	public static void VerifyNotLoggedAtLevel<T>(this ILogger<T> logger, LogLevel level)
	{
		var call = Fake.GetCalls(logger)
			.FirstOrDefault(call => (LogLevel?)call.Arguments[0] == level);

		if (call != null)
		{
			throw new XunitException(
				@$"Log message found at level {level}{Environment.NewLine}
                    Message: {call.Arguments[2]}");
		}
	}
	public static void VerifyLoggedAtLevel<T>(this ILogger<T> logger, LogLevel level)
	{
		var found = Fake.GetCalls(logger)
			.Any(call => (LogLevel?)call.Arguments[0] == level);

		if (!found)
		{
			throw new XunitException($"No log message found at level {level}");
		}
	}

	private static (bool found, LogLevel? level, string? message) VerifyLog<T>(this ILogger<T> logger, string message)
	{
		var call = Fake.GetCalls(logger)
			.FirstOrDefault(call => call.Arguments[2].ToString()
			.Contains(message, StringComparison.OrdinalIgnoreCase));

		return (call != null, (LogLevel?)call?.Arguments[0], call?.Arguments[2].ToString());
	}
}