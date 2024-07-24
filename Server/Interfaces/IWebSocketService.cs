using System.Net.WebSockets;

namespace Server.Interfaces;

public interface IWebSocketService
{
	Task ListenOnSocket (WebSocket socket);
}