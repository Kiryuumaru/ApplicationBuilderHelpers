namespace Domain.AppEnvironment.Models;

/// <summary>
/// Represents a complete application environment configuration.
/// </summary>
/// <remarks>
/// This model encapsulates the identifiers required for any tier (Development, Production)
/// to keep environment naming consistent across the solution, deployment tooling, and generated build metadata.
/// </remarks>
public class AppEnvironment
{
    /// <summary>
    /// Gets the full name of the deployment environment.
    /// </summary>
    /// <remarks>
    /// Standard environment names: "Development", "Production"
    /// </remarks>
    public required string Full { get; init; }

    /// <summary>
    /// Gets the abbreviated short name for the deployment environment.
    /// </summary>
    /// <remarks>
    /// Standard short names: "pre", "prod"
    /// </remarks>
    public required string Short { get; init; }

    /// <summary>
    /// Gets the environment tag used for deployment identification and version management.
    /// </summary>
    /// <remarks>
    /// Standard tags: "prerelease", "master"
    /// </remarks>
    public required string Tag { get; init; }
}
