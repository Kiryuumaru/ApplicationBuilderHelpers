using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Base class for defining application dependencies.
/// </summary>
public abstract class ApplicationDependency
{
    /// <summary>
    /// This method is called first to prepare the application builder before any other configuration methods are invoked.
    /// </summary>
    /// <param name="builder">The application dependency builder.</param>
    public virtual void BuilderPreparation(ApplicationDependencyBuilder builder)
    {
    }

    /// <summary>
    /// This method is called after <see cref="BuilderPreparation"/> to add configuration to the services.
    /// </summary>
    /// <param name="builder">The application dependency builder.</param>
    /// <param name="configuration">The configuration to add.</param>
    public virtual void AddConfiguration(ApplicationDependencyBuilder builder, IConfiguration configuration)
    {
    }

    /// <summary>
    /// This method is called after <see cref="AddConfiguration"/> to add services to the service collection.
    /// </summary>
    /// <param name="builder">The application dependency builder.</param>
    /// <param name="services">The service collection to add services to.</param>
    public virtual void AddServices(ApplicationDependencyBuilder builder, IServiceCollection services)
    {
    }

    /// <summary>
    /// This method is called after <see cref="AddServices"/> to add middlewares to the host.
    /// </summary>
    /// <param name="builder">The application dependency builder.</param>
    /// <param name="host">The host to add middlewares to.</param>
    public virtual void AddMiddlewares(ApplicationDependencyBuilder builder, IHost host)
    {
    }

    /// <summary>
    /// This method is called after <see cref="AddMiddlewares"/> to add mappings to the host.
    /// </summary>
    /// <param name="builder">The application dependency builder.</param>
    /// <param name="host">The host to add mappings to.</param>
    public virtual void AddMappings(ApplicationDependencyBuilder builder, IHost host)
    {
    }
}
