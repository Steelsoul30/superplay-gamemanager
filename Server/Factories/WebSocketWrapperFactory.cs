using Server.Interfaces;
using System.Net.WebSockets;
using Server.Wrappers;

namespace Server.Factories;

public interface IWebSocketWrapperFactory
{
    IWebSocketWrapper Create(WebSocket webSocket);
}

public class WebSocketWrapperFactory : IWebSocketWrapperFactory
{
    public IWebSocketWrapper Create(WebSocket webSocket)
    {
        return new WebSocketWrapper(webSocket);
    }
}