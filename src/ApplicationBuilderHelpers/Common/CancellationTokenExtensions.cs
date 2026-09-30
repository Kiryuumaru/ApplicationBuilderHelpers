using ApplicationBuilderHelpers.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

/// <summary>
/// Links cancellation tokens with timeout and task-bridge helpers.
/// </summary>
internal static class CancellationTokenExtensions
{
    private static readonly ConditionalWeakTable<object, TokenSourceTracker> _trackers = [];

    private sealed class TokenSourceTracker(CancellationTokenSource timeoutSource, CancellationTokenSource linkedSource)
    {
        private readonly CancellationTokenSource _timeoutSource = timeoutSource;
        private readonly CancellationTokenSource _linkedSource = linkedSource;
        private CancellationTokenRegistration _registration;
        private int _disposed;

        public void SetRegistration(CancellationTokenRegistration registration)
        {
            _registration = registration;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                try
                {
                    _registration.Unregister();
                }
                catch { /* Suppress */ }

                try
                {
                    _timeoutSource?.Dispose();
                }
                catch { /* Suppress */ }

                try
                {
                    _linkedSource?.Dispose();
                }
                catch { /* Suppress */ }
            }
        }

        ~TokenSourceTracker()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Creates a linked token cancelled by the original token or the timeout.
    /// </summary>
    public static CancellationToken WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        var timeoutCts = new CancellationTokenSource(timeout);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var resultToken = linkedCts.Token;

        var tracker = new TokenSourceTracker(timeoutCts, linkedCts);

        var trackingKey = new object();
        _trackers.Add(trackingKey, tracker);

        try
        {
            var registration = resultToken.Register(state =>
            {
                var key = state!;
                if (_trackers.TryGetValue(key, out var trackerToDispose))
                {
                    _trackers.Remove(key);
                    trackerToDispose.Dispose();
                }
            }, trackingKey);

            tracker.SetRegistration(registration);
        }
        catch (ObjectDisposedException)
        {
            _trackers.Remove(trackingKey);
            tracker.Dispose();
            throw;
        }

        return resultToken;
    }

    /// <summary>
    /// Bridges cancellation to a task completing on cancel.
    /// </summary>
    public static Task WhenCanceled(this CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        CancellationTokenRegistration? reg = null;
        reg = cancellationToken.Register(s =>
        {
            tcs.TrySetResult(true);
            reg?.Unregister();
        }, tcs);
        return tcs.Task;
    }

    /// <summary>
    /// Runs <paramref name="onCancelled"/> once <paramref name="cancellationToken"/> cancels.
    /// </summary>
    public static void WhenCanceled(this CancellationToken cancellationToken, Func<Task> onCancelled)
    {
        Task.Run(async () =>
        {
            await cancellationToken.WhenCanceled();
            await onCancelled();
        }).Forget();
    }

    /// <summary>
    /// Runs <paramref name="onCancelled"/> once <paramref name="cancellationToken"/> cancels.
    /// </summary>
    public static void WhenCanceled(this CancellationToken cancellationToken, Action onCancelled)
    {
        Task.Run(async () =>
        {
            await cancellationToken.WhenCanceled();
            onCancelled();
        }).Forget();
    }
}
