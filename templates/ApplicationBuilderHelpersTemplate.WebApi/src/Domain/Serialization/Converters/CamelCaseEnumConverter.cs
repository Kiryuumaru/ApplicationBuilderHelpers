using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Serialization.Converters;

internal abstract class CamelCaseEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string when parsing enum '{typeof(TEnum).Name}'.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Enum '{typeof(TEnum).Name}' cannot be null or empty.");
        }

        if (Enum.TryParse(value, ignoreCase: true, out TEnum parsed))
        {
            return parsed;
        }

        throw new JsonException($"Value '{value}' is not valid for enum '{typeof(TEnum).Name}'.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        var name = value.ToString();
        if (string.IsNullOrEmpty(name))
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        var camelName = char.ToLowerInvariant(name[0]) + name[1..];
        writer.WriteStringValue(camelName);
    }
}
