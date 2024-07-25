using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.DB;
using Server.Interfaces;
using Server.Services;
using static Shared.Constants.Constants;

namespace GameManager.Server.Tests.ServiceTests
{
	public class PlayerServiceTests
	{
		private readonly ILogger<PlayerService> _logger;
		private readonly IPlayerConnectionManager _connectionManager;
		private readonly GameContext _context;
		private readonly PlayerService _playerService;

		public PlayerServiceTests()
		{
			_logger = A.Fake<ILogger<PlayerService>>();
			_connectionManager = A.Fake<IPlayerConnectionManager>();

			var options = new DbContextOptionsBuilder<GameContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // use different database name for each test
				.Options;
			_context = new GameContext(options);

			_playerService = new PlayerService(_logger, _connectionManager, _context);
		}

		[Fact]
		public async Task Login_PlayerNotFound_ReturnsPlayerNotRegistered()
		{
			// Arrange
			var deviceId = "test-device-id";
			var socket = A.Fake<IWebSocketWrapper>();

			// Act
			var result = await _playerService.Login(deviceId, socket);

			// Assert
			Assert.Equal(0, result.Payload.PlayerId);
			Assert.Null(result.Payload.PlayerName);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Player not registered", result.Payload.Error);
		}

		[Fact]
		public async Task Login_PlayerAlreadyLoggedIn_ReturnsPlayerAlreadyLoggedIn()
		{
			// Arrange
			var deviceId = "test-device-id";
			var socket = A.Fake<IWebSocketWrapper>();
			var player = new Player { Id = 4, DeviceID = deviceId, PlayerName = "TestPlayer" };

			_context.Players.Add(player);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.TryAddPlayer(player.Id, socket)).Returns(false);

			// Act
			var result = await _playerService.Login(deviceId, socket);

			// Assert
			Assert.Equal(player.Id, result.Payload.PlayerId);
			Assert.Equal(player.PlayerName, result.Payload.PlayerName);
			Assert.Equal(Error, result.Payload.Status);
			Assert.Equal("Player already logged in", result.Payload.Error);
		}

		[Fact]
		public async Task Login_SuccessfulLogin_ReturnsSuccess()
		{
			// Arrange
			var deviceId = "test-device-id";
			var socket = A.Fake<IWebSocketWrapper>();
			var player = new Player { Id = 1, DeviceID = deviceId, PlayerName = "TestPlayer" };

			_context.Players.Add(player);
			await _context.SaveChangesAsync();

			A.CallTo(() => _connectionManager.TryAddPlayer(player.Id, socket)).Returns(true);

			// Act
			var result = await _playerService.Login(deviceId, socket);

			// Assert
			Assert.Equal(player.Id, result.Payload.PlayerId);
			Assert.Equal(player.PlayerName, result.Payload.PlayerName);
			Assert.Equal(Success, result.Payload.Status);
			Assert.Null(result.Payload.Error);
		}
	}
}
