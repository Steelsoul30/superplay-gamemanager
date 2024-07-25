using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Interfaces;
using Shared.Models.Messages;
using static Shared.Constants.Constants;

namespace Server.Services;

public class ResourceService (GameContext context, ILogger<ResourceService> logger, IPlayerConnectionManager connectionManager): IResourceService
{
	public async Task<UpdateResourcesResponse> UpdateResources(string resourceType, int resourceValue, IWebSocketWrapper socket)
	{
		logger.LogInformation("UpdateResources attempt");
		var players = await context.Players.ToListAsync();
		var (playerId, verifyMsg) = connectionManager.GetPlayerIdBySocket(socket, players);
		if (playerId == 0)
		{
			return new UpdateResourcesResponse(new UpdateResourcesResponsePayload(null, 0, Error, verifyMsg));
		}

		var objLock = connectionManager.GetLockObject(playerId);
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

	public async Task<SendGiftResponse> SendGift(string resourceType, int resourceValue, int recipientId, IWebSocketWrapper socket)
	{
		logger.LogInformation("SendGift attempt");
		if (resourceValue <= 0)
		{
			return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, "Can't gift a non positive amount"));
		}
		var players = await context.Players.ToListAsync();
		var (playerId, verifyMsg) = connectionManager.GetPlayerIdBySocket(socket, players);
		if (playerId == 0)
		{
			return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, verifyMsg));
		}
		if (playerId == recipientId)
		{
			logger.LogWarning("Player {Player} tried to send a gift to themselves", playerId);
			return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, "Can't gift to yourself"));
		}
		if (players.SingleOrDefault(p => p.Id == recipientId) == null)
		{
			logger.LogWarning("Recipient not found");
			return new SendGiftResponse(new SendGiftResponsePayload(null, 0, 0, null, Error, "Recipient not found"));
		}
		var firstId = Math.Min(playerId, recipientId);
		var secondId = Math.Max(playerId, recipientId);
		var objLock = connectionManager.GetLockObject(firstId);
		var objLock2 = connectionManager.GetLockObject(secondId);
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
}