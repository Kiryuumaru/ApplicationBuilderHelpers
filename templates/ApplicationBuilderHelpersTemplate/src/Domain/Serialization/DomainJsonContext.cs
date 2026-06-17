using System.Text.Json.Serialization;
using Domain.WeatherForecast.Entities;
using Domain.WeatherForecast.Events;

namespace Domain.Serialization;

/// <summary>
/// Source-generated JSON context for Domain types.
/// Add [JsonSerializable(typeof(T))] for each type that needs serialization.
/// Supports Native AOT compilation.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(WeatherForecastEntity))]
[JsonSerializable(typeof(WeatherForecastCreatedEvent))]
public partial class DomainJsonContext : JsonSerializerContext
{
}
