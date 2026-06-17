using Infrastructure.OpenTelemetry.Abstractions;

namespace Infrastructure.OpenTelemetry.LogEventPropertyTypes;

internal class DateTimeOffsetPropertyParser : LogEventPropertyParser<DateTimeOffset>
{
    public static DateTimeOffsetPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        if (DateTimeOffset.TryParse(dataStr, out var result))
        {
            return result;
        }
        return null;
    }
}
