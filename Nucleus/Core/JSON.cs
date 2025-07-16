using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

// carryover from slightsand
// todo; make other things go through this?

namespace Nucleus.Core;

public class NucleusJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType) {
		return objectType == typeof(IDataBinder);
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
		return IDataBinder.Create(existingValue);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
		serializer.Serialize(writer, (value as IDataBinder)?.GetBackingObject());
	}
}
public static class JSON
{
	public static string Serialize<T>(T obj) {
		JsonSerializer serializer = new();
		serializer.Converters.Add(new NucleusJsonConverter());
		using (TextWriter text = new StringWriter())
		using (JsonTextWriter writer = new JsonTextWriter(text)) {
			serializer.Serialize(writer, obj);
			return text.ToString() ?? throw new Exception("text.ToString returned null??");
		}
	}
	public static T? Deserialize<T>(string json) {
		JsonSerializer serializer = new();
		serializer.Converters.Add(new NucleusJsonConverter());
		using (TextReader text = new StringReader(json))
		using (JsonTextReader reader = new JsonTextReader(text)) {
			return serializer.Deserialize<T>(reader);
		}
	}
}
