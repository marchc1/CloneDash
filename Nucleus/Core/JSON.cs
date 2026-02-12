using Newtonsoft.Json;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// carryover from slightsand
// todo; make other things go through this?

namespace Nucleus.Core;


public static class JSON
{
	static readonly JsonSerializer serializer;
	static JSON(){
		serializer = new();
		serializer.Converters.Add(new Vector2FJsonConverter());
	}

	public static string Serialize<T>(T obj) {
		using (TextWriter text = new StringWriter())
		using (JsonTextWriter writer = new JsonTextWriter(text)) {
			serializer.Serialize(writer, obj);
			return text.ToString() ?? throw new Exception("text.ToString returned null??");
		}
	}
	public static T? Deserialize<T>(string json) {
		using (TextReader text = new StringReader(json))
		using (JsonTextReader reader = new JsonTextReader(text)) {
			return serializer.Deserialize<T>(reader);
		}
	}
}
