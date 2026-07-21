using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stycue.Api.Converters
{
    public class PreserveStringDictionaryKeyJsonConverter : JsonConverter<Dictionary<string, string>>
    {
        public override Dictionary<string, string> Read(
          ref Utf8JsonReader reader,
          Type typeToConvert,
          JsonSerializerOptions options)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected JSON object.");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return result;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected JSON property name.");
                }

                var key = reader.GetString()
                    ?? throw new JsonException("Property name cannot be null.");

                reader.Read();
                result[key] = reader.GetString() ?? string.Empty;
            }

            throw new JsonException("Unexpected end of JSON.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Dictionary<string, string> value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var pair in value)
            {
                // 明確使用原始 key，不套用全域 DictionaryKeyPolicy。
                writer.WriteString(pair.Key, pair.Value);
            }

            writer.WriteEndObject();
        }
    }
}
