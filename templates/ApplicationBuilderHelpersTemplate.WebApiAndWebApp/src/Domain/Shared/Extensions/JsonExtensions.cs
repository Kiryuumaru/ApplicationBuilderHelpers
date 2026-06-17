using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Domain.Shared.Extensions;

public static class JsonExtensions
{
    public static JsonNode GetJsonNodeOrThrow(this JsonNode node, params string[] propertyNames)
    {
        JsonNode currentNode = node;
        string currentKey = "@root";

        if (propertyNames != null)
        {
            foreach (var propertyName in propertyNames)
            {
                if (currentNode.GetValueKind() != JsonValueKind.Object)
                    throw new InvalidOperationException($"Cannot access property '{propertyName}' because '{currentKey}' is not an object but a {currentNode.GetValueKind()}.");

                if (currentNode is JsonObject jsonObj && jsonObj.TryGetPropertyValue(propertyName, out var nextNode) && nextNode != null)
                {
                    currentNode = nextNode;
                }
                else
                {
                    throw new KeyNotFoundException($"Property '{propertyName}' was not found in the JSON document '{currentKey}'.");
                }
                currentKey = currentKey + "." + propertyName;
            }
        }

        return currentNode;
    }

    public static void ThrowIfNone(this JsonNode node, params string[] propertyNames)
    {
        GetJsonNodeOrThrow(node, propertyNames);
    }

    public static void ThrowIfNone(this JsonElement element, params string[] propertyNames)
    {
        GetJsonElementOrThrow(element, propertyNames);
    }

    public static bool ContainsProperty(this JsonNode node, params string[] propertyNames)
    {
        try
        {
            GetJsonNodeOrThrow(node, propertyNames);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool ContainsProperty(this JsonElement element, params string[] propertyNames)
    {
        try
        {
            GetJsonElementOrThrow(element, propertyNames);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static JsonElement GetJsonElementOrThrow(this JsonElement element, params string[] propertyNames)
    {
        JsonElement currentElement = element;
        string currentKey = "@root";

        if (propertyNames != null)
        {
            foreach (var propertyName in propertyNames)
            {
                if (currentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Cannot access property '{propertyName}' because '{currentKey}' is not an object but a {currentElement.ValueKind}.");

                if (currentElement.TryGetProperty(propertyName, out var nextElement))
                {
                    currentElement = nextElement;
                }
                else
                {
                    throw new KeyNotFoundException($"Property '{propertyName}' was not found in the JSON document '{currentKey}'.");
                }
                currentKey = currentKey + "." + propertyName;
            }
        }

        return currentElement;
    }

    public static TValue GetValueOrThrow<TValue>(this JsonNode node, params string[] propertyNames)
    {
        JsonNode currentNode = GetJsonNodeOrThrow(node, propertyNames);

        try
        {
            var actualType = typeof(TValue).GetNullableUnderlyingType();

            if (actualType == typeof(JsonArray) &&
                currentNode as JsonArray is JsonArray jsonArray)
            {
                return (TValue)(object)jsonArray;
            }

            if (actualType == typeof(JsonObject) &&
                currentNode as JsonObject is JsonObject jsonObject)
            {
                return (TValue)(object)jsonObject;
            }

            return currentNode.GetValue<TValue>();
        }
        catch (Exception ex)
        {
            string path = "@root" + (propertyNames?.Length > 0 ? "." + string.Join(".", propertyNames) : "");
            throw new InvalidOperationException($"Could not convert JSON value at '{path}' to type '{typeof(TValue).Name}'.", ex);
        }
    }

    public static TValue GetValueOrDefault<TValue>(this JsonNode node, TValue defaultValue, params string[] propertyNames)
    {
        try
        {
            JsonNode currentNode = GetJsonNodeOrThrow(node, propertyNames);

            var actualType = typeof(TValue).GetNullableUnderlyingType();

            if (actualType == typeof(JsonArray) &&
                currentNode as JsonArray is JsonArray jsonArray)
            {
                return (TValue)(object)jsonArray;
            }

            if (actualType == typeof(JsonObject) &&
                currentNode as JsonObject is JsonObject jsonObject)
            {
                return (TValue)(object)jsonObject;
            }

            return currentNode.GetValue<TValue>();
        }
        catch
        {
            return defaultValue;
        }
    }

    public static TValue GetValueOrThrow<TValue>(this JsonElement element, params string[] propertyNames)
    {
        JsonElement currentElement = GetJsonElementOrThrow(element, propertyNames);

        try
        {
            var actualType = typeof(TValue).GetNullableUnderlyingType();

            return actualType switch
            {
                _ when actualType == typeof(string) => (TValue)(object)currentElement.GetString()!,
                _ when actualType == typeof(int) => (TValue)(object)currentElement.GetInt32(),
                _ when actualType == typeof(long) => (TValue)(object)currentElement.GetInt64(),
                _ when actualType == typeof(bool) => (TValue)(object)currentElement.GetBoolean(),
                _ when actualType == typeof(double) => (TValue)(object)currentElement.GetDouble(),
                _ when actualType == typeof(decimal) => (TValue)(object)currentElement.GetDecimal(),
                _ when actualType == typeof(float) => (TValue)(object)currentElement.GetSingle(),
                _ when actualType == typeof(byte) => (TValue)(object)currentElement.GetByte(),
                _ when actualType == typeof(short) => (TValue)(object)currentElement.GetInt16(),
                _ when actualType == typeof(Guid) => (TValue)(object)Guid.Parse(currentElement.GetString()!),
                _ when actualType == typeof(DateTime) => (TValue)(object)currentElement.GetDateTime(),
                _ when actualType == typeof(DateTimeOffset) => (TValue)(object)DateTimeOffset.Parse(currentElement.GetString()!),
                _ when actualType == typeof(TimeSpan) => (TValue)(object)TimeSpan.Parse(currentElement.GetString()!),
                _ when actualType.IsEnum => (TValue)Enum.Parse(actualType, currentElement.GetString()!),
                _ => throw new InvalidOperationException($"Unsupported type '{actualType.Name}' for JSON conversion.")
            };
        }
        catch (Exception ex)
        {
            string path = "@root" + (propertyNames?.Length > 0 ? "." + string.Join(".", propertyNames) : "");
            throw new InvalidOperationException($"Could not convert JSON value at '{path}' to type '{typeof(TValue).Name}'.", ex);
        }
    }

    public static bool TryGetValue<TValue>(this JsonNode node, [NotNullWhen(true)] out TValue? value, params string[] propertyNames)
    {
        try
        {
            value = GetValueOrThrow<TValue>(node, propertyNames)!;
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public static bool TryGetValue<TValue>(this JsonElement element, [NotNullWhen(true)] out TValue? value, params string[] propertyNames)
    {
        try
        {
            value = GetValueOrThrow<TValue>(element, propertyNames)!;
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public static TValue[] GetArrayValueOrThrow<TValue>(this JsonNode node, params string[] propertyNames)
    {
        var jsonArray = node.GetValueOrThrow<JsonArray>(propertyNames);

        TValue[] values = new TValue[jsonArray.Count];
        for (int i = 0; i < jsonArray.Count; i++)
        {
            if (jsonArray[i] is JsonNode jsonNode)
            {
                values[i] = GetValueOrThrow<TValue>(jsonNode);
            }
            else
            {
                throw new InvalidOperationException($"Element at index {i} in the JSON array is not a valid JSON node.");
            }
        }

        return values;
    }

    public static bool TryGetArrayValue<TValue>(this JsonNode node, [NotNullWhen(true)] out TValue[]? value, params string[] propertyNames)
    {
        try
        {
            value = GetArrayValueOrThrow<TValue>(node, propertyNames)!;
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public static async Task<Stream> ToStream(this JsonElement element, JsonWriterOptions? jsonWriterOptions = default, CancellationToken cancellationToken = default)
    {
        jsonWriterOptions ??= new JsonWriterOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        Stream stream = new MemoryStream();
        await using Utf8JsonWriter utf8JsonWriter = new(stream, jsonWriterOptions.Value);
        element.WriteTo(utf8JsonWriter);
        await utf8JsonWriter.FlushAsync(cancellationToken);
        stream.Position = 0;
        return stream;
    }

    public static async Task<Stream> ToStream(this JsonNode node, JsonWriterOptions? jsonWriterOptions = default, CancellationToken cancellationToken = default)
    {
        jsonWriterOptions ??= new JsonWriterOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        Stream stream = new MemoryStream();
        await using Utf8JsonWriter utf8JsonWriter = new(stream, jsonWriterOptions.Value);
        node.WriteTo(utf8JsonWriter);
        await utf8JsonWriter.FlushAsync(cancellationToken);
        stream.Position = 0;
        return stream;
    }

    public static JsonValue? Create<TValue>(TValue value)
    {
        try
        {
            var actualType = typeof(TValue).GetNullableUnderlyingType();

            return actualType switch
            {
                _ when actualType == typeof(string) => JsonValue.Create((string)(object)value!),
                _ when actualType == typeof(int) => JsonValue.Create((int)(object)value!),
                _ when actualType == typeof(long) => JsonValue.Create((long)(object)value!),
                _ when actualType == typeof(bool) => JsonValue.Create((bool)(object)value!),
                _ when actualType == typeof(double) => JsonValue.Create((double)(object)value!),
                _ when actualType == typeof(decimal) => JsonValue.Create((decimal)(object)value!),
                _ when actualType == typeof(float) => JsonValue.Create((float)(object)value!),
                _ when actualType == typeof(byte) => JsonValue.Create((byte)(object)value!),
                _ when actualType == typeof(short) => JsonValue.Create((short)(object)value!),
                _ when actualType == typeof(Guid) => JsonValue.Create((Guid)(object)value!),
                _ when actualType == typeof(DateTime) => JsonValue.Create((DateTime)(object)value!),
                _ when actualType == typeof(DateTimeOffset) => JsonValue.Create((DateTimeOffset)(object)value!),
                _ when actualType == typeof(TimeSpan) => JsonValue.Create(((TimeSpan)(object)value!).ToString()),
                _ when actualType.IsEnum => JsonValue.Create(value!.ToString()),
                _ => throw new InvalidOperationException($"Unsupported type '{actualType.Name}' for JSON conversion.")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not convert value to JSON for type '{typeof(TValue).Name}'.", ex);
        }
    }

    public static JsonNode Merge(this JsonNode target, JsonNode source)
    {
        if (source == null)
            return target;

        if (target == null)
            return source.DeepClone();

        // Handle JsonObject merging
        if (target is JsonObject targetObject && source is JsonObject sourceObject)
        {
            foreach (var sourceProperty in sourceObject)
            {
                string propertyName = sourceProperty.Key;
                JsonNode? sourceValue = sourceProperty.Value;

                if (sourceValue == null)
                {
                    targetObject[propertyName] = null;
                    continue;
                }

                // If target has the property, merge recursively
                if (targetObject.TryGetPropertyValue(propertyName, out JsonNode? targetValue) && targetValue != null)
                {
                    // Both are objects - merge recursively
                    if (targetValue is JsonObject && sourceValue is JsonObject)
                    {
                        targetObject[propertyName] = Merge(targetValue, sourceValue);
                    }
                    // Both are arrays - merge/replace (you can customize this behavior)
                    else if (targetValue is JsonArray && sourceValue is JsonArray)
                    {
                        // Replace the array (you can change this to append or merge arrays differently)
                        targetObject[propertyName] = sourceValue.DeepClone();
                    }
                    // Otherwise, replace the value
                    else
                    {
                        targetObject[propertyName] = sourceValue.DeepClone();
                    }
                }
                else
                {
                    // Property doesn't exist in target, add it
                    targetObject[propertyName] = sourceValue.DeepClone();
                }
            }

            return targetObject;
        }

        // Handle JsonArray merging (replace target with source)
        if (target is JsonArray && source is JsonArray)
        {
            return source.DeepClone();
        }

        // For value types or mismatched types, replace target with source
        return source.DeepClone();
    }
}
