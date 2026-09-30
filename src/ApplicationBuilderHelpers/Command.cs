using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using ApplicationBuilderHelpers.Interfaces;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Base command bound to one host-builder type.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The host-builder type this command constructs per run.</typeparam>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Command<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : ApplicationDependency, ICommand
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Builds the host application builder for this run.
    /// </summary>
    /// <param name="stoppingToken">Token observing host shutdown during the build.</param>
    /// <returns>The host application builder this run executes against.</returns>
    protected abstract ValueTask<THostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken);

    /// <summary>
    /// Runs the command work; a normal return signals success (exit 0).
    /// </summary>
    /// <param name="applicationHost">The typed host carrying the built <c>IHost</c> and services.</param>
    /// <param name="cancellationToken">Token observing cooperative cancellation (exit 130).</param>
    /// <returns>A task completing when the command work finishes.</returns>
    protected virtual async ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
#pragma warning disable CS0618 // Obsolete overload remains the dispatch target here.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await Run(applicationHost, linkedCts).ConfigureAwait(false);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Forwards to the <see cref="CancellationToken"/> overload; override that overload instead.
    /// </summary>
    /// <param name="applicationHost">The typed host carrying the built <c>IHost</c> and services.</param>
    /// <param name="cancellationTokenSource">Source linked to the run token.</param>
    /// <returns>A task completing when the command work finishes.</returns>
    /// <exception cref="NotImplementedException">Thrown by default; override the <see cref="CancellationToken"/> overload instead.</exception>
    [Obsolete("Override Run(ApplicationHost<THostApplicationBuilder>, CancellationToken) instead and return on success.")]
    protected virtual ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException($"Override {nameof(Run)}({nameof(ApplicationHost<THostApplicationBuilder>)}, {nameof(CancellationToken)}) instead.");
    }

    void ICommand.CommandPreparationInternal(ApplicationBuilder applicationBuilder)
    {
        CommandPreparation(applicationBuilder);
    }

    async ValueTask<ApplicationHostBuilder> ICommand.ApplicationBuilderInternal(CancellationToken stoppingToken)
    {
        var hostApplicationBuilder = await ApplicationBuilder(stoppingToken);
        return new ApplicationHostBuilder<THostApplicationBuilder>(hostApplicationBuilder);
    }

    async ValueTask ICommand.RunInternal(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        var typedHost = (applicationHost as ApplicationHost<THostApplicationBuilder>)!;
        await Run(typedHost, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Default command bound to <c>HostApplicationBuilder</c>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Command : Command<HostApplicationBuilder>
{
    /// <summary>
    /// Builds the default host builder via <c>Host.CreateApplicationBuilder</c>.
    /// </summary>
    /// <param name="stoppingToken">Token observing host shutdown during the build.</param>
    /// <returns>The default host application builder.</returns>
    protected override ValueTask<HostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        return new ValueTask<HostApplicationBuilder>(Host.CreateApplicationBuilder());
    }
}
