namespace Server.Interfaces;

public interface IWebSocketService
{
	Task ListenOnSocket (IWebSocketWrapper socket);
}