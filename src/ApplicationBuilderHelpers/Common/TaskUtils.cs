using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

internal static class TaskUtils
{
    public static async Task DelayAndForget(int milliseconds, CancellationToken cancellationToken = default)
    {
        await DelayAndForget(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);
    }

    public static async Task DelayAndForget(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(timeSpan, cancellationToken);
        }
        catch { }
    }

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

    public static Task WaitThread(this Task task)
    {
        return ThreadHelpers.WaitThread(() => task);
    }

    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        Action<(Exception Error, int Attempts)>? onRetry = null,
        TimeSpan? retryDelay = default,
        int maxRetries = -1,
        CancellationToken cancellationToken = default)
    {
        return await RetryInternalAsync(action, onRetry, retryDelay, maxRetries, cancellationToken);
    }

    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        Func<(Exception Error, int Attempts), Task>? onRetry = null,
        TimeSpan? retryDelay = default,
        int maxRetries = -1,
        CancellationToken cancellationToken = default)
    {
        return await RetryInternalAsync(action, onRetry, retryDelay, maxRetries, cancellationToken);
    }

    public static async Task RetryAsync(
        Func<Task> action,
        Action<(Exception Error, int Attempts)>? onRetry = null,
        TimeSpan? retryDelay = default,
        int maxRetries = -1,
        CancellationToken cancellationToken = default)
    {
        await RetryAsync<object?>(async () => { await action(); return null; }, onRetry, retryDelay, maxRetries, cancellationToken);
    }

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

                // Handle both sync and async onRetry callbacks
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
