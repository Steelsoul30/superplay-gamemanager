using System.Net.WebSockets;

namespace Manager
{
	public interface IGameService
	{
		public Task AddSocket(WebSocket socket);
	}
}
