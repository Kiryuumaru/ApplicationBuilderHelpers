using System;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Executor exit-classification for cancellation (cancellation takes precedence).
/// Cancellation observed with the outer token or Ctrl+C before host completion maps to
/// <see cref="CommandExecutor.CanceledExitCode"/> (130); internal-only cooperative
/// cancellation stays success (0). Single location; replaces the two classification blocks.
/// </summary>
/// <remarks>
/// 130 follows the Unix 128 + SIGINT convention. Windows has no SIGINT exit-code
/// convention (a Ctrl+C kill tears the process down at OS level with its own status),
/// so 130 is the library-level cancellation mapping on all platforms.
/// </remarks>
internal static class CommandExitMapper
{
    /// <summary>
    /// Returns <c>true</c> when an observed <see cref="OperationCanceledException"/>
    /// must map to <see cref="CommandExecutor.ExternalCancellationException"/>:
    /// shutdown was requested and the request came from the outer token or Ctrl+C.
    /// </summary>
    internal static bool IsExternalAbort(CancellationToken shutdownToken, CancellationToken outerToken, bool ctrlCCanceled)
    {
        return shutdownToken.IsCancellationRequested && (outerToken.IsCancellationRequested || ctrlCCanceled);
    }

    /// <summary>
    /// Throws <see cref="CommandExecutor.ExternalCancellationException"/> when
    /// cancellation was requested with the passed token or Ctrl+C.
    /// </summary>
    internal static void ThrowIfExternalAbort(CancellationToken shutdownToken, CancellationToken outerToken, bool ctrlCCanceled)
    {
        if (IsExternalAbort(shutdownToken, outerToken, ctrlCCanceled))
        {
            throw new CommandExecutor.ExternalCancellationException();
        }
    }
}
