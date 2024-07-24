using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text.Json;
using Server.Interfaces;
using Shared.Models.Messages;
using Shared.Helpers;
using static Shared.Constants.Constants;

namespace Server.Commands;

public class LoginCommandHandler(IPlayerService playerService) : ICommandHandler
{
	private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true};


	public async Task HandleAsync(string data, WebSocket socket)
	{
		var loginRequest = JsonSerializer.Deserialize<LoginRequest>(data, _jsonOptions) ?? throw new SerializationException($"Invalid request for {LoginCommand} command");
		var loginResponse = await playerService.Login(loginRequest.Payload.DeviceId, socket);
		await socket.SendAsync(loginResponse);
	}

	public bool CanHandle(string command)
	{
		return command == LoginCommand;
	}
}