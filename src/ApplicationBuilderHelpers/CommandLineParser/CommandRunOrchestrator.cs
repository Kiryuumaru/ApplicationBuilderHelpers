using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Runs the command task and the host task; invokes <c>Exiting</c> once.
/// Command-finished faulted/canceled/success, host-finished faulted/canceled/
/// success-with-code, including the host-nonzero result and the
/// host-finished-with-success-but-command-canceled OCE rethrow.
/// Fault and canceled resolutions propagate as exceptions (identity and stack
/// preserved); only the success resolutions return a <see cref="CommandRunOutcome"/>
/// (<see cref="CommandRunOutcome.CommandSuccess"/> or
/// <see cref="CommandRunOutcome.HostSuccessWithCode"/>).
/// <see cref="LifetimeGlobalService"/>'s own exactly-once guards stay untouched as
/// the backup; this class calls <c>Exiting</c> at the matching completion branches.
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
            if (commandTask.IsFaulted)
            {
                hostCts.Cancel();
                try { await hostTask.ConfigureAwait(false); } catch { }
                await commandTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted command task rethrows on await.");
            }
            else if (commandTask.IsCanceled)
            {
                hostCts.Cancel();
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
                try { await hostTask.ConfigureAwait(false); } catch (OperationCanceledException) {  }
                return CommandRunOutcome.CommandSuccess;
            }
        }
        else
        {
            if (hostTask.IsFaulted)
            {
                try { await commandTask.ConfigureAwait(false); } catch { }
                _ = await hostTask.ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the faulted host task rethrows on await.");
            }
            else if (hostTask.IsCanceled)
            {
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
