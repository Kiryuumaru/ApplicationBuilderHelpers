using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Executes the target command.
/// </summary>
internal sealed class CommandExecutor(
    IApplicationDependencyCollection applicationDependencyCollection,
    ConsoleOutput consoleOutput)
{
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

        using var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Ctrl+C requests cancellation alongside the passed token.
        bool ctrlCCanceled = false;
        LifetimeGlobalService? lifetimeGlobalService = null;

        ConsoleCancelEventHandler cancelKeyPressHandler = (sender, e) =>
        {
            ctrlCCanceled = true;
            shutdownCts.Cancel();
            e.Cancel = true;
        };

        bool cancelKeyPressSubscribed = false;
        try
        {
            consoleOutput.CancelKeyPress += cancelKeyPressHandler;
            cancelKeyPressSubscribed = true;
        }
        catch (PlatformNotSupportedException)
        {
            consoleOutput.WriteLineError("Note: Console.CancelKeyPress is not supported on this platform.");
        }
        catch (IOException)
        {
            consoleOutput.WriteLineError("Note: Console.CancelKeyPress is unavailable (I/O).");
        }

        try
        {
            var applicationBuilder = await command.ApplicationBuilderInternal(shutdownCts.Token);
            lifetimeGlobalService = new LifetimeGlobalService();
            shutdownCts.Token.Register(lifetimeGlobalService.CancellationTokenSource.Cancel);

            applicationBuilder.Services.AddSingleton(lifetimeGlobalService);
            applicationBuilder.Services.AddScoped<LifetimeService>();

            applicationBuilder.ApplicationDependencies.Add(command);
            foreach (var dependency in applicationDependencyCollection.ApplicationDependencies)
            {
                applicationBuilder.ApplicationDependencies.Add(dependency);
            }

            ApplicationHost applicationHost = applicationBuilder.BuildInternal();
            applicationHost.ConsoleOutput = consoleOutput;

            try
            {
                using var hostCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCts.Token);
                Task commandTask = command.RunInternal(applicationHost, shutdownCts.Token).AsTask();
                Task<int> hostTask = applicationHost.Run(hostCts.Token);

                Task finished = await Task.WhenAny(commandTask, hostTask);

                if (ReferenceEquals(finished, commandTask))
                {
                    // Command finished first: normal return = success, so stop the host.
                    if (commandTask.IsFaulted)
                    {
                        hostCts.Cancel();
                        try { await hostTask; } catch { /* Host outcome is irrelevant: the command failed. */ }
                        await commandTask;
                    }
                    else if (commandTask.IsCanceled)
                    {
                        hostCts.Cancel();
                        try { await hostTask; } catch { /* Host outcome is irrelevant: the command was canceled. */ }
                        ThrowIfExternalAbort(shutdownCts, cancellationToken, ctrlCCanceled);
                        await commandTask;
                    }
                    else
                    {
                        await commandTask;
                        hostCts.Cancel();
                        try { await hostTask; } catch (OperationCanceledException) { /* Expected: framework stopped the host after command success. */ }
                    }
                }
                else
                {
                    // Host stopped first (e.g. IHost.StopAsync): drain the command so a
                    // faulted/canceled command is still observed, then honor its outcome.
                    if (hostTask.IsFaulted)
                    {
                        try { await commandTask; } catch { /* Command outcome is irrelevant: the host failed. */ }
                        _ = await hostTask;
                    }
                    else if (hostTask.IsCanceled)
                    {
                        try { await commandTask; } catch { /* Command outcome is irrelevant: the host was canceled. */ }
                        _ = await hostTask;
                    }
                    else
                    {
                        int hostExitCode = await hostTask;
                        await commandTask;
                        if (hostExitCode != 0)
                        {
                            throw new CommandException($"Command '{commandInfo.FullCommandName}' exited with code {hostExitCode}", hostExitCode);
                        }
                    }
                }

                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
            }
            finally
            {
                // Ensure we clean up the lifetime service on both 0 and 130 paths.
                // Null-guarded: service may not exist on early-abort paths.
                await (lifetimeGlobalService?.InvokeApplicationExitedCallbacksAsync() ?? Task.CompletedTask);
            }
        }
        catch (OperationCanceledException ex) when (ex is not ExternalCancellationException && shutdownCts.IsCancellationRequested && (cancellationToken.IsCancellationRequested || ctrlCCanceled))
        {
            // Cancellation was requested via the passed token or Ctrl+C.
            throw new ExternalCancellationException();
        }
        finally
        {
            if (cancelKeyPressSubscribed)
            {
                consoleOutput.CancelKeyPress -= cancelKeyPressHandler;
            }
        }
    }

    /// <summary>
    /// Throws <see cref="ExternalCancellationException"/> when cancellation
    /// was requested via the passed token or Ctrl+C.
    /// </summary>
    private static void ThrowIfExternalAbort(CancellationTokenSource shutdownCts, CancellationToken outerToken, bool ctrlCCanceled)
    {
        if (shutdownCts.IsCancellationRequested && (outerToken.IsCancellationRequested || ctrlCCanceled))
        {
            throw new ExternalCancellationException();
        }
    }
}
