using Serilog;
using System.Net.WebSockets;
using System.Text;
using Shared.Helpers;
using System.Text.Json;
using System.Runtime.Serialization;
using Shared.Models.Messages;
using static Shared.Constants.Constants;

namespace Client
{
	internal static class Listener
	{
		public static ClientState ClientState { get; set; } = new();

        private static JsonSerializerOptions Options { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static async Task Listen(WebSocket ws, CancellationToken ct)
		{
			var buffer = new byte[1024 * 4];
			while (true)
			{
				try
				{
					var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
					if (ct.IsCancellationRequested)
					{
						break;
					}
					if (result.MessageType == WebSocketMessageType.Close)
					{
						ClientState.IsExpectingResponse = false;
						break;
					}
					var data = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(data);
				}
				catch (WebSocketException ex)
				{
					Log.Error("Socket closed unexpectedly - {ex}", ex.Message);
					ClientState.IsExpectingResponse = false;
					break;
				}
				catch (OperationCanceledException)
				{
					Log.Information("Cancellation requested");
					break;
				}
				catch (Exception ex)
				{
					Log.Error("Error occurred while listening - {ex}", ex.Message);
					break;
				}	
			}
		}

		private static void ProcessMessage(string data)
		{
			Log.Debug("Received message: {data}", data);
			var command = JsonHelper.GetElementByKey(Command, data);
			switch (command)
			{
				case LoginCommand:
					var response = JsonSerializer.Deserialize<LoginResponse>(data, Options) ?? throw new SerializationException($"Invalid response for {command} command");
					Log.Information("Login response received: {response}", response);
					var payload = response.Payload;
					switch (payload.Status)
					{
						case Error:
							Log.Error("Login request unsuccessful. Received error {error}", payload.Error);
							break;
						case Success:
							Log.Information("Login request successful. Received player {PlayerId}: {PlayerName}", payload.PlayerId, payload.PlayerName);
							ClientState.PlayerName = payload.PlayerName!;
							ClientState.IsLoggedIn = true;
							break;
					}
					ClientState.IsExpectingResponse = false;
					break;
				case UpdateResourcesCommand:
					var updateResponse = JsonSerializer.Deserialize<UpdateResourcesResponse>(data, Options) ?? throw new SerializationException($"Invalid response for {command} command");
                    Log.Information("Update resources response received: {updateResponse}", updateResponse);
                    var updatePayload = updateResponse.Payload;
                    switch (updatePayload.Status)
					{
						case Error:
							Log.Error("Update resources request unsuccessful. Received error {error}", updatePayload.Error);
							break;
						case Success:
							Log.Information("Update resources request successful. Received new resource value {ResourceValue} for {Type}",
								updatePayload.Balance,
								updatePayload.ResourceType);
							break;
					}
                    break;
				case SendGiftCommand:
					var giftResponse = JsonSerializer.Deserialize<SendGiftResponse>(data, Options) ?? throw new SerializationException($"Invalid response for {command} command");
					Log.Information("Send gift response received: {giftResponse}", giftResponse);
					var giftPayload = giftResponse.Payload;
					switch (giftPayload.Status)
					{
						case Error:
							Log.Error("Send gift request unsuccessful. Received error {error}", giftPayload.Error);
							break;
						case Success:
							Log.Information("Send gift request successful. Received {ResourceValue} of type {Type} from {Sender}",
								giftPayload.ResourceValue,
								giftPayload.ResourceType,
								giftPayload.Sender);
							break;
					}
					break;
				case Empty:
					Log.Warning("message did not contain a command");
					break;
				default:
					Log.Warning("Unknown command");
					break;
			}
		}
	}
}
