using AbsolutePathHelpers;

namespace Application.Shared.Extensions;

public static class AbsolutePathExtensions
{
    public static async Task CreateOrCleanDirectory(this AbsolutePath path, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (path.DirectoryExists())
            {
                Directory.Delete(path.ToString(), true);
            }
            path.CreateDirectory();
        }, cancellationToken);
    }

    public static async Task UncompressTo(this AbsolutePath source, AbsolutePath destination, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            destination.CreateDirectory();
            
            if (source.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(source.ToString(), destination.ToString());
            }
            else if (source.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || 
                     source.Name.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
            {
                // Copy the file
                // Tar extraction uses a library like SharpZipLib; this path copies the file
                File.Copy(source.ToString(), destination / source.Name, true);
            }
            else
            {
                throw new NotSupportedException($"Archive format not supported: {source.Name}");
            }
        }, cancellationToken);
    }

    public static IEnumerable<AbsolutePath> GetFiles(this AbsolutePath directory)
    {
        if (!directory.DirectoryExists())
            return Enumerable.Empty<AbsolutePath>();

        return Directory.GetFiles(directory.ToString())
            .Select(file => AbsolutePath.Create(file));
    }
}