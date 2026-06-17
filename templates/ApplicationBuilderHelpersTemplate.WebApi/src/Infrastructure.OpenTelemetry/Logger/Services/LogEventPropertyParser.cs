using Infrastructure.OpenTelemetry.Logger.Interfaces;

namespace Infrastructure.OpenTelemetry.Logger.Services;

internal abstract class LogEventPropertyParser<T> : ILogEventPropertyParser
{
    public string TypeIdentifier => typeof(T).Name;

    public abstract object? Parse(string? dataStr);
}
