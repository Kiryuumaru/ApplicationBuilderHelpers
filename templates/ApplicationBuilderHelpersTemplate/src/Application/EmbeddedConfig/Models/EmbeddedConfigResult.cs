using System.Text.Json.Nodes;

namespace Application.EmbeddedConfig.Models;

/// <summary>
/// Container for environment-specific and shared embedded configuration data.
/// </summary>
public class EmbeddedConfigResult
{
    public required JsonObject EnvironmentConfig { get; init; }

    public required JsonObject SharedConfig { get; init; }
}
