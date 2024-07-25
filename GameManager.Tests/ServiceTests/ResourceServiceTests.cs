using FakeItEasy;
using GameManager.Server.Tests.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.DB;
using Server.Interfaces;
using Server.Services;
using static Shared.Constants.Constants;

namespace GameManager.Server.Tests.ServiceTests
{
	public class ResourceServiceTests
	{
		private ILogger<ResourceService> _logger;
		private IPlayerConnectionManager _connectionManager;
		private GameContext _context;
		private ResourceService _resourceService;

		private void Setup(string databaseName)
		{
			_logger = A.Fake<ILogger<ResourceService>>();
			_connectionManager = A.Fake<IPlayerConnectionManager>();

			var options = new DbContextOptionsBuilder<GameContext>()
				.UseInMemoryDatabase(databaseName: databaseName)
				.Options;
			_context = new GameContext(options);

			_resourceService = new ResourceService(_context, _logger, _connectionManager);
		}

		[Fact]
		public async Task UpdateResources_PlayerNotFound_ReturnsError()
		{
			// Arrange
			Setup("UpdateResources_PlayerNotFound");
			var resourceType = Coins;
			var resourceValue = 10;
			var socket = A.Fake<IWebSocketWrapper>();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((0, "Player not found"));

			// Act
			var result = await _resourceService.UpdateResources(resourceType, resourceValue, socket);

			// Assert
			Assert.Null(result.Payload.ResourceType);
			Assert.Equal(0, result.Payload.Balance);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Player not found", result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "UpdateResources attempt");
		}

		[Fact]
		public async Task UpdateResources_InsufficientCoins_ReturnsError()
		{
			// Arrange
			Setup("UpdateResources_InsufficientCoins");
			var resourceType = Coins;
			var resourceValue = -200;
			var playerId = 1;
			var socket = A.Fake<IWebSocketWrapper>();
			var player = new Player { DeviceID = "1234", PlayerName = "John", Id = playerId, Coins = 100, Rolls = 10 };

			_context.Players.Add(player);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((player.Id, null));
			A.CallTo(() => _connectionManager.GetLockObject(player.Id)).Returns(new SemaphoreSlim(1, 1));

			// Act
			var result = await _resourceService.UpdateResources(resourceType, resourceValue, socket);

			// Assert
			Assert.Equal(Coins, result.Payload.ResourceType);
			Assert.Equal(-100, result.Payload.Balance);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Insufficient coins", result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "UpdateResources attempt");
			_logger.VerifyLogged(LogLevel.Warning, $"Player {player.Id} has insufficient coins");
		}

		[Fact]
		public async Task UpdateResources_SuccessfulUpdate_ReturnsSuccess()
		{
			// Arrange
			Setup("UpdateResources_SuccessfulUpdate");
			var resourceType = Coins;
			var resourceValue = 100;
			var playerId = 1;
			var socket = A.Fake<IWebSocketWrapper>();
			var player = new Player { DeviceID = "1234", PlayerName = "John", Id = playerId, Coins = 100, Rolls = 10 };

			_context.Players.Add(player);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((player.Id, null));
			A.CallTo(() => _connectionManager.GetLockObject(player.Id)).Returns(new SemaphoreSlim(1, 1));

			// Act
			var result = await _resourceService.UpdateResources(resourceType, resourceValue, socket);

			// Assert
			Assert.Equal(Coins, result.Payload.ResourceType);
			Assert.Equal(200, result.Payload.Balance);
			Assert.Equal(Success, result.Payload.Status);
			Assert.Null(result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "UpdateResources attempt");
			_logger.VerifyLogged(LogLevel.Information, $"Player {player.Id} updated {resourceType} by {resourceValue}");
		}

		[Fact]
		public async Task SendGift_PlayerNotFound_ReturnsError()
		{
			// Arrange
			Setup("SendGift_PlayerNotFound");
			var resourceType = Coins;
			var resourceValue = 10;
			var recipientId = 2;
			var socket = A.Fake<IWebSocketWrapper>();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((0, "Player not found"));

			// Act
			var result = await _resourceService.SendGift(resourceType, resourceValue, recipientId, socket);

			// Assert
			Assert.Null(result.Payload.ResourceType);
			Assert.Equal(0, result.Payload.ResourceValue);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Player not found", result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "SendGift attempt");
		}

		[Fact]
		public async Task SendGift_CannotGiftToSelf_ReturnsError()
		{
			// Arrange
			Setup("SendGift_CannotGiftToSelf");
			var resourceType = Coins;
			var resourceValue = 10;
			var playerId = 1;
			var socket = A.Fake<IWebSocketWrapper>();

			var player = new Player { DeviceID = "1234", PlayerName = "John", Id = playerId, Coins = 100, Rolls = 10 };
			_context.Players.Add(player);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((playerId, null));

			// Act
			var result = await _resourceService.SendGift(resourceType, resourceValue, playerId, socket);

			// Assert
			Assert.Null(result.Payload.ResourceType);
			Assert.Equal(0, result.Payload.ResourceValue);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Can't gift to yourself", result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "SendGift attempt");
			_logger.VerifyLogged(LogLevel.Warning, $"Player {playerId} tried to send a gift to themselves");
		}

		[Fact]
		public async Task SendGift_RecipientNotFound_ReturnsError()
		{
			// Arrange
			Setup("SendGift_RecipientNotFound");
			var resourceType = Coins;
			var resourceValue = 10;
			var playerId = 1;
			var recipientId = 2;
			var socket = A.Fake<IWebSocketWrapper>();

			var player = new Player { DeviceID = "1234", PlayerName = "John", Id = playerId, Coins = 100, Rolls = 10 };
			_context.Players.Add(player);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((playerId, null));

			// Act
			var result = await _resourceService.SendGift(resourceType, resourceValue, recipientId, socket);

			// Assert
			Assert.Null(result.Payload.ResourceType);
			Assert.Equal(0, result.Payload.ResourceValue);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Recipient not found", result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "SendGift attempt");
			_logger.VerifyLogged(LogLevel.Warning, "Recipient not found");
		}

		[Fact]
		public async Task SendGift_InsufficientCoins_ReturnsError()
		{
			// Arrange
			Setup("SendGift_InsufficientCoins");
			var resourceType = Coins;
			var resourceValue = 500;
			var playerId = 1;
			var recipientId = 2;
			var socket = A.Fake<IWebSocketWrapper>();

			var player = new Player { DeviceID = "1234", PlayerName = "John", Id = playerId, Coins = 100, Rolls = 10 };
			var recipient = new Player { DeviceID = "5678", PlayerName = "Jane", Id = recipientId, Coins = 50, Rolls = 5 };

			_context.Players.AddRange(player, recipient);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((playerId, null));
			A.CallTo(() => _connectionManager.GetLockObject(playerId)).Returns(new SemaphoreSlim(1, 1));
			A.CallTo(() => _connectionManager.GetLockObject(recipientId)).Returns(new SemaphoreSlim(1, 1));

			// Act
			var result = await _resourceService.SendGift(resourceType, resourceValue, recipientId, socket);

			// Assert
			Assert.Equal(Coins, result.Payload.ResourceType);
			Assert.Equal(-400, result.Payload.ResourceValue);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Insufficient coins", result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "SendGift attempt");
			_logger.VerifyLogged(LogLevel.Warning, $"Player {playerId} has insufficient coins");
		}

		[Fact]
		public async Task SendGift_SuccessfulSend_ReturnsSuccess()
		{
			// Arrange
			Setup("SendGift_SuccessfulSend");
			var resourceType = Coins;
			var resourceValue = 50;
			var playerId = 1;
			var recipientId = 2;
			var socket = A.Fake<IWebSocketWrapper>();

			var player = new Player { DeviceID = "1234", PlayerName = "John", Id = playerId, Coins = 100, Rolls = 10 };
			var recipient = new Player { DeviceID = "5678", PlayerName = "Jane", Id = recipientId, Coins = 50, Rolls = 5 };

			_context.Players.AddRange(player, recipient);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.GetPlayerIdBySocket(socket, A<IEnumerable<Player>>._))
				.Returns((playerId, null));
			A.CallTo(() => _connectionManager.GetLockObject(playerId)).Returns(new SemaphoreSlim(1, 1));
			A.CallTo(() => _connectionManager.GetLockObject(recipientId)).Returns(new SemaphoreSlim(1, 1));

			// Act
			var result = await _resourceService.SendGift(resourceType, resourceValue, recipientId, socket);

			// Assert
			Assert.Equal(Coins, result.Payload.ResourceType);
			Assert.Equal(resourceValue, result.Payload.ResourceValue);
			Assert.Equal(Success, result.Payload.Status);
			Assert.Null(result.Payload.Error);

			// Log test
			_logger.VerifyLogged(LogLevel.Information, "SendGift attempt");
			_logger.VerifyLogged(LogLevel.Information, $"Player {playerId} sent {resourceValue} {resourceType} to {recipientId}");
		}
	}
}
