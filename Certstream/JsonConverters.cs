using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Certstream
{
    public class UnixDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long longTime))
                    return DateTimeOffset.FromUnixTimeSeconds(longTime);

                if (reader.TryGetDouble(out double doubleTime))
                    return DateTimeOffset.FromUnixTimeSeconds((long)doubleTime);
            }

            throw new JsonException("Expected a number representing Unix time.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            long unixTime = (long)value.ToUnixTimeSeconds();
            writer.WriteNumberValue(unixTime);
        }
    }
}
