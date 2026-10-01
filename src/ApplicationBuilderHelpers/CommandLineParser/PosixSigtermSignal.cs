using System;
using System.IO;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Production <see cref="ISigtermSignal"/> over POSIX SIGTERM.
/// No-op on platforms without SIGTERM support.
/// </summary>
internal sealed class PosixSigtermSignal : ISigtermSignal, IDisposable
{
    /// <inheritdoc />
    public event Action? Signaled;

#if NET6_0_OR_GREATER
    private IDisposable? _registration;
#endif
    private bool _disposed;

    internal PosixSigtermSignal()
    {
#if NET6_0_OR_GREATER
        try
        {
            _registration = System.Runtime.InteropServices.PosixSignalRegistration.Create(
                System.Runtime.InteropServices.PosixSignal.SIGTERM,
                context =>
                {
                    context.Cancel = true;
                    Signaled?.Invoke();
                });
        }
        catch (PlatformNotSupportedException)
        {
            _registration = null;
        }
        catch (IOException)
        {
            _registration = null;
        }
#endif
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
#if NET6_0_OR_GREATER
        _registration?.Dispose();
        _registration = null;
#endif
    }
}
