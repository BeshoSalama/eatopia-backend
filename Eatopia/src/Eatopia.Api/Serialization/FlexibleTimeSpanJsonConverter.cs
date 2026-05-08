using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eatopia.Api.Serialization;

/// <summary>
/// Accepts frontend time values as "HH:mm", "HH:mm:ss" or normal TimeSpan strings.
/// Serializes as "HH:mm:ss" so SQL Server Time columns bind consistently.
/// </summary>
public sealed class FlexibleTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    private static readonly string[] Formats =
    {
        @"hh\:mm",
        @"h\:mm",
        @"hh\:mm\:ss",
        @"h\:mm\:ss",
        @"c"
    };

    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return TimeSpan.Zero;

            value = value.Trim();

            foreach (var format in Formats)
            {
                if (TimeSpan.TryParseExact(value, format, null, out var parsed))
                    return parsed;
            }

            if (TimeSpan.TryParse(value, out var fallback))
                return fallback;
        }

        throw new JsonException("Time must be in HH:mm or HH:mm:ss format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
    }
}

public sealed class NullableFlexibleTimeSpanJsonConverter : JsonConverter<TimeSpan?>
{
    private readonly FlexibleTimeSpanJsonConverter _inner = new();

    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return _inner.Read(ref reader, typeof(TimeSpan), options);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            _inner.Write(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}
