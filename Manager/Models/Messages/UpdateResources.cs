using System.Text.Json.Serialization;

namespace Shared.Models.Messages
{
	public record UpdateResourcesRequest([property: JsonPropertyName("payload")]UpdateResourcesRequestPayload Payload) : MessageBase("updateResources");
	public record UpdateResourcesRequestPayload(string ResourceType, int ResourceValue);
	public record UpdateResourcesResponse([property: JsonPropertyName("payload")]UpdateResourcesResponsePayload Payload) : MessageBase("updateResources");
	public record UpdateResourcesResponsePayload(string ResourceType, int Balance, string Status, string Error);
}
