using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;

namespace Application.AppEnvironment.Extensions;

/// <summary>
/// Configuration extensions for application environment tag override.
/// Supports @ref: reference chains via GetRefValueOrDefault.
/// </summary>
public static class AppEnvironmentConfigurationExtensions
{
    private const string AppTagOverrideKey = "RUNTIME_APP_TAG_OVERRIDE";

    extension(IConfiguration configuration)
    {
        public string? AppTagOverride
        {
            get => configuration.GetRefValueOrDefault(AppTagOverrideKey);
            set => configuration[AppTagOverrideKey] = value;
        }
    }
}
