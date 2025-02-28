using ApplicationBuilderHelpers.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers;

public static class ConfigurationExtensions
{
    public static bool ContainsVarRefValue(this IConfiguration configuration, string varName)
    {
        try
        {
            return !string.IsNullOrEmpty(configuration.GetVarRefValue(varName));
        }
        catch
        {
            return false;
        }
    }

    public static string GetVarRefValue(this IConfiguration configuration, string varName)
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

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetVarRefValueOrDefault(this IConfiguration configuration, string varName, string? defaultValue = null)
    {
        try
        {
            return configuration.GetVarRefValue(varName);
        }
        catch
        {
            return defaultValue;
        }
    }
}
