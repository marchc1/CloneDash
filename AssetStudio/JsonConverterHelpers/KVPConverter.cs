using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetStudio
{
    public static partial class JsonConverterHelper
    {
        public class KVPConverter : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                if (!typeToConvert.IsGenericType)
                    return false;

                var generic = typeToConvert.GetGenericTypeDefinition();
                return generic == typeof(KeyValuePair<,>);
            }

            public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
            {
                var kvpArgs = type.GetGenericArguments();
                return (JsonConverter)Activator.CreateInstance(typeof(KVPConverter<,>).MakeGenericType(kvpArgs));
            }
        }

        private class KVPConverter<TKey, TValue> : JsonConverter<KeyValuePair<TKey, TValue>>
        {
            public override KeyValuePair<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                //startKvpObject
                reader.Read(); //propName
                reader.Read(); //keyType
                var key = reader.TokenType == JsonTokenType.StartObject
                    ? JsonSerializer.Deserialize<Dictionary<string, TKey>>(ref reader).Values.First()
                    : JsonSerializer.Deserialize<TKey>(ref reader);
                reader.Read(); //propName
                reader.Read(); //startObject
                var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
                reader.Read(); //endKvpObject

                return new KeyValuePair<TKey, TValue>(key, value);
            }

            public override void Write(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
