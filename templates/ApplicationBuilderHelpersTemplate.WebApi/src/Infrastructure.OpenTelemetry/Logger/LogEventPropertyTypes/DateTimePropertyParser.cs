using Infrastructure.OpenTelemetry.Logger.Services;

namespace Infrastructure.OpenTelemetry.Logger.LogEventPropertyTypes;

internal sealed class DateTimePropertyParser : LogEventPropertyParser<DateTime>
{
    public static DateTimePropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        if (DateTime.TryParse(dataStr, out var result))
        {
            return result;
        }
        return null;
    }
}
