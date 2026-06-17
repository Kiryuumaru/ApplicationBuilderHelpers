using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Serialization.Converters;

internal sealed class CamelCaseStringEnumConverter : JsonStringEnumConverter
{
    public CamelCaseStringEnumConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
