using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Enumerable host-builder wrapper exposing the underlying builder, services, configuration, and dependency list.
/// </summary>
public abstract class ApplicationHostBuilderBase(IHostApplicationBuilder builder, List<IApplicationDependency>? applicationDependencies = null) : 
    IEnumerable<IApplicationDependency>
{
    internal List<IApplicationDependency> ApplicationDependencies { get; set; } = applicationDependencies ?? [];

    /// <summary>
    /// Gets the wrapped host builder receiving configuration and services.
    /// </summary>
    public IHostApplicationBuilder Builder { get; } = builder;

    /// <summary>
    /// Gets the service collection of <see cref="Builder"/>.
    /// </summary>
    public IServiceCollection Services => Builder.Services;

    /// <summary>
    /// Gets the configuration of <see cref="Builder"/>.
    /// </summary>
    public IConfiguration Configuration => Builder.Configuration;

    /// <summary>
    /// Enumerates the registered dependencies in registration order.
    /// </summary>
    /// <returns>An enumerator over the registered dependencies.</returns>
    public IEnumerator<IApplicationDependency> GetEnumerator()
    {
        return ApplicationDependencies.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Build-stage wrapper that prepares dependencies then builds the host.
/// </summary>
public abstract class ApplicationHostBuilder(IHostApplicationBuilder builder, List<IApplicationDependency>? applicationDependencies = null) : 
    ApplicationHostBuilderBase(builder, applicationDependencies)
{
    internal abstract ApplicationHost Build();

    internal ApplicationHost BuildInternal()
    {
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.BuilderPreparation(this);
        }
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddConfigurations(this, Builder.Configuration);
        }
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddServices(this, Builder.Services);
        }

        // The options flag skips registration of the startup/shutdown status messages;
        // the filter catches residual Microsoft.Hosting.Lifetime output while preserving
        // the error channel (Warning and above still flow).
        Services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
        Builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

        return Build();
    }
}

/// <summary>
/// Typed build-stage wrapper that constructs the host via the builder <c>Build</c> method.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The host-builder type this wrapper constructs.</typeparam>
public class ApplicationHostBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder builder) : ApplicationHostBuilder(builder)
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Gets the underlying builder cast to <typeparamref name="THostApplicationBuilder"/>.
    /// </summary>
    public new THostApplicationBuilder Builder => (THostApplicationBuilder)base.Builder;

    /// <summary>
    /// Appends a dependency instance to the pipeline and returns this builder.
    /// </summary>
    /// <param name="applicationDependency">The dependency instance to append.</param>
    /// <returns>This builder.</returns>
    public ApplicationHostBuilder<THostApplicationBuilder> AddApplication(IApplicationDependency applicationDependency)
    {
        ApplicationDependencies.Add(applicationDependency);
        return this;
    }

    /// <summary>
    /// Constructs a dependency of the given type, appends it, and returns this builder.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The dependency type to construct.</typeparam>
    /// <returns>This builder.</returns>
    public ApplicationHostBuilder<THostApplicationBuilder> AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : IApplicationDependency
    {
        var instance = Activator.CreateInstance<TApplicationDependency>();
        ApplicationDependencies.Add(instance);
        return this;
    }

    internal override ApplicationHost Build()
    {
        var appObj = BuildHostBuilder(typeof(THostApplicationBuilder), Builder);

        if (appObj is not IHost host)
        {
            throw new Exception($"App does not support type {appObj?.GetType()?.FullName}.");
        }

        return new ApplicationHost<THostApplicationBuilder>(Builder, host)
        {
            ApplicationDependencies = ApplicationDependencies,
        };
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo> BuildMethodCache = new();
    private static readonly object BuildMethodSyncRoot = new();

    private static object? BuildHostBuilder(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type builderType,
        object builder)
    {
        if (!BuildMethodCache.TryGetValue(builderType, out var buildMethod))
        {
            lock (BuildMethodSyncRoot)
            {
                if (!BuildMethodCache.TryGetValue(builderType, out buildMethod))
                {
                    var resolved = builderType.GetMethod("Build");
                    if (resolved is null)
                    {
                        throw new Exception("Builder does not have a build method.");
                    }

                    BuildMethodCache[builderType] = resolved;
                    buildMethod = resolved;
                }
            }
        }

        return buildMethod.Invoke(builder, null);
    }
}
