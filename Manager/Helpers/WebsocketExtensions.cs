using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Helpers
{
	public static class WebsocketExtensions
	{
		private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault};
		public static async Task SendAsync(this System.Net.WebSockets.WebSocket socket, string message)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
		}

		public static async Task SendAsync(this System.Net.WebSockets.WebSocket socket, object message)
		{
			var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _options));
			await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
		}
	}
}
