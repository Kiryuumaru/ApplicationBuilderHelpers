using Infrastructure.OpenTelemetry.Abstractions;

namespace Infrastructure.OpenTelemetry.LogEventPropertyTypes;

internal class StringPropertyParser : LogEventPropertyParser<string>
{
    public static StringPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        return dataStr;
    }
}
