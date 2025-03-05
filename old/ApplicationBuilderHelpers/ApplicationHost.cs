using ApplicationBuilderHelpers.Exceptions;
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
/// Represents a builder for managing application dependencies and running the configured application.
/// </summary>
public abstract class ApplicationHost(IHostApplicationBuilder builder, IHost host) : ApplicationHostBuilder(builder)
{
    /// <summary>
    /// Creates an instance of <see cref="ApplicationHostBuilder"/> from an existing <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <typeparam name="THostApplicationBuilder">The type of the host application builder.</typeparam>
    /// <param name="applicationBuilder">The host application builder.</param>
    /// <returns>An instance of <see cref="ApplicationDependencyBuilderHost{THostApplicationBuilder}"/>.</returns>
    public static ApplicationHostBuilder<THostApplicationBuilder> FromBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder applicationBuilder)
        where THostApplicationBuilder : IHostApplicationBuilder
    {
        ApplicationRuntime.Configuration = applicationBuilder.Configuration;
        return new(applicationBuilder);
    }

    /// <summary>
    /// Gets the <see cref="IHost"/> created from <see cref="ApplicationDependencyBuilderHost{THostApplicationBuilder}.Build"/>.
    /// </summary>
    public IHost Host { get; protected set; } = host;

    /// <summary>
    /// Runs the configured application.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown if there is an error during application startup.</exception>
    public async Task<int> Run(CancellationToken cancellationToken = default)
    {
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddMiddlewares(this, Host);
        }

        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddMappings(this, Host);
        }

        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.RunPreparation(this);
        }

        try
        {
            await HostingAbstractionsHostExtensions.RunAsync(Host, cancellationToken);
        }
        catch (CommandException ex)
        {
            Console.WriteLine(ex.Message);
            return ex.ExitCode;
        }

        return 0;
    }
}

/// <summary>
/// Represents a builder for managing application dependencies and running the configured application.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The type of the host application builder.</typeparam>
public class ApplicationHost<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder builder, IHost host) : ApplicationHost(builder, host)
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public new THostApplicationBuilder Builder => (THostApplicationBuilder)base.Builder;
}
