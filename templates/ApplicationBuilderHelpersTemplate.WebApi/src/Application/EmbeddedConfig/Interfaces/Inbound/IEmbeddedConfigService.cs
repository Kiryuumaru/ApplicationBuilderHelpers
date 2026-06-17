namespace Application.EmbeddedConfig.Interfaces.Inbound;

/// <summary>
/// Application service for retrieving environment-specific embedded configuration.
/// </summary>
public interface IEmbeddedConfigService
{
    Task<Models.EmbeddedConfigResult> GetConfig(string envTag, CancellationToken cancellationToken);

    Task<Models.EmbeddedConfigResult> GetConfig(CancellationToken cancellationToken);
}
