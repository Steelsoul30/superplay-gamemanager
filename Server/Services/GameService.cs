using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Shared.Helpers;
using Shared.Models.Messages;
using static Shared.Constants.Constants;

namespace Server.Services
{
	public class GameService(ILogger<GameService> logger, GameContext context) : IGameService
	{
		private readonly List<WebSocket> _sockets = [];
		private readonly ConcurrentDictionary<string, WebSocket> _playersConnectedDict = new();
		private readonly object _lock = new();

		private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		public async Task ListenOnSocket(WebSocket socket)
		{
			logger.LogInformation("Socket added");
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
						logger.LogDebug("Received message: {data}", data);
						var command = JsonHelper.GetElementByKey("command", data);
						switch (command)
						{
							case LoginCommand:
								var request = JsonSerializer.Deserialize<LoginRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {command} command");
								var response = await Login(request.Payload.DeviceId, socket);
								var loginResponse = JsonSerializer.Serialize(response, _jsonOptions);
								await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(loginResponse)), WebSocketMessageType.Text, true, CancellationToken.None);
								break;
							case UpdateResourcesCommand:
								var updateRequest = JsonSerializer.Deserialize<UpdateResourcesRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {command} command");
								var updateResourcesResponse = await UpdateResources(updateRequest.Payload.ResourceType, updateRequest.Payload.ResourceValue, socket);
								var updateResponse = JsonSerializer.Serialize(updateResourcesResponse, _jsonOptions);
								await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(updateResponse)), WebSocketMessageType.Text, true, CancellationToken.None);
								break;
							case Empty:
								logger.LogWarning("message did not contain a command");
								break;
							default:
								logger.LogWarning("Unknown command");
								break;
						}
					}
					catch (WebSocketException ex)
					{
						logger.LogError("Socket closed unexpectedly - {ex}", ex.Message);
						break;
					}
					catch (KeyNotFoundException)
					{
						logger.LogWarning("message did not contain a command2");
					}
					catch (SerializationException ex)
					{
						logger.LogError("Bad request - {ex}", ex.Message);
					}
				}
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
				lock (_lock)
				{
					_sockets.Remove(socket);
				}
				logger.LogInformation("Socket removed");
			});
		}

		private async Task<LoginResponse> Login(string deviceId, WebSocket socket)
		{
			logger.LogInformation("Login attempt");
			var player = await GetPlayerNameByDeviceId(deviceId);
			if (player == string.Empty)
			{
				logger.LogWarning($"Player not found for device {deviceId}");
				return new LoginResponse(new LoginResponsePayload(player, Error, "Player not registered"));
			}
			else if (!_playersConnectedDict.TryAdd(player, socket))
			{
				logger.LogWarning($"Player {player} is already logged in");
				return new LoginResponse(new LoginResponsePayload(player, Error, "Player already logged in"));
			}
			else
			{

				logger.LogInformation($"Player {player} logged in");
				return new LoginResponse(new LoginResponsePayload(player, Success, string.Empty));
			}
		}

		private async Task<UpdateResourcesResponse> UpdateResources(string resourceType, int resourceValue, WebSocket socket)
		{
			logger.LogInformation("UpdateResources attempt");
			var player = _playersConnectedDict.FirstOrDefault(p => p.Value == socket).Key;
			if (player == null)
			{
				logger.LogWarning("Player not logged in");
				return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(Empty, 0, Error, "Player not logged in"));
			}
			var playerEntity = await context.Players.Where(p => p.PlayerName == player).FirstOrDefaultAsync();
			if (playerEntity == null)
			{
				logger.LogError("Player not found in database");
				return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(Empty, 0, Error, "Player not found. Internal Error"));
			}

            var playerCopy = new Player()
                { PlayerName = playerEntity.PlayerName, Coins = playerEntity.Coins, Rolls = playerEntity.Rolls };
			switch (resourceType)
			{
				case Coins:
                    playerCopy.Coins += resourceValue;
					if (playerCopy.Coins < 0)
					{
						logger.LogWarning("Player {Player} has insufficient coins", player);
						return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(Coins, playerCopy.Coins, Error, "Insufficient coins"));
					}
					break;
				case Rolls:
                    playerCopy.Rolls += resourceValue;
					if (playerCopy.Rolls < 0)
					{
						logger.LogWarning("Player {Player} has insufficient rolls", player);
						return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(Rolls, playerCopy.Rolls, Error, "Insufficient rolls"));
					}
					break;
				default:
					logger.LogWarning("Unknown resource type requested");
					return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(Empty, 0, Error, "Unknown resource type"));
			}
			playerEntity.Coins = playerCopy.Coins;
			playerEntity.Rolls = playerCopy.Rolls;
			await context.SaveChangesAsync();
			logger.LogInformation("Player {Player} updated {ResourceType} by {ResourceValue}", player, resourceType, resourceValue);
			return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(resourceType == Coins ? Coins : Rolls,
                resourceType == Coins ? playerEntity.Coins : playerEntity.Rolls,
                Success,
                string.Empty));
		}

		private async Task<string> GetPlayerNameByDeviceId(string deviceId)
		{
			logger.LogInformation("GetPlayerNameByDeviceId attempt");
			var player = await context.Players.Where(p => p.DeviceID == deviceId).SingleOrDefaultAsync();
			return player?.PlayerName ?? string.Empty;
		}
	}
}
