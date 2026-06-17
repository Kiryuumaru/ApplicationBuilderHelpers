using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AppEnvironment.Extensions;

public static class AppEnvironmentConfigurationExtensions
{
    private const string AppTagOverrideKey = "VEG_APP_TAG_OVERRIDE";
    public static string? GetAppTagOverride(this IConfiguration configuration)
    {
        return configuration.GetRefValueOrDefault(AppTagOverrideKey);
    }
    public static void SetAppTagOverride(this IConfiguration configuration, string appTag)
    {
        configuration[AppTagOverrideKey] = appTag;
    }

    private const string DeviceSerialIdentifierOverrideKey = "VEG_APP_DEVICE_SERIAL_IDENTIFIER_OVERRIDE";
    public static string? GetDeviceSerialIdentifierOverride(this IConfiguration configuration)
    {
        return configuration.GetRefValueOrDefault(DeviceSerialIdentifierOverrideKey);
    }
    public static void SetDeviceSerialIdentifierOverride(this IConfiguration configuration, string? deviceSerialIdentifier)
    {
        configuration[DeviceSerialIdentifierOverrideKey] = deviceSerialIdentifier;
    }

    private const string NetworkCodeOverrideKey = "VEG_APP_NETWORK_CODE_OVERRIDE";
    public static string? GetNetworkCodeOverride(this IConfiguration configuration)
    {
        return configuration.GetRefValueOrDefault(NetworkCodeOverrideKey);
    }
    public static void SetNetworkCodeOverride(this IConfiguration configuration, string? networkCode)
    {
        configuration[NetworkCodeOverrideKey] = networkCode;
    }
}
