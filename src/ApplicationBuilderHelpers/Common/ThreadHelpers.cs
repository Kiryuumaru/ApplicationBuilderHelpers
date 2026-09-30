using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

/// <summary>Background-thread execution and wait-handle bridging for command runs.</summary>
internal static class ThreadHelpers
{
    /// <summary>Runs <paramref name="action"/> on a background thread; cancellation abandons the wait.</summary>
    /// <param name="action">Synchronous work to run off the caller thread.</param>
    /// <param name="cancellationToken">Abandons the wait when canceled.</param>
    /// <returns>A task completing when the worker finishes or cancellation is requested.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> cancels first.</exception>
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
            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <summary>Runs async <paramref name="task"/> work on a background thread; cancellation abandons the wait.</summary>
    /// <param name="task">Asynchronous work to run off the caller thread.</param>
    /// <param name="cancellationToken">Abandons the wait when canceled.</param>
    /// <returns>A task completing when the worker finishes or cancellation is requested.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> cancels first.</exception>
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
            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <summary>Bridges <paramref name="waitHandle"/> to a task; <c>false</c> on cancellation.</summary>
    /// <param name="waitHandle">Handle to wait on.</param>
    /// <param name="cancellationToken">Returns <c>false</c> when canceled.</param>
    /// <returns>True when signaled; false on cancellation.</returns>
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

    /// <summary>Bridges <paramref name="waitHandle"/> to a task; <c>false</c> on timeout.</summary>
    /// <param name="waitHandle">Handle to wait on.</param>
    /// <param name="millisecondsTimeout">Timeout in milliseconds; zero polls once.</param>
    /// <returns>True when signaled; false on timeout.</returns>
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

    /// <summary>Bridges <paramref name="waitHandle"/> to a task; <c>false</c> on timeout.</summary>
    /// <param name="waitHandle">Handle to wait on.</param>
    /// <param name="timeout">Timeout duration; truncated to whole milliseconds.</param>
    /// <returns>True when signaled; false on timeout.</returns>
    public static ValueTask<bool> WaitAsync(this WaitHandle waitHandle, TimeSpan timeout)
    {
        return waitHandle.WaitAsync((int)timeout.TotalMilliseconds);
    }
}
