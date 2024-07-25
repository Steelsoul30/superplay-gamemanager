using System.Text.Json;

namespace Shared.Helpers
{
	public static class JsonHelper
	{
		private static string GetElementByKey(string key, JsonElement jsonElement)
		{
			return jsonElement.TryGetProperty(key, out var element) 
			? element.GetString() ?? string.Empty
			: string.Empty;
		}

		public static string GetElementByKey(string key, string jsonString)
		{
			var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
			return GetElementByKey(key, jsonElement);
		}
	}
}
