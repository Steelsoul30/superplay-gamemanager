using Microsoft.Extensions.Logging;
using Moq;

namespace GameManager.Server.Tests.Extensions;

public static class LoggerMockExtensions
{
	public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string message, Times times)
	{
		loggerMock.Verify(
			x => x.Log(
				// Check if the log level matches
				It.Is<LogLevel>(l => l == logLevel),
				// This can be ignored if you're not using EventIds
				It.IsAny<EventId>(),
				// Check if the logged state contains the message
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
				// This can be ignored if you're not checking exceptions
				It.IsAny<Exception>(),
				// Func to format state and exception into a log message string
				It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)!),
			times);
	}
}