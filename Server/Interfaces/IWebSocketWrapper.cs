using System.Net.WebSockets;

namespace Server.Interfaces;

public interface IWebSocketWrapper
{
	Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
	Task SendAsync(object message);
	Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
}