using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Application.Logger.Extensions;

public static class ILoggerExtensions
{
    public const string SourceContextActionIdentifier = "SourceContextAction";
    public const string SourceContextActionsIdentifier = "SourceContextActions";
    public const string SourceContextActionSeparator = "___";
    public const string SourceContextActionIdentifierAndSeparator = $"{SourceContextActionIdentifier}{SourceContextActionSeparator}";

    public static IDisposable? BeginScopeMap(this ILogger logger, [CallerMemberName] string? contextAction = null, Dictionary<string, object?>? scopeMap = null)
    {
        scopeMap ??= [];
        if (!string.IsNullOrEmpty(contextAction) && logger.GetType().GenericTypeArguments is Type[] genericTypes && genericTypes.Length == 1)
        {
            var genericType = genericTypes[0];
            scopeMap[$"{SourceContextActionIdentifierAndSeparator}{genericType.FullName}{SourceContextActionSeparator}{contextAction}"] = true;
        }
        return logger.BeginScope(scopeMap);
    }

    public static void Info(this ILogger logger, string message, params object?[] args)
    {
        logger.LogInformation(message, args);
    }

    public static void Warn(this ILogger logger, string message, params object?[] args)
    {
        logger.LogWarning(message, args);
    }

    public static void Error(this ILogger logger, string message, params object?[] args)
    {
        logger.LogError(message, args);
    }

    public static void Debug(this ILogger logger, string message, params object?[] args)
    {
        logger.LogDebug(message, args);
    }

    public static void Trace(this ILogger logger, string message, params object?[] args)
    {
        logger.LogTrace(message, args);
    }

    public static void Critical(this ILogger logger, string message, params object?[] args)
    {
        logger.LogCritical(message, args);
    }

    public static void Log(this ILogger logger, LogLevel logLevel, string message)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.Trace(message);
                break;
            case LogLevel.Debug:
                logger.Debug(message);
                break;
            case LogLevel.Information:
                logger.Info(message);
                break;
            case LogLevel.Warning:
                logger.Warn(message);
                break;
            case LogLevel.Error:
                logger.Error(message);
                break;
            case LogLevel.Critical:
                logger.Critical(message);
                break;
        }
    }
}
