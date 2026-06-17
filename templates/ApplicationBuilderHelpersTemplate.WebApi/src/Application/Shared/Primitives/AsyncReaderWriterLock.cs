using DisposableHelpers.Attributes;

namespace Application.Shared.Primitives;

[Disposable]
public partial class AsyncReaderWriterLock : IDisposable
{
    private readonly SemaphoreSlim _readerSemaphore = new(1, 1); // Controls access to reader count
    private readonly SemaphoreSlim _writerSemaphore = new(1, 1); // Controls exclusive writer access
    private int _readerCount = 0;

    public async Task<IDisposable> EnterReadLockAsync(CancellationToken cancellationToken = default)
    {
        await _readerSemaphore.WaitAsync(cancellationToken);
        try
        {
            _readerCount++;
            if (_readerCount == 1)
            {
                // First reader blocks writers
                await _writerSemaphore.WaitAsync(cancellationToken);
            }
        }
        finally
        {
            _readerSemaphore.Release();
        }

        return new ReadLockReleaser(this);
    }

    public async Task<IDisposable> EnterWriteLockAsync(CancellationToken cancellationToken = default)
    {
        await _writerSemaphore.WaitAsync(cancellationToken);
        return new WriteLockReleaser(this);
    }

    private void ExitReadLock()
    {
        _readerSemaphore.Wait();
        try
        {
            _readerCount--;
            if (_readerCount == 0)
            {
                // Last reader allows writers
                _writerSemaphore.Release();
            }
        }
        finally
        {
            _readerSemaphore.Release();
        }
    }

    private void ExitWriteLock()
    {
        _writerSemaphore.Release();
    }

    private class ReadLockReleaser(AsyncReaderWriterLock parent) : IDisposable
    {
        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                parent.ExitReadLock();
                _disposed = true;
            }
        }
    }

    private class WriteLockReleaser(AsyncReaderWriterLock parent) : IDisposable
    {
        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                parent.ExitWriteLock();
                _disposed = true;
            }
        }
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _readerSemaphore?.Dispose();
            _writerSemaphore?.Dispose();
        }
    }
}
