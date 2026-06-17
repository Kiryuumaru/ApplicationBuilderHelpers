using Infrastructure.OpenTelemetry.Logger.Services;

namespace Infrastructure.OpenTelemetry.Logger.LogEventPropertyTypes;

internal sealed class GuidPropertyParser : LogEventPropertyParser<Guid>
{
    public static GuidPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        if (Guid.TryParse(dataStr, out var result))
        {
            return result;
        }
        return null;
    }
}
