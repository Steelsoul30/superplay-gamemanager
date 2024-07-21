using System.Text;

namespace Shared.Helpers
{
	public static class WebsocketExtensions
	{
		public static async Task SendAsync(this System.Net.WebSockets.WebSocket socket, string message)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
		}
	}
}
