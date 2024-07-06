using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies.
/// </summary>
public abstract class ApplicationDependencyBuilder(IHostApplicationBuilder builder) : IEnumerable<ApplicationDependency>
{
    protected readonly List<ApplicationDependency> ApplicationDependencies = [];

    /// <summary>
    /// Creates an instance of <see cref="ApplicationDependencyBuilder"/> from an existing <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>An instance of ApplicationDependencyBuilder.</returns>
    public static ApplicationDependencyBuilder<THostApplicationBuilder> FromBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder applicationBuilder)
        where THostApplicationBuilder : IHostApplicationBuilder
    {
        ApplicationRuntime.Configuration = applicationBuilder.Configuration;
        return new(applicationBuilder);
    }

    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public IHostApplicationBuilder Builder { get; } = builder;

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> associated with the <see cref="Builder"/>.
    /// </summary>
    public IConfiguration Configuration => Builder.Configuration;

    /// <summary>
    /// Adds an <see cref="ApplicationDependency"/>.
    /// </summary>
    /// <param name="applicationDependency">The application dependency to add.</param>
    /// <returns>The instance of the builder.</returns>
    public ApplicationDependencyBuilder Add(ApplicationDependency applicationDependency)
    {
        ApplicationDependencies.Add(applicationDependency);
        return this;
    }

    /// <summary>
    /// Adds an <see cref="ApplicationDependency"/> of type <typeparamref name="TApplicationDependency"/>.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The type of <see cref="ApplicationDependency"/> to add.</typeparam>
    /// <returns>The instance of the builder.</returns>
    public ApplicationDependencyBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : ApplicationDependency
    {
        var instance = Activator.CreateInstance<TApplicationDependency>();
        ApplicationDependencies.Add(instance);
        return this;
    }

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

    /// <summary>
    /// Runs the configured application.
    /// </summary>
    public abstract void Run();
}

/// <summary>
/// Represents a builder for managing application dependencies.
/// </summary>
public class ApplicationDependencyBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder builder) : ApplicationDependencyBuilder(builder)
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public new THostApplicationBuilder Builder { get => (THostApplicationBuilder)base.Builder; }

    /// <inheritdoc/>
    public override void Run()
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
            throw new Exception("Builder does not have build");
        }

        var appObj = builderBuildethodInfo.Invoke(Builder, null);

        if (appObj is not IHost app)
        {
            throw new Exception("App does not support " + appObj?.GetType()?.FullName);
        }

        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddMiddlewares(this, app);
        }

        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddMappings(this, app);
        }

        if (appObj.GetType().GetMethod("Run") is MethodInfo appRunMethodInfo)
        {
            if (appRunMethodInfo.GetParameters().Length == 0)
            {
                appRunMethodInfo.Invoke(appObj, null);
            }
            else if (appRunMethodInfo.GetParameters().Length == 1)
            {
                appRunMethodInfo.Invoke(appObj, [null]);
            }
            else
            {
                throw new Exception("App run does not support " + appObj?.GetType()?.FullName);
            }
        }
        else
        {
            HostingAbstractionsHostExtensions.Run(app);
        }
    }
}
