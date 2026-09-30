using ApplicationBuilderHelpers.Exceptions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary><c>@ref:</c> reference resolution for <c>IConfiguration</c>.</summary>
public static class ConfigurationExtensions
{
    /// <summary>Resolves <paramref name="varName"/> through its <c>@ref:</c> chain.</summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name.</param>
    /// <param name="value">The resolved value, or null.</param>
    /// <returns>True when resolved; otherwise, false.</returns>
    public static bool TryGetRefValue(this IConfiguration configuration, string varName, [NotNullWhen(true)] out string? value)
    {
        const int maxDepth = 32;
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        string? varValue = $"@ref:{varName}";
        while (true)
        {
            if (varValue.StartsWith("@ref:"))
            {
                varName = varValue[5..];
                if (!visited.Add(varName) || visited.Count > maxDepth)
                {
                    value = null;
                    return false;
                }
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

    /// <summary>True when <paramref name="varName"/> resolves.</summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name.</param>
    /// <returns>True when the value exists and resolves; otherwise, false.</returns>
    public static bool ContainsRefValue(this IConfiguration configuration, string varName)
    {
        return TryGetRefValue(configuration, varName, out _);
    }

    /// <summary>Returns the resolved value; throws when the chain cannot resolve.</summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name.</param>
    /// <returns>The resolved configuration value.</returns>
    /// <exception cref="NoConfigValueException">Thrown when the value cannot be resolved.</exception>
    public static string GetRefValue(this IConfiguration configuration, string varName)
    {
        if (!TryGetRefValue(configuration, varName, out var value))
        {
            throw new NoConfigValueException(varName);
        }
        return value;
    }

    /// <summary>Returns the resolved value, or <paramref name="defaultValue"/> when the chain cannot resolve.</summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="varName">The variable name.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <returns>The resolved value or the default value.</returns>
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
