using System;
using System.Text.Json.Serialization;

namespace Shared.Models.Messages
{
	public record LoginRequest([property: JsonPropertyName("payload")]LoginRequestPayload Payload) : MessageBase("login");
	public record LoginRequestPayload(string DeviceID);
	public record LoginResponse([property: JsonPropertyName("payload")]LoginResponsePayload Payload) : MessageBase("login");
	public record LoginResponsePayload(string PlayerName, string Status, string Error);
}
