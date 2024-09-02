using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies.
/// </summary>
public abstract class ApplicationHostBuilder(IHostApplicationBuilder builder) : IEnumerable<ApplicationDependency>
{
    internal List<ApplicationDependency> ApplicationDependencies { get; set; } = [];

    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public IHostApplicationBuilder Builder { get; } = builder;

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> associated with the <see cref="Builder"/>.
    /// </summary>
    public IConfiguration Configuration => Builder.Configuration;

    /// <inheritdoc/>
    public IEnumerator<ApplicationDependency> GetEnumerator()
    {
        return ApplicationDependencies.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Represents a builder for managing application dependencies for a specific host application builder type.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The type of the host application builder.</typeparam>
public class ApplicationHostBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder builder) : ApplicationHostBuilder(builder)
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public new THostApplicationBuilder Builder => (THostApplicationBuilder)base.Builder;

    /// <summary>
    /// Adds an <see cref="ApplicationDependency"/> to the builder.
    /// </summary>
    /// <param name="applicationDependency">The application dependency to add.</param>
    /// <returns>The instance of the builder.</returns>
    public ApplicationHostBuilder<THostApplicationBuilder> Add(ApplicationDependency applicationDependency)
    {
        ApplicationDependencies.Add(applicationDependency);
        return this;
    }

    /// <summary>
    /// Adds an <see cref="ApplicationDependency"/> of type <typeparamref name="TApplicationDependency"/>.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The type of <see cref="ApplicationDependency"/> to add.</typeparam>
    /// <returns>The instance of the builder.</returns>
    public ApplicationHostBuilder<THostApplicationBuilder> Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : ApplicationDependency
    {
        var instance = Activator.CreateInstance<TApplicationDependency>();
        ApplicationDependencies.Add(instance);
        return this;
    }

    /// <summary>
    /// Builds the configured application.
    /// </summary>
    /// <returns>An instance of <see cref="ApplicationDependencyBuilderApp{THostApplicationBuilder}"/>.</returns>
    /// <exception cref="Exception">Thrown if the builder does not have a build method or if the resulting object is not an <see cref="IHost"/>.</exception>
    public ApplicationHost<THostApplicationBuilder> Build()
    {
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.BuilderPreparation(this);
        }
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddConfiguration(this, Builder.Configuration);
        }
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddServices(this, Builder.Services);
        }

        if (Builder.GetType().GetMethod("Build") is not MethodInfo builderBuildethodInfo)
        {
            throw new Exception("Builder does not have a build method.");
        }

        var appObj = builderBuildethodInfo.Invoke(Builder, null);

        if (appObj is not IHost host)
        {
            throw new Exception($"App does not support type {appObj?.GetType()?.FullName}.");
        }

        return new(Builder, host)
        {
            ApplicationDependencies = ApplicationDependencies,
        };
    }
}