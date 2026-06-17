using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Extensions;

public static class CommonConfigurationExtensions
{
    private static Guid? _runtimeGuid = null;
    public static Guid GetRuntimeGuid(this IConfiguration configuration)
    {
        if (_runtimeGuid == null)
        {
            _runtimeGuid = Guid.Parse(configuration.GetRefValueOrDefault("RUNTIME_RUNTIME_GUID", Guid.NewGuid().ToString()));
        }
        return _runtimeGuid.Value;
    }

    private const string ServiceNameKey = "RUNTIME_SERVICE_NAME";
    public static string GetServiceName(this IConfiguration configuration)
    {
        return configuration.GetRefValue(ServiceNameKey);
    }
    public static void SetServiceName(this IConfiguration configuration, string serviceName)
    {
        configuration[ServiceNameKey] = serviceName;
    }

    private const string LogsDumpDirectoryKey = "RUNTIME_LOGS_DUMP_DIRECTORY";
    public static AbsolutePath? GetLogsDumpDirectory(this IConfiguration configuration)
    {
        var logsDumpDirStr = configuration.GetRefValueOrDefault(LogsDumpDirectoryKey);
        if (string.IsNullOrWhiteSpace(logsDumpDirStr))
        {
            return null;
        }
        return AbsolutePath.Create(logsDumpDirStr);
    }
    public static void SetLogsDumpDirectory(this IConfiguration configuration, AbsolutePath? logsDumpDir)
    {
        configuration[LogsDumpDirectoryKey] = logsDumpDir?.ToString();
    }

    private const string LoggerLevelKey = "RUNTIME_LOGGER_LEVEL";
    public static LogLevel GetLoggerLevel(this IConfiguration configuration)
    {
        var loggerLevel = configuration.GetRefValueOrDefault(LoggerLevelKey, LogLevel.Information.ToString());
        return Enum.Parse<LogLevel>(loggerLevel);
    }
    public static void SetLoggerLevel(this IConfiguration configuration, LogLevel loggerLevel)
    {
        configuration[LoggerLevelKey] = loggerLevel.ToString();
    }

    private const string HomePathKey = "RUNTIME_HOME_PATH";
    public static AbsolutePath GetHomePath(this IConfiguration configuration)
    {
        return configuration.GetRefValueOrDefault(HomePathKey, AbsolutePath.Create(Environment.CurrentDirectory));
    }
    public static void SetHomePath(this IConfiguration configuration, AbsolutePath dataPath)
    {
        configuration[HomePathKey] = dataPath;
    }

    public static AbsolutePath GetTempPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "temp";
    }

    public static AbsolutePath GetDownloadsPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "downloads";
    }

    public static AbsolutePath GetServicesPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "svc";
    }

    public static AbsolutePath GetBinPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "bin";
    }

    public static AbsolutePath GetAssetsPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "assets";
    }

    public static AbsolutePath GetReleasesPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "releases";
    }

    public static AbsolutePath GetLogsPath(this IConfiguration configuration)
    {
        return configuration.GetHomePath() / "logs";
    }
}