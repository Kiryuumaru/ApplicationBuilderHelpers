using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ApplicationBuilderHelpers;

public class ApplicationDependencyBuilder : IEnumerable<ApplicationDependency>
{
    public static ApplicationDependencyBuilder FromBuilder(IHostApplicationBuilder builder)
    {
        ApplicationRuntime.Configuration = builder.Configuration;
        return new(builder);
    }

    private readonly List<ApplicationDependency> _applicationDependencies = [];

    public IHostApplicationBuilder Builder { get; }

    public IConfiguration Configuration => Builder.Configuration;

    protected ApplicationDependencyBuilder(IHostApplicationBuilder builder)
    {
        Builder = builder;
    }

    public void Add<TApplicationDependency>()
        where TApplicationDependency : ApplicationDependency
    {
        var instance = Activator.CreateInstance<TApplicationDependency>();
        _applicationDependencies.Add(instance);
    }

    public void Add(ApplicationDependency applicationDependency)
    {
        _applicationDependencies.Add(applicationDependency);
    }

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

    public IEnumerator<ApplicationDependency> GetEnumerator()
    {
        return _applicationDependencies.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
