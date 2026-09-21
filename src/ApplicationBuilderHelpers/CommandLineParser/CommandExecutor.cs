using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Executes the target command.
/// Thin sequencer over collaborators (mechanical split, no behavior change):
/// <see cref="CommandShutdownScope"/> (linked CTS + Ctrl+C subscribe/dispose),
/// <see cref="CommandRunOrchestrator"/> (joint command/host run + Exiting fan-out,
/// returning a <see cref="CommandRunOutcome"/>), <see cref="CommandExitMapper"/>
/// (single cancel-wins classification point), and <see cref="ServiceInjectionGate"/>
/// (per-command service injection).
/// </summary>
internal sealed class CommandExecutor
{
    private readonly IApplicationDependencyCollection _applicationDependencyCollection;
    private readonly ConsoleOutput _consoleOutput;
    private readonly IConsoleCancelSignal _cancelSignal;

    internal CommandExecutor(
        IApplicationDependencyCollection applicationDependencyCollection,
        ConsoleOutput consoleOutput,
        IConsoleCancelSignal? cancelSignal = null)
    {
        _applicationDependencyCollection = applicationDependencyCollection;
        _consoleOutput = consoleOutput;
        _cancelSignal = cancelSignal ?? new ConsoleCancelSignal(consoleOutput);
    }

    /// <summary>
    /// Exit code returned when execution is canceled.
    /// Follows the 128 + SIGINT convention.
    /// </summary>
    internal const int CanceledExitCode = 130;

    /// <summary>
    /// Marker for a cancellation request.
    /// Always maps to <see cref="CanceledExitCode"/> (130).
    /// </summary>
    internal sealed class ExternalCancellationException : OperationCanceledException
    {
        public ExternalCancellationException()
            : base("The command was canceled.")
        {
        }
    }

    /// <summary>
    /// Executes the target command. A normal return means success:
    /// the host is stopped after the command returns.
    /// Cancellation token for cooperative cancellation.
    /// </summary>
    public async Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken)
    {
        var command = commandInfo.Command!;

        using var scope = new CommandShutdownScope(cancellationToken, _cancelSignal);

        LifetimeGlobalService? lifetimeGlobalService = null;

        try
        {
            var applicationBuilder = await command.ApplicationBuilderInternal(scope.Token).ConfigureAwait(false);
            lifetimeGlobalService = new LifetimeGlobalService();
            scope.Token.Register(lifetimeGlobalService.CancellationTokenSource.Cancel);

            applicationBuilder.Services.AddSingleton(lifetimeGlobalService);
            applicationBuilder.Services.AddScoped<LifetimeService>();

            applicationBuilder.ApplicationDependencies.Add(command);
            foreach (var dependency in _applicationDependencyCollection.ApplicationDependencies)
            {
                applicationBuilder.ApplicationDependencies.Add(dependency);
            }

            ApplicationHost applicationHost = applicationBuilder.BuildInternal();
            applicationHost.ConsoleOutput = _consoleOutput;

            var scopeFactory = applicationHost.Services.GetRequiredService<IServiceScopeFactory>();
            using var commandScope = scopeFactory.CreateScope();
            ServiceInjectionGate.Inject(commandInfo, commandScope.ServiceProvider);

            try
            {
                _ = await CommandRunOrchestrator.InvokeAsync(command, applicationHost, lifetimeGlobalService, scope, commandInfo).ConfigureAwait(false);

                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
            }
            finally
            {
                // Ensure we clean up the lifetime service on both 0 and 130 paths.
                // Null-guarded: service may not exist on early-abort paths.
                await (lifetimeGlobalService?.InvokeApplicationExitedCallbacksAsync() ?? Task.CompletedTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (ex is not ExternalCancellationException && scope.IsExternalAbortRequested)
        {
            // Cancellation was requested via the passed token or Ctrl+C.
            throw new ExternalCancellationException();
        }
    }
}
