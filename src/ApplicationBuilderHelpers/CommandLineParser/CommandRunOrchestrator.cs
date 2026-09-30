using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Runs the command/host race and resolves the winner.</summary>
internal static class CommandRunOrchestrator
{
    /// <summary>Runs the command/host race; faults and cancels propagate, successes return an outcome.</summary>
    internal static async Task<CommandRunOutcome> InvokeAsync(
        ICommand command,
        ApplicationHost applicationHost,
        LifetimeGlobalService lifetimeGlobalService,
        CommandShutdownScope scope,
        SubCommandInfo commandInfo)
    {
        using var hostCts = CancellationTokenSource.CreateLinkedTokenSource(scope.Token);
        Task commandTask = command.RunInternal(applicationHost, scope.Token).AsTask();
        Task<int> hostTask = applicationHost.Run(hostCts.Token);

        Task finished = await Task.WhenAny(commandTask, hostTask).ConfigureAwait(false);

        if (ReferenceEquals(finished, commandTask))
        {
            if (commandTask.IsFaulted)
            {
                hostCts.Cancel();
                // Settled sibling observed after WhenAny; drained so no task is left unobserved.
                try { await hostTask.ConfigureAwait(false); } catch { }
                await commandTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted command task rethrows on await.");
            }
            else if (commandTask.IsCanceled)
            {
                hostCts.Cancel();
                // Settled sibling observed after WhenAny; drained so no task is left unobserved.
                try { await hostTask.ConfigureAwait(false); } catch { }
                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                scope.ThrowIfExternalAbort();
                await commandTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the canceled command task rethrows on await.");
            }
            else
            {
                await commandTask.ConfigureAwait(false);
                hostCts.Cancel();
                // Host shutdown after command success always cancels the host task, so its OCE is expected.
                try { await hostTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
                return CommandRunOutcome.CommandSuccess;
            }
        }
        else
        {
            if (hostTask.IsFaulted)
            {
                // Settled sibling observed after WhenAny; drained so no task is left unobserved.
                try { await commandTask.ConfigureAwait(false); } catch { }
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted host task rethrows on await.");
            }
            else if (hostTask.IsCanceled)
            {
                // Settled sibling observed after WhenAny; drained so no task is left unobserved.
                try { await commandTask.ConfigureAwait(false); } catch { }
                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the canceled host task rethrows on await.");
            }
            else
            {
                int hostExitCode = await hostTask.ConfigureAwait(false);
                try { await commandTask.ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                    throw;
                }
                if (hostExitCode != 0)
                {
                    throw new CommandException($"Command '{commandInfo.FullCommandName}' exited with code {hostExitCode}", hostExitCode);
                }

                return CommandRunOutcome.HostSuccessWithCode;
            }
        }
    }
}
