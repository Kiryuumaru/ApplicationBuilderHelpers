using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Threading;
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
    /// Runs the application. A normal return signals success (exit code 0);
    /// throw <see cref="Exceptions.CommandException"/> for a non-zero exit code.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
#pragma warning disable CS0618 // Obsolete overload remains the dispatch target here.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await Run(applicationHost, linkedCts).ConfigureAwait(false);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Runs the application. A normal return signals success (exit code 0);
    /// throw <see cref="Exceptions.CommandException"/> for a non-zero exit code.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="cancellationTokenSource">A token source to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Obsolete("Override Run(ApplicationHost<THostApplicationBuilder>, CancellationToken) instead and return on success.")]
    protected virtual ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException($"Override {nameof(Run)}({nameof(ApplicationHost<THostApplicationBuilder>)}, {nameof(CancellationToken)}) instead.");
    }

    /// <summary>
    /// Explicit <see cref="ICommand"/> preparation member.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    void ICommand.CommandPreparationInternal(ApplicationBuilder applicationBuilder)
    {
        CommandPreparation(applicationBuilder);
    }

    /// <summary>
    /// Explicit <see cref="ICommand"/> builder member.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>An instance of <see cref="ApplicationHostBuilder"/>.</returns>
    async ValueTask<ApplicationHostBuilder> ICommand.ApplicationBuilderInternal(CancellationToken stoppingToken)
    {
        var hostApplicationBuilder = await ApplicationBuilder(stoppingToken);
        return new ApplicationHostBuilder<THostApplicationBuilder>(hostApplicationBuilder);
    }

    /// <summary>
    /// Explicit <see cref="ICommand"/> run member.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    async ValueTask ICommand.RunInternal(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        var typedHost = (applicationHost as ApplicationHost<THostApplicationBuilder>)!;
        await Run(typedHost, cancellationToken).ConfigureAwait(false);
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
