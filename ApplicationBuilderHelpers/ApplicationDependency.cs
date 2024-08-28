using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Provides a base class for defining application dependencies, offering hooks for configuring and preparing the application during its startup phase.
/// </summary>
public abstract class ApplicationDependency
{
    /// <summary>
    /// Invoked first during the application setup, allowing the application builder to be prepared before any other configuration methods are called.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    public virtual void BuilderPreparation(ApplicationDependencyBuilder builder)
    {
    }

    /// <summary>
    /// Called after <see cref="BuilderPreparation"/> to add configuration settings from a given <see cref="IConfiguration"/> source to the application builder.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    /// <param name="configuration">The configuration source containing settings to be added.</param>
    public virtual void AddConfiguration(ApplicationDependencyBuilder builder, IConfiguration configuration)
    {
    }

    /// <summary>
    /// Called after <see cref="AddConfiguration"/> to register services with the application's <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    /// <param name="services">The service collection where services are registered.</param>
    public virtual void AddServices(ApplicationDependencyBuilder builder, IServiceCollection services)
    {
    }

    /// <summary>
    /// Called after <see cref="AddServices"/> to add middleware components to the application's <see cref="IHost"/>.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    /// <param name="host">The host where middleware components are added.</param>
    public virtual void AddMiddlewares(ApplicationDependencyBuilder builder, IHost host)
    {
    }

    /// <summary>
    /// Called after <see cref="AddMiddlewares"/> to define endpoint mappings or other routing configurations for the application's <see cref="IHost"/>.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    /// <param name="host">The host where endpoint mappings or other routing configurations are defined.</param>
    public virtual void AddMappings(ApplicationDependencyBuilder builder, IHost host)
    {
    }

    /// <summary>
    /// Invoked last during the application setup process, this method finalizes the application builder's preparation before the application is run.
    /// </summary>
    /// <param name="builder">The application dependency builder.</param>
    public virtual void RunPreparation(ApplicationDependencyBuilder builder)
    {
    }
}
