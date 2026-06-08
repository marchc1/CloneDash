using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetStudio
{
    public static partial class JsonConverterHelper
    {
        public static SerializedFile AssetsFile { get; set; }

        public class PPtrConverter : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                if (!typeToConvert.IsGenericType)
                    return false;

                var generic = typeToConvert.GetGenericTypeDefinition();
                return generic == typeof(PPtr<>);
            }

            public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
            {
                var elementType = type.GetGenericArguments()[0];
                var converter = (JsonConverter)Activator.CreateInstance(typeof(PPtrConverter<>).MakeGenericType(elementType));
                return converter;
            }
        }

        private class PPtrConverter<T> : JsonConverter<PPtr<T>> where T : Object
        {
            public override PPtr<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var pptrObj = JsonSerializer.Deserialize<PPtr<T>>(ref reader, new JsonSerializerOptions { IncludeFields = true });
                pptrObj.AssetsFile = AssetsFile;
                return pptrObj;
            }

            public override void Write(Utf8JsonWriter writer, PPtr<T> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
