using Microsoft.Extensions.Logging;
using Moq;
using Server.Interfaces;
using Server.Services;
using System.Net.WebSockets;
using System.Text;
using GameManager.Server.Tests.Extensions;

namespace GameManager.Server.Tests.ServiceTests
{
	public class WebSocketServiceTests
	{
		private readonly Mock<IWebSocketWrapper> _mockWebSocketWrapper;
		private readonly Mock<ILogger<WebSocketService>> _mockLogger;
		private readonly List<Mock<ICommandHandler>> _mockCommandHandlers;
		private readonly WebSocketService _webSocketService;

		public WebSocketServiceTests()
		{
			_mockWebSocketWrapper = new Mock<IWebSocketWrapper>();
			_mockLogger = new Mock<ILogger<WebSocketService>>();
			_mockCommandHandlers = new List<Mock<ICommandHandler>>();
			_webSocketService = new WebSocketService(_mockLogger.Object, _mockCommandHandlers.Select(m => m.Object));
		}

		[Fact]
		public async Task ListenOnSocket_ClosesSocketOnCloseMessage()
		{
			// Arrange
			var closeResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
			_mockWebSocketWrapper.SetupSequence(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(closeResult);

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper.Object);

			// Assert
			_mockWebSocketWrapper.Verify(ws => ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task ListenOnSocket_ExecutesCommandHandlerOnValidCommand()
		{
			// Arrange
			var message = "{\"command\":\"testCommand\"}";
			var closeResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
			var receivedBytes = Encoding.UTF8.GetBytes(message);
			var receiveResult = new WebSocketReceiveResult(receivedBytes.Length, WebSocketMessageType.Text, true);
			var sequence = _mockWebSocketWrapper.SetupSequence(ws =>
					ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()));

				sequence = sequence.ReturnsAsync((ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
				{
					Array.Copy(receivedBytes, buffer.Array, receivedBytes.Length);
					return receiveResult;
				});
				sequence.ReturnsAsync(closeResult);


			var mockCommandHandler = new Mock<ICommandHandler>();
			mockCommandHandler.Setup(h => h.CanHandle("testCommand")).Returns(true);
			_mockCommandHandlers.Add(mockCommandHandler);

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper.Object);

			// Assert
			mockCommandHandler.Verify(h => h.HandleAsync(It.IsAny<string>(), _mockWebSocketWrapper.Object), Times.Once);
		}

		[Fact]
		public async Task ListenOnSocket_LogsWarningOnUnknownCommand()
		{
			// Arrange
			var message = "{\"command\":\"unknownCommand\"}";
			var buffer = Encoding.UTF8.GetBytes(message);
			var receiveResult = new WebSocketReceiveResult(buffer.Length, WebSocketMessageType.Text, true);
			_mockWebSocketWrapper.SetupSequence(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(receiveResult)
				.ReturnsAsync(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)); // Close connection after handling command

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper.Object);

			// Assert
			_mockLogger.VerifyLog(LogLevel.Warning, "Unknown command", Times.Once());
		}

		[Fact]
		public async Task ListenOnSocket_LogsErrorOnWebSocketException()
		{
			// Arrange
			_mockWebSocketWrapper.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new WebSocketException("Socket closed unexpectedly"));

			// Act
			await _webSocketService.ListenOnSocket(_mockWebSocketWrapper.Object);

			// Assert
			_mockLogger.VerifyLog(LogLevel.Error, "Socket closed unexpectedly", Times.Once());
		}
	}
}