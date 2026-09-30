using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view pipeline contract for host modules.
/// </summary>
public interface IApplicationDependency
{
    /// <summary>
    /// Registers commands and builder state before the host pipeline starts.
    /// </summary>
    /// <param name="applicationBuilder">The entry builder receiving command registrations.</param>
    void CommandPreparation(ApplicationBuilder applicationBuilder);

    /// <summary>
    /// Adjusts the host builder before configuration and services load.
    /// </summary>
    /// <param name="applicationBuilder">The host builder wrapper being prepared.</param>
    void BuilderPreparation(ApplicationHostBuilder applicationBuilder);

    /// <summary>
    /// Contributes configuration sources to the host builder.
    /// </summary>
    /// <param name="applicationBuilder">The host builder wrapper receiving configuration.</param>
    /// <param name="configuration">The current configuration snapshot to extend.</param>
    void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration);

    /// <summary>
    /// Registers services into the host service collection.
    /// </summary>
    /// <param name="applicationBuilder">The host builder wrapper being prepared.</param>
    /// <param name="services">The service collection receiving registrations.</param>
    void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services);

    /// <summary>
    /// Wires middleware onto the built host.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    /// <param name="host">The built <see cref="IHost"/> receiving middleware.</param>
    void AddMiddlewares(ApplicationHost applicationHost, IHost host);

    /// <summary>
    /// Maps endpoints or routing onto the built host.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    /// <param name="host">The built <see cref="IHost"/> receiving mappings.</param>
    void AddMappings(ApplicationHost applicationHost, IHost host);

    /// <summary>
    /// Performs final synchronous setup before the host starts.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    void RunPreparation(ApplicationHost applicationHost);

    /// <summary>
    /// Performs final asynchronous setup before the host starts.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    /// <param name="cancellationToken">Token observing shutdown during setup.</param>
    /// <returns>A task completing when setup finishes.</returns>
    ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken);
}
