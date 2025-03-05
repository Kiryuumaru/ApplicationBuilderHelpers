using ApplicationBuilderHelpers.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Extension methods for IConfiguration to handle reference values.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Checks if the configuration contains a reference value for the specified variable name.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to check.</param>
    /// <returns>True if the reference value exists; otherwise, false.</returns>
    public static bool ContainsRefValue(this IConfiguration configuration, string varName)
    {
        try
        {
            return !string.IsNullOrEmpty(configuration.GetRefValue(varName));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the reference value for the specified variable name.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to get the reference value for.</param>
    /// <returns>The reference value.</returns>
    /// <exception cref="NoConfigValueException">Thrown when the reference value is not found.</exception>
    public static string GetRefValue(this IConfiguration configuration, string varName)
    {
        string? varValue = $"@ref:{varName}";
        while (true)
        {
            if (varValue.StartsWith("@ref:"))
            {
                varName = varValue[5..];
                varValue = configuration[varName];
                if (varValue == null || string.IsNullOrEmpty(varValue))
                {
                    throw new NoConfigValueException(varName);
                }
                continue;
            }
            break;
        }
        return varValue;
    }

    /// <summary>
    /// Gets the reference value for the specified variable name, or a default value if the reference value is not found.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to get the reference value for.</param>
    /// <param name="defaultValue">The default value to return if the reference value is not found.</param>
    /// <returns>The reference value or the default value.</returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetRefValueOrDefault(this IConfiguration configuration, string varName, string? defaultValue = null)
    {
        try
        {
            return configuration.GetRefValue(varName);
        }
        catch
        {
            return defaultValue;
        }
    }
}
