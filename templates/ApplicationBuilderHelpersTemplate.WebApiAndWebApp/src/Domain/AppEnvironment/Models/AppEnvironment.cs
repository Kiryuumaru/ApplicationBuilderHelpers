namespace Domain.AppEnvironment.Models;

/// <summary>
/// Represents a complete application environment configuration for the Jack Of All Trades platform.
/// </summary>
/// <remarks>
/// <para>
/// This model encapsulates the identifiers required for any tier (Development, Staging, Preproduction, Production)
/// so the trading stack can connect to the correct infrastructure targets.
/// </para>
/// <para>
/// It exists to keep environment naming consistent across the solution, deployment tooling, and generated build metadata.
/// </para>
/// </remarks>
public class AppEnvironment
{
    /// <summary>
    /// Gets the full name of the deployment environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The environment name provides the formal, human-readable identifier for the
    /// deployment environment. This is the primary identifier used in user interfaces,
    /// logging, and administrative operations.
    /// </para>
    /// <para>
    /// Standard environment names: "Development", "Staging", "Preproduction", "Production"
    /// </para>
    /// <para>
    /// Used for:
    /// - User interface display and selection
    /// - Configuration validation and routing
    /// - Logging and audit trail identification
    /// - Administrative reporting and monitoring
    /// </para>
    /// </remarks>
    public required string Environment { get; init; }

    /// <summary>
    /// Gets the abbreviated short name for the deployment environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The environment short name provides a concise identifier suitable for use in
    /// URLs, file names, configuration keys, and other contexts where brevity is important.
    /// </para>
    /// <para>
    /// Standard short names: "dev", "stg", "pre", "prod"
    /// </para>
    /// <para>
    /// Used for:
    /// - URL path segments and subdomain construction
    /// - Configuration file naming and organization
    /// - Database and storage namespace prefixes
    /// - Container and service naming conventions
    /// </para>
    /// </remarks>
    public required string EnvironmentShort { get; init; }

    /// <summary>
    /// Gets the environment tag used for deployment identification and version management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The environment tag serves as a deployment-specific identifier used in build systems,
    /// CI/CD pipelines, and version management. It typically corresponds to release channels
    /// or deployment stages in the software delivery pipeline.
    /// </para>
    /// <para>
    /// Standard tags: "alpha", "beta", "rc", "prod"
    /// </para>
    /// <para>
    /// Used for:
    /// - Build system target identification
    /// - CI/CD pipeline routing and deployment
    /// - Version and release management
    /// - Container image tagging and registry organization
    /// </para>
    /// </remarks>
    public required string Tag { get; init; }
}
