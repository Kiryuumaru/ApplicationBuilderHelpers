using ApplicationBuilderHelpers.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

internal static class CancellationTokenExtensions
{
    // ConditionalWeakTable automatically removes entries when keys are GC'd
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
                // Unregister first to prevent callback from firing during disposal
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
            // Finalizer ensures disposal even if token is never cancelled
            Dispose();
        }
    }

    /// <summary>
    /// Creates a linked cancellation token that will be cancelled when either the original token 
    /// is cancelled or the specified timeout elapses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates CancellationTokenSource instances that are automatically disposed when:
    /// 1. The returned token is cancelled (by timeout or original token cancellation)
    /// 2. The returned token is garbage collected without being cancelled (via finalizer)
    /// </para>
    /// <para>
    /// The implementation uses ConditionalWeakTable for automatic cleanup and finalizers as a 
    /// safety net. For critical long-running operations, consider manually managing 
    /// CancellationTokenSource lifetime instead.
    /// </para>
    /// </remarks>
    public static CancellationToken WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        var timeoutCts = new CancellationTokenSource(timeout);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var resultToken = linkedCts.Token;

        // Create a tracker object that will be associated with the token
        var tracker = new TokenSourceTracker(timeoutCts, linkedCts);
        
        // Use a separate tracking key object to avoid struct equality issues
        var trackingKey = new object();
        _trackers.Add(trackingKey, tracker);

        try
        {
            // Register disposal callback - this keeps trackingKey alive until cancellation
            var registration = resultToken.Register(state =>
            {
                var key = state!;
                if (_trackers.TryGetValue(key, out var trackerToDispose))
                {
                    _trackers.Remove(key);
                    trackerToDispose.Dispose();
                }
            }, trackingKey);

            // Store the registration in the tracker so it can be properly unregistered
            tracker.SetRegistration(registration);
        }
        catch (ObjectDisposedException)
        {
            // Token was already disposed, clean up immediately
            _trackers.Remove(trackingKey);
            tracker.Dispose();
            throw;
        }

        return resultToken;
    }

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

    public static void WhenCanceled(this CancellationToken cancellationToken, Func<Task> onCancelled)
    {
        Task.Run(async () =>
        {
            await cancellationToken.WhenCanceled();
            await onCancelled();
        }).Forget();
    }

    public static void WhenCanceled(this CancellationToken cancellationToken, Action onCancelled)
    {
        Task.Run(async () =>
        {
            await cancellationToken.WhenCanceled();
            onCancelled();
        }).Forget();
    }
}
