using Infrastructure.OpenTelemetry.Logger.Services;

namespace Infrastructure.OpenTelemetry.Logger.LogEventPropertyTypes;

internal sealed class ShortPropertyParser : LogEventPropertyParser<short>
{
    public static ShortPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        if (short.TryParse(dataStr, out var result))
        {
            return result;
        }
        return null;
    }
}
