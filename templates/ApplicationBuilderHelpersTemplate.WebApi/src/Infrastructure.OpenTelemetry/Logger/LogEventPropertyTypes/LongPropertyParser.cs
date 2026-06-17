using Infrastructure.OpenTelemetry.Logger.Services;

namespace Infrastructure.OpenTelemetry.Logger.LogEventPropertyTypes;

internal sealed class LongPropertyParser : LogEventPropertyParser<long>
{
    public static LongPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        if (long.TryParse(dataStr, out var result))
        {
            return result;
        }
        return null;
    }
}
