﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Provides a defining application dependencies, offering hooks for configuring and preparing the application during its startup phase.
/// </summary>
public interface IApplicationDependency
{
    /// <summary>
    /// Invoked first during the application setup, allowing the application builder to be prepared before any other configuration methods are called.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    void BuilderPreparation(ApplicationHostBuilder applicationBuilder);

    /// <summary>
    /// Called after <see cref="BuilderPreparation"/> to add configuration settings from a given <see cref="IConfiguration"/> source to the application builder.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    /// <param name="configuration">The configuration source containing settings to be added.</param>
    void AddConfiguration(ApplicationHostBuilder applicationBuilder, IConfiguration configuration);

    /// <summary>
    /// Called after <see cref="AddConfiguration"/> to register services with the application's <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The application dependency builder used to configure the application.</param>
    /// <param name="services">The service collection where services are registered.</param>
    void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services);

    /// <summary>
    /// Called after <see cref="AddServices"/> to add middleware components to the application's <see cref="IHost"/>.
    /// </summary>
    /// <param name="applicationHost">The application dependency host used to configure the application.</param>
    /// <param name="host">The host where middleware components are added.</param>
    void AddMiddlewares(ApplicationHost applicationHost, IHost host);

    /// <summary>
    /// Called after <see cref="AddMiddlewares"/> to define endpoint mappings or other routing configurations for the application's <see cref="IHost"/>.
    /// </summary>
    /// <param name="applicationHost">The application dependency host used to configure the application.</param>
    /// <param name="host">The host where endpoint mappings or other routing configurations are defined.</param>
    void AddMappings(ApplicationHost applicationHost, IHost host);

    /// <summary>
    /// Invoked last during the application setup process, this method finalizes the application builder's preparation before the application is run.
    /// </summary>
    /// <param name="applicationHost">The application dependency host.</param>
    void RunPreparation(ApplicationHost applicationHost);
}
