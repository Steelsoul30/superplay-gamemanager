using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Helpers
{
	public static class JsonHelper
	{
		public static string GetElementByKey(string key, JsonElement jsonElement)
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
