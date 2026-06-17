namespace Domain.AppEnvironment.Constants;

/// <summary>
/// Application environment definitions. This is the single source of truth for all environments.
/// </summary>
/// <remarks>
/// The LAST environment in <see cref="AllValues"/> is treated as the main/production branch.
/// </remarks>
public static class AppEnvironments
{
    public static Models.AppEnvironment Development { get; } = new()
    {
        Tag = "prerelease",
        Environment = "Development",
        EnvironmentShort = "pre"
    };

    public static Models.AppEnvironment Production { get; } = new()
    {
        Tag = "master",
        Environment = "Production",
        EnvironmentShort = "prod"
    };

    /// <summary>
    /// All defined environments. The last environment is treated as the main/production branch.
    /// </summary>
    public static Models.AppEnvironment[] AllValues { get; } = [Development, Production];

    public static bool IsValidAppTag(string appTag)
    {
        return AllValues.Any(x => x.Tag.Equals(appTag, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool IsValidEnvironment(string environment)
    {
        return AllValues.Any(x => x.Environment.Equals(environment, StringComparison.InvariantCultureIgnoreCase));
    }

    public static Models.AppEnvironment GetByTag(string tag)
    {
        return AllValues.FirstOrDefault(x => x.Tag.Equals(tag, StringComparison.InvariantCultureIgnoreCase))
            ?? throw new ArgumentException($"Invalid environment tag: {tag}");
    }

    public static Models.AppEnvironment GetByEnvironment(string environment)
    {
        return AllValues.FirstOrDefault(x => x.Environment.Equals(environment, StringComparison.InvariantCultureIgnoreCase))
            ?? throw new ArgumentException($"Invalid environment: {environment}");
    }
}
