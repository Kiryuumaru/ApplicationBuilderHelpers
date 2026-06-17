using Infrastructure.OpenTelemetry.Logger.Services;

namespace Infrastructure.OpenTelemetry.Logger.LogEventPropertyTypes;

internal sealed class IntPropertyParser : LogEventPropertyParser<int>
{
    public static IntPropertyParser Default { get; } = new();

    public override object? Parse(string? dataStr)
    {
        if (int.TryParse(dataStr, out var result))
        {
            return result;
        }
        return null;
    }
}
