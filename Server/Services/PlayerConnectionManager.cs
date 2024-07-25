using System.Collections.Concurrent;
using System.Net.WebSockets;
using Server.DB;
using Server.Interfaces;

namespace Server.Services;

public class PlayerConnectionManager: IPlayerConnectionManager
{
	private static readonly ConcurrentDictionary<int, IWebSocketWrapper> _playersConnectedDict = new();
	private static readonly ConcurrentDictionary<int, SemaphoreSlim> _lockObjects = new();

	public bool TryAddPlayer(int playerId, IWebSocketWrapper socket)
	{
		return _playersConnectedDict.TryAdd(playerId, socket);
	}

	public bool TryGetPlayerSocket(int playerId, out IWebSocketWrapper? socket)
	{
		return _playersConnectedDict.TryGetValue(playerId, out socket);
	}

	public (int, string?) GetPlayerIdBySocket(IWebSocketWrapper socket, IEnumerable<Player> players)
	{
		var player = _playersConnectedDict.FirstOrDefault(p => p.Value == socket).Key;
		if (player == 0)
		{
			return (0, "Player not logged in");
		}

		var playerEntity = players.SingleOrDefault(p => p.Id == player);
		return playerEntity != null ? (player, null) : (0, "Player not found");
	}

	public SemaphoreSlim GetLockObject(int playerId)
	{
		return _lockObjects.GetOrAdd(playerId, new SemaphoreSlim(1, 1));
	}
}