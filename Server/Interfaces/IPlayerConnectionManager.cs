using System.Net.WebSockets;
using Server.DB;

namespace Server.Interfaces;

public interface IPlayerConnectionManager
{
	bool TryAddPlayer(int playerId, WebSocket socket);
	bool TryGetPlayerSocket(int playerId, out WebSocket? socket);
	(int, string?) GetPlayerIdBySocket(WebSocket socket, IEnumerable<Player> players);
	SemaphoreSlim GetLockObject(int playerId);
}