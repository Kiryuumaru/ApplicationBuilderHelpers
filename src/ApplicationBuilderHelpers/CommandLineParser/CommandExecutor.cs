using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Executes the target command.
/// Calls:
/// <see cref="CommandShutdownScope"/> (linked CTS + Ctrl+C subscribe/dispose),
/// <see cref="CommandRunOrchestrator"/> (command task and host task + Exiting callbacks run exactly once,
/// returning a <see cref="CommandRunOutcome"/>), <see cref="CommandExitMapper"/>
/// (single classification point where cancellation takes precedence), and <see cref="ServiceInjectionGate"/>
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
                await (lifetimeGlobalService?.InvokeApplicationExitedCallbacksAsync() ?? Task.CompletedTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (ex is not ExternalCancellationException && scope.IsExternalAbortRequested)
        {
            throw new ExternalCancellationException();
        }
    }
}
