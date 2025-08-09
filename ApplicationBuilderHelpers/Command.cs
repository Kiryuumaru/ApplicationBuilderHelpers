using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using ApplicationBuilderHelpers.Interfaces;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Provides a base implementation for commands that can be executed within the application with a specific host application builder type.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The type of host application builder used by this command.</typeparam>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Command<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : ApplicationDependency, ICommand
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Builds the application builder.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>An instance of the host application builder type.</returns>
    protected abstract ValueTask<THostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken);

    /// <summary>
    /// Runs the application.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="cancellationTokenSource">A token source to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Internal method for command preparation.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    void ICommand.CommandPreparationInternal(ApplicationBuilder applicationBuilder)
    {
        CommandPreparation(applicationBuilder);
    }

    /// <summary>
    /// Internal method for building the application.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>An instance of <see cref="ApplicationHostBuilder"/>.</returns>
    async ValueTask<ApplicationHostBuilder> ICommand.ApplicationBuilderInternal(CancellationToken stoppingToken)
    {
        var hostApplicationBuilder = await ApplicationBuilder(stoppingToken);
        return new ApplicationHostBuilder<THostApplicationBuilder>(hostApplicationBuilder);
    }

    /// <summary>
    /// Internal method for running the application.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="cancellationTokenSource">A token source to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ICommand.RunInternal(ApplicationHost applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        return Run((applicationHost as ApplicationHost<THostApplicationBuilder>)!, cancellationTokenSource);
    }
}

/// <summary>
/// Provides a base implementation for commands that can be executed within the application using the default <see cref="HostApplicationBuilder"/>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Command : Command<HostApplicationBuilder>
{
    /// <summary>
    /// Builds the application builder.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>An instance of <see cref="HostApplicationBuilder"/>.</returns>
    protected override ValueTask<HostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        return new ValueTask<HostApplicationBuilder>(Host.CreateApplicationBuilder());
    }
}
