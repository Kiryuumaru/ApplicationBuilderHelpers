using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <summary>
/// No-op base for host-pipeline modules; override only the lifecycle hooks the module owns.
/// </summary>
public abstract class ApplicationDependency : IApplicationDependency
{
    /// <summary>
    /// Registers commands and builder state before the host pipeline starts; no-op by default.
    /// </summary>
    /// <param name="applicationBuilder">The entry builder receiving command registrations.</param>
    public virtual void CommandPreparation(ApplicationBuilder applicationBuilder)
    {
    }

    /// <summary>
    /// Adjusts the host builder before configuration and services load; no-op by default.
    /// </summary>
    /// <param name="applicationBuilder">The host builder wrapper being prepared.</param>
    public virtual void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
    {
    }

    /// <summary>
    /// Contributes configuration sources to the host builder; no-op by default.
    /// </summary>
    /// <param name="applicationBuilder">The host builder wrapper receiving configuration.</param>
    /// <param name="configuration">The current configuration snapshot to extend.</param>
    public virtual void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
    }

    /// <summary>
    /// Registers services into the host service collection; no-op by default.
    /// </summary>
    /// <param name="applicationBuilder">The host builder wrapper being prepared.</param>
    /// <param name="services">The service collection receiving registrations.</param>
    public virtual void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
    }

    /// <summary>
    /// Wires middleware onto the built host; no-op by default.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    /// <param name="host">The built <see cref="IHost"/> receiving middleware.</param>
    public virtual void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
    }

    /// <summary>
    /// Maps endpoints or routing onto the built host; no-op by default.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    /// <param name="host">The built <see cref="IHost"/> receiving mappings.</param>
    public virtual void AddMappings(ApplicationHost applicationHost, IHost host)
    {
    }

    /// <summary>
    /// Performs final synchronous setup before the host starts; no-op by default.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    public virtual void RunPreparation(ApplicationHost applicationHost)
    {
    }

    /// <summary>
    /// Performs final asynchronous setup before the host starts; completes synchronously by default.
    /// </summary>
    /// <param name="applicationHost">The built host wrapper.</param>
    /// <param name="cancellationToken">Token observing shutdown during setup.</param>
    /// <returns>A task completing when setup finishes.</returns>
    public virtual ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
