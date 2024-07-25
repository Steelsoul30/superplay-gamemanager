using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text;
using Server.Factories;
using Server.Interfaces;
using Shared.Helpers;
using Shared.Models.Messages;
using static Shared.Constants.Constants;

namespace Server.Services;

public class WebSocketService(ILogger<WebSocketService> logger, IEnumerable<ICommandHandler> commandHandlers) : IWebSocketService
{
	public async Task ListenOnSocket(IWebSocketWrapper socket)
	{
		logger.LogInformation("New connection established");
		var buffer = new byte[1024 * 4];
		while (true)
		{
			try
			{
				var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					break;
				}
				var data = Encoding.UTF8.GetString(buffer, 0, result.Count);
				logger.LogDebug("Received message: {data}", data);
				var command = JsonHelper.GetElementByKey("command", data);
				var handler = commandHandlers.FirstOrDefault(h => h.CanHandle(command));
				if (handler == null)
				{
					logger.LogWarning("Unknown command");
					continue;
				}
				await handler.HandleAsync(data, socket);
			}
			catch (WebSocketException ex)
			{
				logger.LogError("Socket closed unexpectedly - {ex}", ex.Message);
				break;
			}
			catch (KeyNotFoundException)
			{
				logger.LogWarning("message did not contain a command");
			}
			catch (SerializationException ex)
			{
				logger.LogError("Bad request - {ex}", ex.Message);
			}
		}
		await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
		logger.LogInformation("Connection terminated");
	}
}