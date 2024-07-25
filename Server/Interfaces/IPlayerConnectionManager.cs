using System.Net.WebSockets;
using Server.DB;

namespace Server.Interfaces;

public interface IPlayerConnectionManager
{
	bool TryAddPlayer(int playerId, IWebSocketWrapper socket);
	bool TryGetPlayerSocket(int playerId, out IWebSocketWrapper? socket);
	(int, string?) GetPlayerIdBySocket(IWebSocketWrapper socket, IEnumerable<Player> players);
	SemaphoreSlim GetLockObject(int playerId);
}