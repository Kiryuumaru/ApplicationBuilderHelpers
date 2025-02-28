using Microsoft.Extensions.Configuration;
using System;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Provides access to the application configuration during runtime.
/// </summary>
public static class ApplicationRuntime
{
    /// <summary>
    /// The application configuration.
    /// </summary>
    /// <remarks>
    /// This property is set during application startup to provide access to the application configuration throughout the runtime.
    /// </remarks>
    public static IConfiguration Configuration { get; internal set; } = null!;
}
