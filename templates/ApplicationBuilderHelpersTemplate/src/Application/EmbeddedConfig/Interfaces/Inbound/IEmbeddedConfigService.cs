namespace Application.EmbeddedConfig.Interfaces.Inbound;

/// <summary>
/// Application service for retrieving environment-specific and shared embedded configuration.
/// </summary>
public interface IEmbeddedConfigService
{
    Task<Models.EmbeddedConfigResult> GetConfig(CancellationToken cancellationToken);
}
