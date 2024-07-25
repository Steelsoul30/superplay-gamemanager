using Server.Interfaces;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Server.Wrappers;

public class WebSocketWrapper(WebSocket webSocket) : IWebSocketWrapper
{
	private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

	public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
	{
		return await webSocket.ReceiveAsync(buffer, cancellationToken);
	}

	private async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		await webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
	}

	public async Task SendAsync(object message)
	{
		var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _options));
		await SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
	}

	public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		return webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
	}
}