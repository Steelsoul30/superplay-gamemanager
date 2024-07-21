using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Shared.Helpers;
using Shared.Models.Messages;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using static Shared.Constants.Constants;

namespace Manager
{
	public class GameService(ILogger<GameService> logger, GameContext context) : IGameService
	{
		private readonly List<WebSocket> _sockets = [];
		private readonly ConcurrentDictionary<string, WebSocket> _playersConnectedDict = new();
		private readonly object _lock = new();
		private readonly ILogger<GameService> _logger = logger;
		private readonly GameContext _context = context;

		private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		async public Task AddSocket(WebSocket socket)
		{
			_logger.LogInformation("Socket added");
			lock (_lock)
			{
				_sockets.Add(socket);
			}

			await Task.Run(async () =>
			{
				var buffer = new byte[1024 * 4];
				while (true)
				{
					try
					{
						var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
						if (result.MessageType == WebSocketMessageType.Close)
						{
							break;
						}
						var data = Encoding.UTF8.GetString(buffer, 0, result.Count);
						if (data == null)
						{
							_logger.LogWarning("Data is null");
							continue;
						}
						_logger.LogDebug("Received message: {data}", data);
						var command = JsonHelper.GetElementByKey("command", data);
						switch (command)
						{
							case LoginCommand:
								var request = JsonSerializer.Deserialize<LoginRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {command} command");
								var response = await Login(request.Payload.DeviceID, socket);
								var loginResponse = JsonSerializer.Serialize(response, _jsonOptions);
								await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(loginResponse)), WebSocketMessageType.Text, true, CancellationToken.None);
								break;
							case UpdateResourcesCommand:
								var updateRequest = JsonSerializer.Deserialize<UpdateResourcesRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {command} command");
								var UpdateResourcesResponse = await UpdateResources(updateRequest.Payload.ResourceType, updateRequest.Payload.ResourceValue, socket);
								var updateResponse = JsonSerializer.Serialize(UpdateResourcesResponse, _jsonOptions);
								await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(updateResponse)), WebSocketMessageType.Text, true, CancellationToken.None);
								break;
							case Empty:
								_logger.LogWarning("message did not contain a command");
								break;
							default:
								_logger.LogWarning("Unknown command");
								break;
						}
					}
					catch (WebSocketException ex)
					{
						_logger.LogError("Socket closed unexpectedly - {ex}", ex.Message);
						break;
					}
					catch (KeyNotFoundException ex)
					{

					}
					catch (SerializationException ex)
					{
						_logger.LogError("Bad request - {ex}", ex.Message);
					}
				}
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
				lock (_lock)
				{
					_sockets.Remove(socket);
				}
				_logger.LogInformation("Socket removed");
			});
		}

		public async Task<LoginResponse> Login(string deviceId, WebSocket socket)
		{
			_logger.LogInformation("Login attempt");
			var player = await GetPlayerNameByDeviceId(deviceId);
			if (player == string.Empty)
			{
				_logger.LogWarning($"Player not found for device {deviceId}");
				return new LoginResponse(new LoginResponsePayload(player, Error, "Player not registered"));
			}
			else if (!_playersConnectedDict.TryAdd(player, socket))
			{
				_logger.LogWarning($"Player {player} is already logged in");
				return new LoginResponse(new LoginResponsePayload(player, Error, "Player already logged in"));
			}
			else
			{

				_logger.LogInformation($"Player {player} logged in");
				return new LoginResponse(new LoginResponsePayload(player, Success, string.Empty));
			}
		}

		public async Task<UpdateResourcesResponse> UpdateResources(string resourceType, int resourceValue, WebSocket socket)
		{
			_logger.LogInformation("UpdateResources attempt");
			var player = _playersConnectedDict.FirstOrDefault(p => p.Value == socket).Key;
			if (player == null)
			{
				_logger.LogWarning("Player not logged in");
				return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(0, Error, "Player not logged in"));
			}
			var playerEntity = await _context.Players.Where(p => p.PlayerName == player).FirstOrDefaultAsync();
			if (playerEntity == null)
			{
				_logger.LogError("Player not found in database");
				return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(0, Error, "Player not found. Internal Error"));
			}
			switch (resourceType)
			{
				case Coins:
					playerEntity.Coins += resourceValue;
					if (playerEntity.Coins < 0)
					{
						_logger.LogWarning("Player {Player} has insufficient coins", player);
						return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(playerEntity.Coins, Error, "Insufficient coins"));
					}
					break;
				case Rolls:
					playerEntity.Rolls += resourceValue;
					if (playerEntity.Rolls < 0)
					{
						_logger.LogWarning("Player {Player} has insufficient rolls", player);
						return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(playerEntity.Rolls, Error, "Insufficient rolls"));
					}
					break;
				default:
					_logger.LogWarning("Unknown resource type requested");
					return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(0, Error, "Unknown resource type"));
			}
			_context.Players.Update(playerEntity);
			await _context.SaveChangesAsync();
			_logger.LogInformation("Player {Player} updated {ResourceType} by {ResourceValue}", player, resourceType, resourceValue);
			return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(resourceType == Coins ? playerEntity.Coins : playerEntity.Rolls, Success, string.Empty));
		}

		async private Task<string> GetPlayerNameByDeviceId(string deviceId)
		{
			_logger.LogInformation("GetPlayerNameByDeviceId attempt");
			var player = await _context.Players.Where(p => p.DeviceID == deviceId).SingleOrDefaultAsync();
			return player?.PlayerName ?? string.Empty;
		}
	}
}
