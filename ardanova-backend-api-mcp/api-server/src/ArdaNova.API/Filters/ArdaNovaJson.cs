using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArdaNova.API.Filters;

/// <summary>The API's one JSON contract: camelCase, string enums, UTC timestamps. See Filters/AGENTS.md §UTC timestamps.</summary>
public static class ArdaNovaJson
{
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new UtcDateTimeJsonConverter());
        return options;
    }
}

/// <summary>Reads any ISO-8601 timestamp as UTC and always writes a 'Z'-suffixed UTC value.</summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("A timestamp must be an ISO-8601 string.");
        var text = reader.GetString();
        if (!DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            throw new JsonException("A timestamp must be an ISO-8601 string.");
        return parsed.UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Npgsql's legacy timestamp mode reads 'timestamp without time zone' as Unspecified; every stored value is UTC.
        var utc = value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value,
        };
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
    }
}
