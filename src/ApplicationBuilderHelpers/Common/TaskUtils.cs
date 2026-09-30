using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

/// <summary>Fire-and-forget helpers plus cooperative retry for setup work.</summary>
internal static class TaskUtils
{
    /// <summary>Delays without surfacing faults; swallows all exceptions.</summary>
    /// <param name="milliseconds">Delay duration in milliseconds.</param>
    /// <param name="cancellationToken">Early wake-up; still swallowed, never thrown.</param>
    /// <returns>A task completing after the delay or its cancellation.</returns>
    public static async Task DelayAndForget(int milliseconds, CancellationToken cancellationToken = default)
    {
        await DelayAndForget(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);
    }

    /// <summary>Delays without surfacing faults; swallows all exceptions.</summary>
    /// <param name="timeSpan">Delay duration.</param>
    /// <param name="cancellationToken">Early wake-up; still swallowed, never thrown.</param>
    /// <returns>A task completing after the delay or its cancellation.</returns>
    public static async Task DelayAndForget(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(timeSpan, cancellationToken);
        }
        catch { }
    }

    /// <summary>Observes <paramref name="task"/> so a fault never escapes unobserved.</summary>
    /// <param name="task">Task whose fault to swallow.</param>
    public static void Forget(this Task task)
    {
        if (!task.IsCompleted || task.IsFaulted)
        {
            _ = ForgetAwaited(task);
        }

        async static Task ForgetAwaited(Task task)
        {
            try
            {
                await task;
            }
            catch { }
        }
    }

    /// <summary>Runs <paramref name="task"/> on a background thread via <c>ThreadHelpers</c>.</summary>
    /// <param name="task">Task whose delegate to re-run off the caller thread.</param>
    /// <returns>A task completing when the background run finishes.</returns>
    public static Task WaitThread(this Task task)
    {
        return ThreadHelpers.WaitThread(() => task);
    }

    /// <summary>Retries <paramref name="action"/> until success, reporting failures through sync <paramref name="onRetry"/>.</summary>
    /// <param name="action">Fallible work returning a value.</param>
    /// <param name="onRetry">Observer receiving each error plus its attempt number.</param>
    /// <param name="retryDelay">Delay between attempts; defaults to 2 seconds.</param>
    /// <param name="maxRetries">Maximum retries; negative means unbounded.</param>
    /// <param name="cancellationToken">Stops retrying by throwing.</param>
    /// <returns>The first successful result.</returns>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        Action<(Exception Error, int Attempts)>? onRetry = null,
        TimeSpan? retryDelay = default,
        int maxRetries = -1,
        CancellationToken cancellationToken = default)
    {
        return await RetryInternalAsync(action, onRetry, retryDelay, maxRetries, cancellationToken);
    }

    /// <summary>Retries <paramref name="action"/> until success, reporting failures through async <paramref name="onRetry"/>.</summary>
    /// <param name="action">Fallible work returning a value.</param>
    /// <param name="onRetry">Observer receiving each error plus its attempt number.</param>
    /// <param name="retryDelay">Delay between attempts; defaults to 2 seconds.</param>
    /// <param name="maxRetries">Maximum retries; negative means unbounded.</param>
    /// <param name="cancellationToken">Stops retrying by throwing.</param>
    /// <returns>The first successful result.</returns>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        Func<(Exception Error, int Attempts), Task>? onRetry = null,
        TimeSpan? retryDelay = default,
        int maxRetries = -1,
        CancellationToken cancellationToken = default)
    {
        return await RetryInternalAsync(action, onRetry, retryDelay, maxRetries, cancellationToken);
    }

    /// <summary>Retries void <paramref name="action"/> until success, reporting failures through sync <paramref name="onRetry"/>.</summary>
    /// <param name="action">Fallible work with no return value.</param>
    /// <param name="onRetry">Observer receiving each error plus its attempt number.</param>
    /// <param name="retryDelay">Delay between attempts; defaults to 2 seconds.</param>
    /// <param name="maxRetries">Maximum retries; negative means unbounded.</param>
    /// <param name="cancellationToken">Stops retrying by throwing.</param>
    /// <returns>A task completing on the first success.</returns>
    public static async Task RetryAsync(
        Func<Task> action,
        Action<(Exception Error, int Attempts)>? onRetry = null,
        TimeSpan? retryDelay = default,
        int maxRetries = -1,
        CancellationToken cancellationToken = default)
    {
        await RetryAsync<object?>(async () => { await action(); return null; }, onRetry, retryDelay, maxRetries, cancellationToken);
    }

    /// <summary>Shared retry loop dispatching the sync/async observer.</summary>
    /// <param name="action">Fallible work returning a value.</param>
    /// <param name="onRetry">Sync or async observer (or null).</param>
    /// <param name="retryDelay">Delay between attempts; null means 2 seconds.</param>
    /// <param name="maxRetries">Maximum retries; negative means unbounded.</param>
    /// <param name="cancellationToken">Checked before each attempt and during the delay.</param>
    /// <returns>The first successful result.</returns>
    private static async Task<T> RetryInternalAsync<T>(
        Func<Task<T>> action,
        object? onRetry,
        TimeSpan? retryDelay,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        retryDelay ??= TimeSpan.FromSeconds(2);
        int attempt = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await action();
            }
            catch (Exception ex) when (maxRetries < 0 || attempt < maxRetries)
            {
                attempt++;

                switch (onRetry)
                {
                    case Action<(Exception, int)> syncCallback:
                        syncCallback((ex, attempt));
                        break;
                    case Func<(Exception, int), Task> asyncCallback:
                        await asyncCallback((ex, attempt));
                        break;
                }

                await Task.Delay(retryDelay.Value, cancellationToken);
            }
        }
    }
}
