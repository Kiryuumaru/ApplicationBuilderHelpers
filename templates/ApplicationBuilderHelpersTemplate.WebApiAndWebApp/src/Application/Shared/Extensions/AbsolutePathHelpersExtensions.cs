using AbsolutePathHelpers;
using System.Text;

namespace Application.Shared.Extensions;

public static class AbsolutePathHelpersExtensions
{
    public static string GetArchiveTypeBySignature(this AbsolutePath filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            // Check if file is large enough for basic signature detection
            if (fs.Length < 4)
                return "Unknown";
                
            var buffer = new byte[Math.Min(10, (int)fs.Length)];
            var bytesRead = fs.Read(buffer);
            
            if (bytesRead < 2)
                return "Unknown";

            // ZIP files (need at least 4 bytes)
            if (bytesRead >= 4 && buffer[0] == 0x50 && buffer[1] == 0x4B &&
                (buffer[2] == 0x03 || buffer[2] == 0x05 || buffer[2] == 0x07))
                return "ZIP";

            // RAR files (need at least 4 bytes)
            if (bytesRead >= 4 && buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21)
                return "RAR";

            // 7-Zip files (need at least 4 bytes)
            if (bytesRead >= 4 && buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF)
                return "7Z";

            // GZIP files (need at least 2 bytes)
            if (bytesRead >= 2 && buffer[0] == 0x1F && buffer[1] == 0x8B)
                return "GZIP";

            // MSI files (Compound File Binary Format) (need at least 8 bytes)
            if (bytesRead >= 8 && buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
                buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)
                return "MSI";

            // TAR files (more complex detection needed)
            // Check for TAR signature at offset 257 (need at least 262 bytes total)
            if (fs.Length >= 262)
            {
                fs.Seek(257, SeekOrigin.Begin);
                var tarBuffer = new byte[5];
                var tarBytesRead = fs.Read(tarBuffer);
                if (tarBytesRead == 5 && Encoding.ASCII.GetString(tarBuffer) == "ustar")
                    return "TAR";
            }

            return "Unknown";
        }
        catch (Exception)
        {
            // If we can't read the file for any reason (permissions, corruption, etc.)
            // just return "Unknown" instead of throwing
            return "Unknown";
        }
    }

    public static async Task TryKillPath1(this AbsolutePath path, string[]? skipTaskNames = null, CancellationToken cancellationToken = default)
    {
        if (skipTaskNames == null || skipTaskNames.Length == 0)
        {
            skipTaskNames ??= ["explorer", "msiexec"];
        }
        
        int iterations = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            int procsCount = 0;
            int kills = 0;
            
            var procs = await path.GetProcesses(cancellationToken);
            
            foreach (var proc in procs)
            {
                try
                {
                    var procName = proc.ProcessName;
                    
                    if (skipTaskNames.Any(i => i.Contains(procName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    
                    procsCount++;
                    proc.Kill(true);
                    kills++;
                }
                catch
                {
                    // Intentionally empty: best-effort process kill, failure is expected for protected processes
                }
            }
            
            if (procsCount == kills)
            {
                break;
            }
            
            if (iterations > 20)
            {
                break;
            }
            
            iterations++;
            await Task.Delay(2000, cancellationToken);
        }
    }

    public static async Task Murder(this AbsolutePath path, string[]? skipTaskNames = null, CancellationToken cancellationToken = default)
    {
        if (skipTaskNames == null || skipTaskNames.Length == 0)
        {
            skipTaskNames ??= ["explorer", "msiexec"];
        }

        int iterations = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await path.Delete(cancellationToken);
            }
            catch
            {
                // Intentionally empty: best-effort delete, process holding file lock may prevent deletion
            }

            if (!path.IsExists())
            {
                break;
            }

            int procsCount = 0;
            int kills = 0;

            var procs = await path.GetProcesses(cancellationToken);

            foreach (var proc in procs)
            {
                try
                {
                    var procName = proc.ProcessName;

                    if (skipTaskNames.Any(i => i.Contains(procName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    procsCount++;
                    proc.Kill(true);
                    kills++;
                }
                catch
                {
                    // Intentionally empty: best-effort process kill, failure is expected for protected processes
                }
            }

            if (procsCount == kills)
            {
                break;
            }

            if (iterations > 20)
            {
                break;
            }

            iterations++;
            await Task.Delay(2000, cancellationToken);
        }
    }
}
