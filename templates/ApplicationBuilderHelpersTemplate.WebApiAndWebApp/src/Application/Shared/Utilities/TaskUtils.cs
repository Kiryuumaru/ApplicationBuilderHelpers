using DisposableHelpers;

namespace Application.Shared.Utilities;

/// <summary>
/// Provides utility methods for task operations.
/// </summary>
public static class TaskUtils
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
        catch
        {
            // Intentionally empty: fire-and-forget delay should not propagate cancellation
        }
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

    public static Disposable DisposableTask(
        Func<CancellationToken, Task> task,
        Func<Exception, bool>? onDisposeError = null,
        Func<Exception, bool>? onTaskError = null)
    {
        CancellationTokenSource cts = new();
        Task? backgroundTask = null;

        Disposable disposable = new(async disposing =>
        {
            if (disposing)
            {
                cts.Cancel();

                // Wait for the background task to complete (with timeout)
                if (backgroundTask != null)
                {
                    try
                    {
                        await backgroundTask.WaitAsync(TimeSpan.FromMinutes(1));
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            onDisposeError?.Invoke(ex);
                        }
                        catch
                        {
                            // Intentionally empty: exceptions from error handler should not propagate
                        }
                    }
                }

                try
                {
                    cts.Dispose();
                }
                catch
                {
                    // Intentionally empty: dispose should not throw
                }
                try
                {
                    backgroundTask?.Dispose();
                }
                catch
                {
                    // Intentionally empty: dispose should not throw
                }
            }
        });

        backgroundTask = Task.Run(async () =>
        {
            try
            {
                await task(cts.Token);
            }
            catch (Exception ex)
            {
                try
                {
                    onTaskError?.Invoke(ex);
                }
                catch
                {
                    // Intentionally empty: exceptions from error handler should not propagate
                }
            }
        });

        return disposable;
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
