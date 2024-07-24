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
		private static readonly ConcurrentDictionary<int, WebSocket> _playersConnectedDict = new();
		private static readonly ConcurrentDictionary<int, SemaphoreSlim> _lockObjects = new();

		private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		public async Task ListenOnSocket(WebSocket socket)
		{
			logger.LogInformation("New connection established");

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
								await socket.SendAsync(response);
								break;
							case UpdateResourcesCommand:
								var updateRequest = JsonSerializer.Deserialize<UpdateResourcesRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {command} command");
								var updateResourcesResponse = await UpdateResources(updateRequest.Payload.ResourceType, updateRequest.Payload.ResourceValue, socket);
								await socket.SendAsync(updateResourcesResponse);
								break;
							case SendGiftCommand:
								var sendGiftRequest = JsonSerializer.Deserialize<SendGiftRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {command} command");
								var sendGiftResponse = await SendGift(sendGiftRequest.Payload.ResourceType, sendGiftRequest.Payload.ResourceValue, sendGiftRequest.Payload.RecipientId, socket);
								if (sendGiftResponse.Payload.Status == Error)
								{
									await socket.SendAsync(sendGiftResponse); // send to the sender
								}
								else if (_playersConnectedDict.TryGetValue(sendGiftRequest.Payload.RecipientId, out var recipientSocket))
								{
									await recipientSocket.SendAsync(sendGiftResponse); // send to the recipient
								}
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
				logger.LogInformation("Connection terminated");
			});
		}

		private async Task<LoginResponse> Login(string deviceId, WebSocket socket)
		{
			logger.LogInformation("Login attempt");
			var player = await context.Players.Where(p => p.DeviceID == deviceId).SingleOrDefaultAsync();
			if (player == null)
			{
				logger.LogWarning($"Player not found for device {deviceId}");
				return new LoginResponse(new LoginResponsePayload(0, null, Error, "Player not registered"));
			}

			if (!_playersConnectedDict.TryAdd(player.Id, socket))
			{
				logger.LogWarning($"Player {player.Id}: {player.PlayerName} is already logged in");
				return new LoginResponse(new LoginResponsePayload(player.Id, player.PlayerName, Error, "Player already logged in"));
			}

			logger.LogInformation($"Player {player.Id}: {player.PlayerName} logged in");
			return new LoginResponse(new LoginResponsePayload(player.Id, player.PlayerName, Success, null));
		}

		private async Task<UpdateResourcesResponse> UpdateResources(string resourceType, int resourceValue, WebSocket socket)
		{
			logger.LogInformation("UpdateResources attempt");
			var (playerId, verifyMsg) = await GetPlayerIdBySocket(socket);
			if (playerId == 0)
			{
				return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(null, 0, Error, verifyMsg));
			}

			var objLock = _lockObjects.GetOrAdd(playerId, new SemaphoreSlim(1));
			await objLock.WaitAsync();
			try
			{
				var playerEntity = await context.Players.SingleAsync(p => p.Id == playerId);
				var coins = playerEntity.Coins;
				var rolls = playerEntity.Rolls;
				switch (resourceType)
				{
					case Coins:
						coins += resourceValue;
						if (coins < 0)
						{
							logger.LogWarning("Player {Player} has insufficient coins", playerEntity.Id);
							return new UpdateResourcesResponse(
								new UpdateResourcesResponsePayload(Coins, coins, Error, "Insufficient coins"));
						}

						break;
					case Rolls:
						rolls += resourceValue;
						if (rolls < 0)
						{
							logger.LogWarning("Player {Player} has insufficient rolls", playerEntity.Id);
							return new UpdateResourcesResponse(
								new UpdateResourcesResponsePayload(Rolls, rolls, Error, "Insufficient rolls"));
						}

						break;
					default:
						logger.LogWarning("Unknown resource type requested");
						return new UpdateResourcesResponse(
							new UpdateResourcesResponsePayload(resourceType, 0, Error, "Unknown resource type"));
				}

				playerEntity.Coins = coins;
				playerEntity.Rolls = rolls;
				await context.SaveChangesAsync();
				logger.LogInformation("Player {Player} updated {ResourceType} by {ResourceValue}", playerEntity.Id, resourceType, resourceValue);
				return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(resourceType == Coins ? Coins : Rolls,
					resourceType == Coins ? playerEntity.Coins : playerEntity.Rolls,
					Success,
					null));
			}
			finally
			{
				objLock.Release();
			}
		}

		private async Task<SendGiftResponse> SendGift(string resourceType, int resourceValue, int recipientId, WebSocket socket)
		{
			logger.LogInformation("SendGift attempt");
			if (resourceValue <= 0)
			{
				return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, "Can't gift a non positive amount"));
			}
			var (playerId, verifyMsg) = await GetPlayerIdBySocket(socket);
			if (playerId == 0)
			{
				return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, verifyMsg));
			}
			if (playerId == recipientId)
			{
				logger.LogWarning("Player {Player} tried to send a gift to themselves", playerId);
				return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, "Can't gift to yourself"));
			}
			if (await context.Players.SingleOrDefaultAsync(p => p.Id == recipientId) == null)
			{
				logger.LogWarning("Recipient not found");
				return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, "Recipient not found"));
			}
			var firstId = Math.Min(playerId, recipientId);
			var secondId = Math.Max(playerId, recipientId);
			var objLock = _lockObjects.GetOrAdd(firstId, new SemaphoreSlim(1));
			var objLock2 = _lockObjects.GetOrAdd(secondId, new SemaphoreSlim(1));
			await objLock.WaitAsync();
			await objLock2.WaitAsync();
			try
			{
				var playerEntity = await context.Players.SingleAsync(p => p.Id == playerId);
				var coins = playerEntity.Coins;
				var rolls = playerEntity.Rolls;
				switch (resourceType)
				{
					case Coins:
						coins -= resourceValue;
						if (coins < 0)
						{
							logger.LogWarning("Player {Player} has insufficient coins", playerEntity.Id);
							return new SendGiftResponse(new SendGiftResponsePayload(Coins, coins, playerEntity.Coins, null, Error, "Insufficient coins"));
						}

						break;
					case Rolls:
						rolls -= resourceValue;
						if (rolls < 0)
						{
							logger.LogWarning("Player {Player} has insufficient rolls", playerEntity.Id);
							return new SendGiftResponse(new SendGiftResponsePayload(Rolls, rolls, playerEntity.Rolls, null, Error, "Insufficient rolls"));
						}

						break;
					default:
						logger.LogWarning("Unknown resource type requested");
						return new SendGiftResponse(new SendGiftResponsePayload(resourceType, 0, 0, null, Error, "Unknown resource type"));
				}

				var recipientEntity = await context.Players.SingleAsync(p => p.Id == recipientId);
				recipientEntity.Coins += playerEntity.Coins - coins;
				recipientEntity.Rolls += playerEntity.Rolls - rolls;
				playerEntity.Coins = coins;
				playerEntity.Rolls = rolls;

				await context.SaveChangesAsync();
				logger.LogInformation("Player {Player} sent {ResourceValue} {ResourceType} to {Recipient}", playerId,
					resourceValue, resourceType, recipientId);
				return new SendGiftResponse(new SendGiftResponsePayload(resourceType == Coins ? Coins : Rolls,
					resourceValue, resourceType == Coins ? recipientEntity.Coins : recipientEntity.Rolls, playerEntity.PlayerName,
					Success,
					null));

			}
			finally
			{
				objLock2.Release();
				objLock.Release();
			}
		}

		private async Task<(int, string)> GetPlayerIdBySocket(WebSocket socket)
		{
			var player = _playersConnectedDict.FirstOrDefault(p => p.Value == socket).Key;
			if (player == 0)
			{
				logger.LogWarning("Player not logged in");
				return (0, "Player not logged in");
			}

			var playerEntity = await context.Players.SingleOrDefaultAsync(p => p.Id == player);
			if (playerEntity != null)
				return (playerEntity.Id, Empty);
			logger.LogError("Player not found in database. Shouldn't happen as the player is logged in");
			return (0, "Player not found. Internal Error");

		}
	}
}
