namespace Application.Shared.Utilities;

public static class ThreadHelpers
{
    public static Task WaitThread(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(action, cancellationToken);
    }

    public static Task WaitThread(Func<Task> task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(task, cancellationToken);
    }

    public static async ValueTask<bool> WaitAsync(this WaitHandle waitHandle, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (waitHandle.WaitOne(0))
        {
            return true;
        }

        var tcs = new TaskCompletionSource();

        var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            waitObject: waitHandle,
            callBack: (o, timeout) => tcs.TrySetResult(),
            state: null,
            millisecondsTimeOutInterval: -1,
            executeOnlyOnce: true);

        bool cancelled = false;
        CancellationTokenRegistration? cancellationTokenRegistration = null;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                cancelled = true;
                tcs.TrySetCanceled();
                registeredWaitHandle.Unregister(null);
                cancellationTokenRegistration?.Unregister();
            });
        }

        try
        {
            await tcs.Task.ConfigureAwait(false);
        }
        catch
        {
            if (cancelled)
            {
                return false;
            }
            throw;
        }
        finally
        {
            registeredWaitHandle.Unregister(null);
            cancellationTokenRegistration?.Unregister();
        }

        return true;
    }

    public static async ValueTask<bool> WaitAsync(this WaitHandle waitHandle, int millisecondsTimeout)
    {
        if (waitHandle.WaitOne(0))
        {
            return true;
        }

        if (millisecondsTimeout == 0)
        {
            return false;
        }

        var tcs = new TaskCompletionSource();
        using var cts = new CancellationTokenSource(millisecondsTimeout);

        var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            waitObject: waitHandle,
            callBack: (o, timeout) => tcs.TrySetResult(),
            state: null,
            millisecondsTimeOutInterval: -1,
            executeOnlyOnce: true);

        bool cancelled = false;

        CancellationTokenRegistration? cancellationTokenRegistration = null;
        cancellationTokenRegistration = cts.Token.Register(() =>
        {
            cancelled = true;
            tcs.TrySetCanceled();
            registeredWaitHandle.Unregister(null);
            cancellationTokenRegistration?.Unregister();
        });

        try
        {
            await tcs.Task.ConfigureAwait(false);
        }
        catch
        {
            if (cancelled)
            {
                return false;
            }
            throw;
        }
        finally
        {
            registeredWaitHandle.Unregister(null);
            cancellationTokenRegistration?.Unregister();
        }

        return true;
    }

    public static ValueTask<bool> WaitAsync(this WaitHandle waitHandle, TimeSpan timeout)
    {
        return WaitAsync(waitHandle, (int)timeout.TotalMilliseconds);
    }
}
