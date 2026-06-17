using Infrastructure.OpenTelemetry.Logger.Services;

namespace Infrastructure.OpenTelemetry.Logger.LogEventPropertyTypes;

internal sealed class StringPropertyParser : LogEventPropertyParser<string>
{
    public static StringPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        return dataStr;
    }
}
