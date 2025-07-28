using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

internal static class ThreadHelpers
{
    public static async Task WaitThread(Action action, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim reset = new(0);
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                reset.Release();
            }
        })
        {
            IsBackground = true
        };
        thread.Start();

        var threadTask = Task.Run(async () =>
        {
            await reset.WaitAsync();
            thread.Join();
            if (exception != null)
            {
                throw exception;
            }
        }, cancellationToken);

        if (await Task.WhenAny(threadTask, Task.Delay(Timeout.Infinite, cancellationToken)) == threadTask)
        {
            await threadTask;
        }
        else
        {
            //thread.Interrupt();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    public static async Task WaitThread(Func<Task> task, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim reset = new(0);
        Exception? exception = null;
        var thread = new Thread(async () =>
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                reset.Release();
            }
        })
        {
            IsBackground = true
        };
        thread.Start();

        var threadTask = Task.Run(async () =>
        {
            await reset.WaitAsync();
            thread.Join();
            if (exception != null)
            {
                throw exception;
            }
        }, cancellationToken);

        if (await Task.WhenAny(threadTask, Task.Delay(Timeout.Infinite, cancellationToken)) == threadTask)
        {
            await threadTask;
        }
        else
        {
            //thread.Interrupt();
            throw new OperationCanceledException(cancellationToken);
        }
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
        return waitHandle.WaitAsync((int)timeout.TotalMilliseconds);
    }
}