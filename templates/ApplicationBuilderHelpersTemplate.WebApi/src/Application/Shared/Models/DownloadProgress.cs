namespace Application.Shared.Models;

/// <summary>
/// Represents the progress of a file download operation.
/// </summary>
public readonly struct DownloadProgress
{
    /// <summary>
    /// Gets the number of bytes downloaded in the current chunk.
    /// </summary>
    public long BytesDownloaded { get; }

    /// <summary>
    /// Gets the total number of bytes downloaded so far.
    /// </summary>
    public long TotalBytesDownloaded { get; }

    /// <summary>
    /// Gets the total size of the file being downloaded, if known.
    /// </summary>
    public long? TotalBytes { get; }

    /// <summary>
    /// Gets the download progress as a percentage, if total size is known.
    /// </summary>
    public readonly double? Percentage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadProgress"/> struct.
    /// </summary>
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

    /// <inheritdoc />
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
