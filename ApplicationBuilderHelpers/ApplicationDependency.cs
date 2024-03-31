using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ApplicationBuilderHelpers;

public abstract class ApplicationDependency
{
    public virtual void BuilderPreparation(ApplicationDependencyBuilder builder)
    {
    }

    public virtual void AddConfiguration(ApplicationDependencyBuilder builder, IConfiguration configuration)
    {
    }

    public virtual void AddServices(ApplicationDependencyBuilder builder, IServiceCollection services)
    {
    }

    public virtual void AddMiddlewares(ApplicationDependencyBuilder builder, IHost host)
    {
    }

    public virtual void AddMappings(ApplicationDependencyBuilder builder, IHost host)
    {
    }
}
