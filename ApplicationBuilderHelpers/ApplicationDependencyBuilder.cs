using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies.
/// </summary>
public class ApplicationDependencyBuilder : IEnumerable<ApplicationDependency>
{
    /// <summary>
    /// Creates an instance of <see cref="ApplicationDependencyBuilder"/> from an existing <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>An instance of ApplicationDependencyBuilder.</returns>
    public static ApplicationDependencyBuilder FromBuilder(IHostApplicationBuilder builder)
    {
        ApplicationRuntime.Configuration = builder.Configuration;
        return new(builder);
    }

    private readonly List<ApplicationDependency> _applicationDependencies = [];

    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public IHostApplicationBuilder Builder { get; }

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> associated with the <see cref="Builder"/>.
    /// </summary>
    public IConfiguration Configuration => Builder.Configuration;

    /// <summary>
    /// Constructs an instance of <see cref="ApplicationDependencyBuilder"/>.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    protected ApplicationDependencyBuilder(IHostApplicationBuilder builder)
    {
        Builder = builder;
    }

    /// <summary>
    /// Adds an <see cref="ApplicationDependency"/>.
    /// </summary>
    /// <param name="applicationDependency">The application dependency to add.</param>
    public void Add(ApplicationDependency applicationDependency)
    {
        _applicationDependencies.Add(applicationDependency);
    }

    /// <summary>
    /// Adds an <see cref="ApplicationDependency"/> of type <typeparamref name="TApplicationDependency"/>.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The type of <see cref="ApplicationDependency"/> to add.</typeparam>
    public void Add<TApplicationDependency>()
        where TApplicationDependency : ApplicationDependency
    {
        var instance = Activator.CreateInstance<TApplicationDependency>();
        _applicationDependencies.Add(instance);
    }

    /// <summary>
    /// Runs the configured application.
    /// </summary>
    public void Run()
    {
        foreach (var applicationDependency in _applicationDependencies)
        {
            applicationDependency.BuilderPreparation(this);
        }
        foreach (var applicationDependency in _applicationDependencies)
        {
            applicationDependency.AddConfiguration(this, Builder.Configuration);
        }
        foreach (var applicationDependency in _applicationDependencies)
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

        foreach (var applicationDependency in _applicationDependencies)
        {
            applicationDependency.AddMiddlewares(this, app);
        }

        foreach (var applicationDependency in _applicationDependencies)
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

    /// <inheritdoc/>
    public IEnumerator<ApplicationDependency> GetEnumerator()
    {
        return _applicationDependencies.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
