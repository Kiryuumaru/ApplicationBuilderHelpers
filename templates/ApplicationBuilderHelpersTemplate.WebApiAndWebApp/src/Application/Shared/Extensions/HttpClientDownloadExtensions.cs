using AbsolutePathHelpers;

namespace Application.Shared.Extensions;

public static class HttpClientDownloadExtensions
{
    public readonly struct DownloadProgress
    {
        public long BytesDownloaded { get; }

        public long TotalBytesDownloaded { get; }

        public long? TotalBytes { get; }

        public readonly double? Percentage { get; }

        public DownloadProgress(long bytesDownloaded, long totalBytesDownloaded, long? totalBytes)
        {
            BytesDownloaded = bytesDownloaded;
            TotalBytesDownloaded = totalBytesDownloaded;
            TotalBytes = totalBytes;
            if (totalBytes.HasValue)
            {
                Percentage = (double)TotalBytesDownloaded / TotalBytes * 100;
            }
        }

        public override readonly string ToString()
        {
            if (TotalBytes.HasValue)
            {
                return $"Downloaded {TotalBytesDownloaded} of {TotalBytes} bytes ({Percentage:F2}%)";
            }
            else
            {
                return $"Downloaded {TotalBytesDownloaded} bytes";
            }
        }
    }

    public static async Task DownloadFile(this HttpClient httpClient, Uri url, Func<(Memory<byte> Memory, long BytesRead, long? ContentLength), ValueTask> onBytesRead, int bufferSize = 8192, CancellationToken cancellationToken = default)
    {
        if (url == null)
            throw new ArgumentException("URL cannot be null.", nameof(url));

        ArgumentNullException.ThrowIfNull(onBytesRead);

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        response.Headers.TryGetValues("Content-Length", out var contentLengthHeader);

        var contentLength = response.Content.Headers.ContentLength;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        Memory<byte> buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await onBytesRead((buffer[..bytesRead], bytesRead, contentLength));
        }
    }

    public static async Task DownloadFile(this HttpClient httpClient, Uri url, AbsolutePath filePath, Action<DownloadProgress>? progress = null, TimeSpan? progressReporting = null, int bufferSize = 8192, CancellationToken cancellationToken = default)
    {
        if (filePath.FileExists())
            throw new InvalidOperationException($"File already exists: {filePath}");

        if (filePath.DirectoryExists())
            throw new InvalidOperationException($"Directory already exists: {filePath}");

        filePath.Parent?.CreateDirectory();

        long totalBytes = 0;
        DateTimeOffset dateTimeOffset = DateTimeOffset.MinValue;
        DownloadProgress? lastDownloadProgress = null;
        bool hasReportedProgress = false;

        using var file = File.OpenWrite(filePath);

        await DownloadFile(httpClient, url, async onRead =>
        {
            hasReportedProgress = false;
            var now = DateTimeOffset.UtcNow;
            await file.WriteAsync(onRead.Memory);
            await file.FlushAsync();
            totalBytes += onRead.BytesRead;
            lastDownloadProgress = new DownloadProgress(onRead.BytesRead, totalBytes, onRead.ContentLength);
            if (progressReporting != null)
            {
                if ((dateTimeOffset + progressReporting.Value) < now)
                {
                    dateTimeOffset = now;
                    progress?.Invoke(lastDownloadProgress.Value);
                    hasReportedProgress = true;
                }
            }
            else
            {
                progress?.Invoke(lastDownloadProgress.Value);
                hasReportedProgress = true;
            }
        }, bufferSize: bufferSize, cancellationToken: cancellationToken);
        if (!hasReportedProgress && lastDownloadProgress.HasValue)
        {
            progress?.Invoke(lastDownloadProgress.Value);
        }
    }
}
