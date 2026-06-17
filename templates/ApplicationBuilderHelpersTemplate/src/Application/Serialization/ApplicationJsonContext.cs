using System.Text.Json.Serialization;
using Application.WeatherForecast.Models;

namespace Application.Serialization;

/// <summary>
/// Source-generated JSON context for Application types.
/// Add [JsonSerializable(typeof(T))] for each type that needs serialization.
/// Supports Native AOT compilation.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(WeatherForecastResult))]
public partial class ApplicationJsonContext : JsonSerializerContext
{
}
