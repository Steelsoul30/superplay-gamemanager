using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text.Json;
using Server.Interfaces;
using Shared.Models.Messages;
using Shared.Helpers;
using static Shared.Constants.Constants;

namespace Server.Commands;

public class UpdateResourcesCommandHandler(IResourceService resourceService) : ICommandHandler
{
	private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };


	public async Task HandleAsync(string data, WebSocket socket)
	{
		var updateRequest = JsonSerializer.Deserialize<UpdateResourcesRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {UpdateResourcesCommand} command");
		var updateResponse = await resourceService.UpdateResources(updateRequest.Payload.ResourceType, updateRequest.Payload.ResourceValue, socket);
		await socket.SendAsync(updateResponse);
	}

	public bool CanHandle(string command)
	{
		return command == UpdateResourcesCommand;
	}
}