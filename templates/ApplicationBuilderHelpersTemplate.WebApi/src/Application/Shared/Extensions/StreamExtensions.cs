namespace Application.Shared.Extensions;

public static class StreamExtensions
{
    class Progress(Action<long>? callback) : IProgress<long>
    {
        private readonly Action<long>? _callback = callback;

        public void Report(long value)
        {
            _callback?.Invoke(value);
        }
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize = 81920, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));

        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }

    public static Task CopyToAsync(this Stream source, Stream destination, int bufferSize = 81920, Action<long>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        var progress = new Progress(progressCallback);
        return source.CopyToAsync(destination, bufferSize, progress, cancellationToken);
    }
}
