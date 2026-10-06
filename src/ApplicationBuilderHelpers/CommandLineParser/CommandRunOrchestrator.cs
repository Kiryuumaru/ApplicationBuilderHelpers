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
    /// <summary>Bounded host-wins drain: the host result wins, the command task gets at most 5s (up-to-10s-on-cancel-race, 5s per phase). Host-wins only: command-first paths already observe a settled sibling, so only the host-wins paths bound the drain.</summary>
    private const int DrainTimeoutMilliseconds = 5000;
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
                try { await DrainAsync(commandTask, scope.Token).ConfigureAwait(false); } catch { }
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted host task rethrows on await.");
            }
            else if (hostTask.IsCanceled)
            {
                try { await DrainAsync(commandTask, scope.Token).ConfigureAwait(false); } catch { }
                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                scope.ThrowIfExternalAbort();
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the canceled host task rethrows on await.");
            }
            else
            {
                int hostExitCode = await hostTask.ConfigureAwait(false);
                try { await DrainAsync(commandTask, scope.Token).ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                    scope.ThrowIfExternalAbort();
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

    private static async Task DrainAsync(Task task, CancellationToken scopeToken)
    {
        // Linked to the scope token so an already-canceled scope skips the full 5s wait;
        // the second bound still caps the drain when the linked delay fires first.
        Task finished = await Task.WhenAny(task, Task.Delay(DrainTimeoutMilliseconds, scopeToken)).ConfigureAwait(false);
        if (!ReferenceEquals(finished, task) && scopeToken.IsCancellationRequested && !task.IsCompleted)
        {
            finished = await Task.WhenAny(task, Task.Delay(DrainTimeoutMilliseconds)).ConfigureAwait(false);
        }

        if (ReferenceEquals(finished, task))
        {
            await task.ConfigureAwait(false);
        }
        else
        {
            // Abandoned: the host result wins and the command task keeps running past
            // the bound. Observe its fault so a later failure cannot go unobserved.
            _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
