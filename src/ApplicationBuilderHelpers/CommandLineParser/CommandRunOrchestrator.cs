using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Owns the joint command/host run and exactly-once <c>Exiting</c> fan-out.
/// Moved verbatim from the executor's race block (mechanical split, no behavior
/// change): command-wins faulted/canceled/success, host-wins faulted/canceled/
/// success-with-code, including the host-nonzero winner and the
/// host-won-with-success-but-command-canceled OCE rethrow.
/// Fault and canceled resolutions propagate as exceptions (identity and stack
/// preserved); only the success resolutions return a <see cref="CommandRunOutcome"/>
/// (<see cref="CommandRunOutcome.CommandSuccess"/> or
/// <see cref="CommandRunOutcome.HostSuccessWithCode"/>).
/// <see cref="LifetimeGlobalService"/>'s own exactly-once guards stay untouched as
/// the fail-safe; this orchestrator owns calling <c>Exiting</c> at the right sites.
/// </summary>
internal static class CommandRunOrchestrator
{
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
            // Command finished first: normal return = success, so stop the host.
            if (commandTask.IsFaulted)
            {
                hostCts.Cancel();
                try { await hostTask.ConfigureAwait(false); } catch { /* Host outcome is irrelevant: the command failed. */ }
                await commandTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted command task rethrows on await.");
            }
            else if (commandTask.IsCanceled)
            {
                hostCts.Cancel();
                try { await hostTask.ConfigureAwait(false); } catch { /* Host outcome is irrelevant: the command was canceled. */ }
                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                scope.ThrowIfExternalAbort();
                await commandTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the canceled command task rethrows on await.");
            }
            else
            {
                await commandTask.ConfigureAwait(false);
                hostCts.Cancel();
                try { await hostTask.ConfigureAwait(false); } catch (OperationCanceledException) { /* Expected: framework stopped the host after command success. */ }
                return CommandRunOutcome.CommandSuccess;
            }
        }
        else
        {
            // Host stopped first (e.g. IHost.StopAsync): drain the command so a
            // faulted/canceled command is still observed, then honor its outcome.
            if (hostTask.IsFaulted)
            {
                try { await commandTask.ConfigureAwait(false); } catch { /* Command outcome is irrelevant: the host failed. */ }
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted host task rethrows on await.");
            }
            else if (hostTask.IsCanceled)
            {
                try { await commandTask.ConfigureAwait(false); } catch { /* Command outcome is irrelevant: the host was canceled. */ }
                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync().ConfigureAwait(false);
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the canceled host task rethrows on await.");
            }
            else
            {
                int hostExitCode = await hostTask.ConfigureAwait(false);
                try { await commandTask.ConfigureAwait(false); }
                catch (OperationCanceledException) // Host won with success but command canceled: run Exiting once (Lifetime Callbacks contract); OCE-only so faults still run only Exited.
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
