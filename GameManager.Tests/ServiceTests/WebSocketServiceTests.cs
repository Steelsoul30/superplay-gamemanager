using FakeItEasy;
using Microsoft.Extensions.Logging;
using Server.Interfaces;
using Server.Services;
using System.Net.WebSockets;
using System.Text;
using GameManager.Server.Tests.Extensions;

namespace GameManager.Server.Tests.ServiceTests
{
	public class WebSocketServiceTests
	{
		private readonly IWebSocketWrapper _mockWebSocketWrapper;
		private readonly ILogger<WebSocketService> _mockLogger;
		private readonly List<ICommandHandler> _mockCommandHandlers;
		private readonly WebSocketService _webSocketService;

		public WebSocketServiceTests()
		{
			_mockWebSocketWrapper = A.Fake<IWebSocketWrapper>();
			_mockLogger = A.Fake<ILogger<WebSocketService>>();
			_mockCommandHandlers = new List<ICommandHandler>();
			_webSocketService = new WebSocketService(_mockLogger, _mockCommandHandlers);
		}

		[Fact]
		public async Task ListenOnSocket_ClosesSocketOnCloseMessage()
		{
			// Arrange
			var closeResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
			A.CallTo(() => _mockWebSocketWrapper.ReceiveAsync(A<ArraySegment<byte>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(closeResult));

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper);

			// Assert
			A.CallTo(() => _mockWebSocketWrapper.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", A<CancellationToken>._))
				.MustHaveHappenedOnceExactly();
		}

		[Fact]
		public async Task ListenOnSocket_ExecutesCommandHandlerOnValidCommand()
		{
			// Arrange
			var message = "{\"command\":\"testCommand\"}";
			var receivedBytes = Encoding.UTF8.GetBytes(message);
			var receiveResult = new WebSocketReceiveResult(receivedBytes.Length, WebSocketMessageType.Text, true);
			var closeResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);

			A.CallTo(() => _mockWebSocketWrapper.ReceiveAsync(A<ArraySegment<byte>>._, A<CancellationToken>._))
				.Invokes((ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
				{
					// Copy the messageBytes into the buffer.Array at the offset position
					Array.Copy(receivedBytes, 0, buffer.Array, buffer.Offset, receivedBytes.Length);
				})
				.ReturnsLazily((ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
					Task.FromResult(receiveResult))
				.Once() // Simulate receiving the message once
				// Then simulate receiving a close message
				.Then
				.Returns(Task.FromResult(closeResult));


			var mockCommandHandler = A.Fake<ICommandHandler>();
			A.CallTo(() => mockCommandHandler.CanHandle("testCommand")).Returns(true);
			_mockCommandHandlers.Add(mockCommandHandler);

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper);

			// Assert
			A.CallTo(() => mockCommandHandler.HandleAsync(A<string>._, _mockWebSocketWrapper)).MustHaveHappenedOnceExactly();
		}

		[Fact]
		public async Task ListenOnSocket_LogsWarningOnUnknownCommand()
		{
			// Arrange
			var message = "{\"command\":\"unknownCommand\"}";
			var receivedBytes = Encoding.UTF8.GetBytes(message);
			var receiveResult = new WebSocketReceiveResult(receivedBytes.Length, WebSocketMessageType.Text, true);
			A.CallTo(() => _mockWebSocketWrapper.ReceiveAsync(A<ArraySegment<byte>>._, A<CancellationToken>._))
				.Invokes((ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
				{
					// Copy the messageBytes into the buffer.Array at the offset position
					Array.Copy(receivedBytes, 0, buffer.Array, buffer.Offset, receivedBytes.Length);
				})
				.ReturnsLazily((ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
					Task.FromResult(receiveResult))
				.Once() // Simulate receiving the message once
				// Then simulate receiving a close message
				.Then
				.Returns(Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)));

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper);

			// Assert
			//A.CallTo(() => _mockLogger.LogWarning("Unknown command")).MustHaveHappened();
			_mockLogger.VerifyLogged(LogLevel.Warning, "Unknown Command");
		}

		[Fact]
		public async Task ListenOnSocket_LogsErrorOnWebSocketException()
		{
			// Arrange
			A.CallTo(() => _mockWebSocketWrapper.ReceiveAsync(A<ArraySegment<byte>>._, A<CancellationToken>._))
				.ThrowsAsync(new WebSocketException("Socket closed unexpectedly"));

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper);

			// Assert
			_mockLogger.VerifyLoggedAtLevel(LogLevel.Error);
		}
	}
}
