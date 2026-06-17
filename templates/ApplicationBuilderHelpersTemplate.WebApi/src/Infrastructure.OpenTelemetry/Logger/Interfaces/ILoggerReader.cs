namespace Infrastructure.OpenTelemetry.Logger.Interfaces;

internal interface ILoggerReader
{
    Task Start(int tail, bool follow, Dictionary<string, string> scope, CancellationToken cancellationToken = default);
}
