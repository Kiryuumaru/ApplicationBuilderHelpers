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

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Command<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : ApplicationDependency, ICommand
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Builds the application builder.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>An instance of <see cref="THostApplicationBuilder"/>.</returns>
    protected abstract ValueTask<THostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken);

    /// <summary>
    /// Runs the application.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="cancellationTokenSource">A token source to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource);

    void ICommand.CommandPreparationInternal(ApplicationBuilder applicationBuilder)
    {
        CommandPreparation(applicationBuilder);
    }

    /// <inheritdoc/>
    async ValueTask<ApplicationHostBuilder> ICommand.ApplicationBuilderInternal(CancellationToken stoppingToken)
    {
        var hostApplicationBuilder = await ApplicationBuilder(stoppingToken);
        return new ApplicationHostBuilder<THostApplicationBuilder>(hostApplicationBuilder);
    }

    /// <inheritdoc/>
    ValueTask ICommand.RunInternal(ApplicationHost applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        return Run((applicationHost as ApplicationHost<THostApplicationBuilder>)!, cancellationTokenSource);
    }
}

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
