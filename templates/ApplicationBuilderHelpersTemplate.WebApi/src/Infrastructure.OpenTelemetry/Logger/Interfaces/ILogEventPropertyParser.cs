namespace Infrastructure.OpenTelemetry.Logger.Interfaces;

internal interface ILogEventPropertyParser
{
    string TypeIdentifier { get; }

    object? Parse(string? dataStr);
}
