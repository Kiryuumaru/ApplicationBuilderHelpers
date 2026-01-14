using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <inheritdoc/>
public abstract class ApplicationDependency : IApplicationDependency
{
    /// <inheritdoc/>
    public virtual void CommandPreparation(ApplicationBuilder applicationBuilder)
    {
    }

    /// <inheritdoc/>
    public virtual void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
    {
    }

    /// <inheritdoc/>
    public virtual void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
    }

    /// <inheritdoc/>
    public virtual void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
    }

    /// <inheritdoc/>
    public virtual void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
    }

    /// <inheritdoc/>
    public virtual void AddMappings(ApplicationHost applicationHost, IHost host)
    {
    }

    /// <inheritdoc/>
    public virtual void RunPreparation(ApplicationHost applicationHost)
    {
    }

    /// <inheritdoc/>
    public virtual ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
