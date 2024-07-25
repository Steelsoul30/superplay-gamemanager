namespace Server.Interfaces;

public interface ICommandHandler
{
	Task HandleAsync(string data, IWebSocketWrapper socket);
	bool CanHandle(string command);
}