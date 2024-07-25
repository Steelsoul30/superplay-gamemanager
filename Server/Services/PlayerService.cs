using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Interfaces;
using Shared.Models.Messages;
using static Shared.Constants.Constants;

namespace Server.Services;

public class PlayerService(
	ILogger<PlayerService> logger,
	IPlayerConnectionManager connectionManager,
	GameContext context) : IPlayerService
{
	public async Task<LoginResponse> Login(string deviceId, IWebSocketWrapper socket)
	{
		logger.LogInformation("Login attempt");
		var player = await context.Players.Where(p => p.DeviceID == deviceId).SingleOrDefaultAsync();
		if (player == null)
		{
			logger.LogWarning($"Player not found for device {deviceId}");
			return new LoginResponse(new LoginResponsePayload(0, null, Error, "Player not registered"));
		}

		if (!connectionManager.TryAddPlayer(player.Id, socket))
		{
			logger.LogWarning($"Player {player.Id}: {player.PlayerName} is already logged in");
			return new LoginResponse(new LoginResponsePayload(player.Id, player.PlayerName, Error, "Player already logged in"));
		}

		logger.LogInformation($"Player {player.Id}: {player.PlayerName} logged in");
		return new LoginResponse(new LoginResponsePayload(player.Id, player.PlayerName, Success, null));
	}
}
