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
public abstract class ApplicationCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : ApplicationDependency, IApplicationCommand
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationCommand{THostApplicationBuilder}"/> class.
    /// </summary>
    /// <param name="description">The description of the command.</param>
    protected ApplicationCommand(string? description = null)
    {
        Name = null;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationCommand{THostApplicationBuilder}"/> class.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">The description of the command.</param>
    protected ApplicationCommand(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the name of the command.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the description of the command.
    /// </summary>
    public string? Description { get; }

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
    protected virtual ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        return new ValueTask();
    }

    void IApplicationCommand.CommandPreparationInternal(ApplicationBuilder applicationBuilder)
    {
        CommandPreparation(applicationBuilder);
    }

    /// <inheritdoc/>
    async ValueTask<ApplicationHostBuilder> IApplicationCommand.ApplicationBuilderInternal(CancellationToken stoppingToken)
    {
        var hostApplicationBuilder = await ApplicationBuilder(stoppingToken);
        return new ApplicationHostBuilder<THostApplicationBuilder>(hostApplicationBuilder);
    }

    /// <inheritdoc/>
    ValueTask IApplicationCommand.RunInternal(ApplicationHost applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        return Run((applicationHost as ApplicationHost<THostApplicationBuilder>)!, cancellationTokenSource);
    }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApplicationCommand : ApplicationCommand<HostApplicationBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationCommand"/> class.
    /// </summary>
    /// <param name="description">The description of the command.</param>
    protected ApplicationCommand(string? description = null)
        : base(description)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationCommand"/> class.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">The description of the command.</param>
    protected ApplicationCommand(string name, string? description = null)
        : base(name, description)
    {
    }

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
