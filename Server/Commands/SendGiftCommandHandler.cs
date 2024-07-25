using System.Runtime.Serialization;
using System.Text.Json;
using Server.Interfaces;
using Shared.Models.Messages;
using static Shared.Constants.Constants;

namespace Server.Commands;

public class SendGiftCommandHandler(IResourceService resourceService, IPlayerConnectionManager connectionManager) : ICommandHandler
{
	private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };


	public async Task HandleAsync(string data, IWebSocketWrapper socket)
	{
		var sendGiftRequest = JsonSerializer.Deserialize<SendGiftRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {SendGiftCommand} command");
		var sendGiftResponse = await resourceService.SendGift(sendGiftRequest.Payload.ResourceType, sendGiftRequest.Payload.ResourceValue, sendGiftRequest.Payload.RecipientId, socket);
		if (sendGiftResponse.Payload.Status == Error)
		{
			await socket.SendAsync(sendGiftResponse); // send to the sender
		}
		else if (connectionManager.TryGetPlayerSocket(sendGiftRequest.Payload.RecipientId, out var recipientSocket))
		{
			await recipientSocket!.SendAsync(sendGiftResponse); // send to the recipient
		}
	}

	public bool CanHandle(string command)
	{
		return command == SendGiftCommand;
	}
}