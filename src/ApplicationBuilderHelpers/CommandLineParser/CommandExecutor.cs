using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Runs the bound target command under a shutdown scope.</summary>
internal sealed class CommandExecutor
{
    private readonly IApplicationDependencyCollection _applicationDependencyCollection;
    private readonly ConsoleOutput _consoleOutput;
    private readonly IConsoleCancelSignal _cancelSignal;

    /// <summary>Wires the dependency collection, output, and cancel signal.</summary>
    internal CommandExecutor(
        IApplicationDependencyCollection applicationDependencyCollection,
        ConsoleOutput consoleOutput,
        IConsoleCancelSignal? cancelSignal = null)
    {
        _applicationDependencyCollection = applicationDependencyCollection;
        _consoleOutput = consoleOutput;
        _cancelSignal = cancelSignal ?? new ConsoleCancelSignal(consoleOutput);
    }

    /// <summary>Exit code for cancellation (128 + SIGINT).</summary>
    internal const int CanceledExitCode = 130;

    /// <summary>Marks external abort so the catch chain can tell it from internal cancel.</summary>
    internal sealed class ExternalCancellationException : OperationCanceledException
    {
        public ExternalCancellationException()
            : base("The command was canceled.")
        {
        }
    }

    /// <summary>Executes the bound target command; normal return means success.</summary>
    /// <param name="commandInfo">The resolved target command.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
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
            // External abort maps to exit 130; caller observes ExternalCancellation, not OCE.
            throw new ExternalCancellationException();
        }
    }
}
