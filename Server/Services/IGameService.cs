using System.Net.WebSockets;

namespace Server.Services
{
	public interface IGameService
	{
		public Task ListenOnSocket(WebSocket socket);
	}
}
