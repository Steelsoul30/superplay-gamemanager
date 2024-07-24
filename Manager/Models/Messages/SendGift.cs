using System.Text.Json.Serialization;

namespace Shared.Models.Messages
{
	public record SendGiftRequest([property: JsonPropertyName("payload")]SendGiftRequestPayload Payload) : MessageBase("sendGift");
	public record SendGiftRequestPayload(string ResourceType, int ResourceValue, int RecipientId);
	public record SendGiftResponse([property: JsonPropertyName("payload")]SendGiftResponsePayload Payload) : MessageBase("sendGift");
	public record SendGiftResponsePayload(string? ResourceType, int ResourceValue, int Balance, string? Sender, string Status, string? Error);
}
