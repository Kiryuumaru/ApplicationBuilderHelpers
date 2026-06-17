using DisposableHelpers.Attributes;

namespace Application.Shared.Primitives;

[Disposable]
public partial class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public AsyncManualResetEvent(bool initialState = false)
    {
        if (initialState)
        {
            _tcs.TrySetResult(true);
        }
    }
    public bool Wait(CancellationToken cancellationToken = default)
        => CoreWait(Timeout.InfiniteTimeSpan, cancellationToken);

    public bool Wait(TimeSpan timeout, CancellationToken cancellationToken = default)
        => CoreWait(timeout, cancellationToken);

    public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken = default)
        => CoreWait(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken);

    public ValueTask<bool> WaitAsync(CancellationToken cancellationToken = default)
        => CoreWaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

    public ValueTask<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        => CoreWaitAsync(timeout, cancellationToken);

    public ValueTask<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
        => CoreWaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken);

    private bool CoreWait(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            var delayTask = Task.Delay(timeout, cancellationToken);
            var completedTask = Task.WhenAny(_tcs.Task, delayTask).GetAwaiter().GetResult();
            if (completedTask == _tcs.Task)
            {
                return true;
            }
        }
        catch
        {
            // Intentionally empty: timeout or cancellation during wait is expected
        }

        return false;
    }

    private async ValueTask<bool> CoreWaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            var delayTask = Task.Delay(timeout, cancellationToken);
            var completedTask = await Task.WhenAny(_tcs.Task, delayTask).ConfigureAwait(false);
            if (completedTask == _tcs.Task)
            {
                return true;
            }
        }
        catch
        {
            // Intentionally empty: timeout or cancellation during wait is expected
        }

        return false;
    }

    public void Set()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (!_tcs.Task.IsCompleted)
        {
            _tcs.TrySetResult(true);
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_tcs.Task.IsCompleted)
        {
            Interlocked.Exchange(ref _tcs, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        }
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            Set();
        }
    }
}
