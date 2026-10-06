using System;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Classifies executor cancellation with external abort taking precedence (outer + Ctrl+C + SIGTERM).</summary>
internal static class CommandExitMapper
{
    /// <summary>True when shutdown was requested via the outer token, Ctrl+C, or SIGTERM.</summary>
    internal static bool IsExternalAbort(CancellationToken shutdownToken, CancellationToken outerToken, bool ctrlCCanceled, bool sigtermCanceled = false)
    {
        return shutdownToken.IsCancellationRequested && (outerToken.IsCancellationRequested || ctrlCCanceled || sigtermCanceled);
    }

    /// <summary>Throws <see cref="CommandExecutor.ExternalCancellationException"/> on external abort.</summary>
    internal static void ThrowIfExternalAbort(CancellationToken shutdownToken, CancellationToken outerToken, bool ctrlCCanceled, bool sigtermCanceled = false)
    {
        if (IsExternalAbort(shutdownToken, outerToken, ctrlCCanceled, sigtermCanceled))
        {
            throw new CommandExecutor.ExternalCancellationException();
        }
    }
}
