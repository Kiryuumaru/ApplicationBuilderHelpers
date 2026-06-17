using DisposableHelpers;

namespace Application.Shared.Utilities;

public static class TaskUtils
{
    private static readonly TimeSpan DisposalTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

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
        catch { /* Intentional fire-and-forget; cancellation is expected */ }
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
                        await backgroundTask.WaitAsync(DisposalTimeout);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            onDisposeError?.Invoke(ex);
                        }
                        catch
                        {
                            // Ignore exceptions from error handler
                        }
                    }
                }

                try
                {
                    cts.Dispose();
                }
                catch { /* Best-effort cleanup; already cancelled */ }
                try
                {
                    backgroundTask?.Dispose();
                }
                catch { /* Best-effort cleanup; task may already be disposed */ }
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
                    // Ignore exceptions from error handler
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

        retryDelay ??= DefaultRetryDelay;
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
