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
/// Moved verbatim from CommandLineParser (mechanical split, no behavior change).
/// </summary>
internal sealed class CommandExecutor(
    IApplicationDependencyCollection applicationDependencyCollection,
    ConsoleOutput consoleOutput)
{
    /// <summary>
    /// Executes the target command
    /// </summary>
    public async Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken)
    {
        var command = commandInfo.Command!;

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        ConsoleCancelEventHandler cancelKeyPressHandler = (sender, e) =>
        {
            cancellationTokenSource.Cancel();
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
            var applicationBuilder = await command.ApplicationBuilderInternal(cancellationTokenSource.Token);
            LifetimeGlobalService lifetimeGlobalService = new();
            cancellationTokenSource.Token.Register(lifetimeGlobalService.CancellationTokenSource.Cancel);

            applicationBuilder.Services.AddSingleton(lifetimeGlobalService);
            applicationBuilder.Services.AddScoped<LifetimeService>();

            applicationBuilder.ApplicationDependencies.Add(command);
            foreach (var dependency in applicationDependencyCollection.ApplicationDependencies)
            {
                applicationBuilder.ApplicationDependencies.Add(dependency);
            }

            ApplicationHost applicationHost = applicationBuilder.BuildInternal();
            applicationHost.ConsoleOutput = consoleOutput;
            ValueTask commandRun = command.RunInternal(applicationHost, cancellationTokenSource);

            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
                await lifetimeGlobalService.InvokeApplicationExitedCallbacksAsync();
                return;
            }

            try
            {
                using var runtimeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await commandRun;
                        }
                        catch
                        {
                            runtimeCts.Cancel();
                            throw;
                        }

                    }, runtimeCts.Token),
                    Task.Run(async () =>
                    {
                        try
                        {
                            int exitCode = await applicationHost.Run(runtimeCts.Token);
                            if (exitCode != 0)
                            {
                                throw new CommandException($"Command '{commandInfo.FullCommandName}' exited with code {exitCode}", exitCode);
                            }
                        }
                        catch
                        {
                            runtimeCts.Cancel();
                            throw;
                        }
                    }, runtimeCts.Token));

                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    throw; // Rethrow if cancellation was not requested by the caller
                }
            }
            finally
            {
                // Ensure we clean up the lifetime service
                await lifetimeGlobalService.InvokeApplicationExitedCallbacksAsync();
            }
        }
        finally
        {
            if (cancelKeyPressSubscribed)
            {
                consoleOutput.CancelKeyPress -= cancelKeyPressHandler;
            }
        }
    }
}
