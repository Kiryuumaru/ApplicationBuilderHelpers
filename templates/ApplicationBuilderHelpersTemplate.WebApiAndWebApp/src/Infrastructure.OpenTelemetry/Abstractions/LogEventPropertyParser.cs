using Infrastructure.OpenTelemetry.Interfaces;

namespace Infrastructure.OpenTelemetry.Abstractions;

internal abstract class LogEventPropertyParser<T> : ILogEventPropertyParser
{
    public string TypeIdentifier => typeof(T).Name;

    public abstract object? Parse(string? dataStr);
}
