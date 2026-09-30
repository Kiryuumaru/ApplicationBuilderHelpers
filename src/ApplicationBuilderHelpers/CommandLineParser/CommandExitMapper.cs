using System;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Classifies executor cancellation with external abort taking precedence.</summary>
internal static class CommandExitMapper
{
    /// <summary>True when shutdown was requested via the outer token or Ctrl+C.</summary>
    internal static bool IsExternalAbort(CancellationToken shutdownToken, CancellationToken outerToken, bool ctrlCCanceled)
    {
        return shutdownToken.IsCancellationRequested && (outerToken.IsCancellationRequested || ctrlCCanceled);
    }

    /// <summary>Throws <see cref="CommandExecutor.ExternalCancellationException"/> on external abort.</summary>
    internal static void ThrowIfExternalAbort(CancellationToken shutdownToken, CancellationToken outerToken, bool ctrlCCanceled)
    {
        if (IsExternalAbort(shutdownToken, outerToken, ctrlCCanceled))
        {
            throw new CommandExecutor.ExternalCancellationException();
        }
    }
}
