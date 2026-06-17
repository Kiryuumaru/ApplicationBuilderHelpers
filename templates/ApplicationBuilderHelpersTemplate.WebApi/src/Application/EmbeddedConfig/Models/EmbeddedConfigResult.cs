using System.Text.Json.Nodes;

namespace Application.EmbeddedConfig.Models;

/// <summary>
/// Container for environment-specific embedded configuration data.
/// </summary>
public sealed class EmbeddedConfigResult
{
    public required JsonObject EnvironmentConfig { get; init; }
}
