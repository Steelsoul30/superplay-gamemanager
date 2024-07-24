using System.Net.WebSockets;

namespace Server.Interfaces;

public interface ICommandHandler
{
	Task HandleAsync(string data, WebSocket socket);
	bool CanHandle(string command);
}