using ApplicationBuilderHelpers.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>
/// Extension methods for IConfiguration to handle reference values that can point to other configuration keys.
/// Reference values use the format "@ref:keyName" to point to another configuration key.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Attempts to get a configuration value, resolving any reference chains.
    /// If the value starts with "@ref:", it will follow the reference to get the actual value.
    /// Supports chained references (references that point to other references).
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to get the value for.</param>
    /// <param name="value">When this method returns, contains the resolved value if found; otherwise, null.</param>
    /// <returns>True if the value was found and resolved successfully; otherwise, false.</returns>
    public static bool TryGetRefValue(this IConfiguration configuration, string varName, [NotNullWhen(true)] out string? value)
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
                    value = null;
                    return false;
                }
                continue;
            }
            break;
        }
        value = varValue;
        return true;
    }

    /// <summary>
    /// Checks if the configuration contains a value (including resolved references) for the specified variable name.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to check.</param>
    /// <returns>True if the value exists and can be resolved; otherwise, false.</returns>
    public static bool ContainsRefValue(this IConfiguration configuration, string varName)
    {
        return TryGetRefValue(configuration, varName, out _);
    }

    /// <summary>
    /// Gets the resolved configuration value for the specified variable name, following any reference chains.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to get the value for.</param>
    /// <returns>The resolved configuration value.</returns>
    /// <exception cref="NoConfigValueException">Thrown when the value is not found or cannot be resolved.</exception>
    public static string GetRefValue(this IConfiguration configuration, string varName)
    {
        if (!TryGetRefValue(configuration, varName, out var value))
        {
            throw new NoConfigValueException(varName);
        }
        return value;
    }

    /// <summary>
    /// Gets the resolved configuration value for the specified variable name, or a default value if not found.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name to get the value for.</param>
    /// <param name="defaultValue">The default value to return if the value is not found or cannot be resolved.</param>
    /// <returns>The resolved configuration value or the default value.</returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetRefValueOrDefault(this IConfiguration configuration, string varName, string? defaultValue = null)
    {
        if (TryGetRefValue(configuration, varName, out var value))
        {
            return value;
        }
        return defaultValue;
    }
}
