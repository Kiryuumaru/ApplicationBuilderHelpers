using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;

namespace Application.Shared.Extensions;

/// <summary>
/// Type-specific configuration extensions that wrap ApplicationBuilderHelpers methods.
/// These methods support @ref: reference chains via GetRefValueOrDefault.
/// </summary>
public static class ConfigurationExtensions
{
    public static bool? GetBoolean(this IConfiguration configuration, string key)
    {
        var valueStr = configuration.GetRefValueOrDefault(key, "");
        if (string.IsNullOrEmpty(valueStr))
        {
            return null;
        }
        if (bool.TryParse(valueStr, out var value))
        {
            return value;
        }
        var valueStrLower = valueStr.ToLowerInvariant();
        return
            valueStrLower.Equals("enabled") ||
            valueStrLower.Equals("enable") ||
            valueStrLower.Equals("true") ||
            valueStrLower.Equals("yes") ||
            valueStrLower.Equals("1");
    }

    public static bool GetBooleanOrDefault(this IConfiguration configuration, string key, bool defaultValue = false)
    {
        var valueStr = configuration.GetRefValueOrDefault(key, defaultValue ? "1" : "0");
        if (bool.TryParse(valueStr, out var value))
        {
            return value;
        }
        var valueStrLower = valueStr.ToLowerInvariant();
        return
            valueStrLower.Equals("enabled") ||
            valueStrLower.Equals("enable") ||
            valueStrLower.Equals("true") ||
            valueStrLower.Equals("yes") ||
            valueStrLower.Equals("1");
    }

    public static void SetBoolean(this IConfiguration configuration, string key, bool value)
    {
        configuration[key] = value ? "true" : "false";
    }
}
