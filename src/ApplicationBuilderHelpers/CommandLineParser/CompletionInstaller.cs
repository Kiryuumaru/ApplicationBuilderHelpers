using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ApplicationBuilderHelpers.Exceptions;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Installs and removes managed shell-completion blocks.
/// Block format: start marker + managed line + script body + end marker.
/// Bash/Zsh/Pwsh targets are rc/profile files (replace-in-place or append);
/// fish target is a file-drop. Home/XDG/SHELL/OS lookups are injectable for tests.
/// Executable names are validated once here (single owner); shell function
/// identifiers use the transliterated projection in <see cref="CompletionScriptWriter"/>.
/// Mutating install/uninstall paths hold a per-target sibling lock file;
/// dry-run paths are lock-free.
/// </summary>
internal static class CompletionInstaller
{
    internal static Func<string> HomeProvider = DefaultHomeDirectory;
    internal static Func<string, string?> EnvironmentProvider = static name => Environment.GetEnvironmentVariable(name);
    internal static Func<bool>? IsWindowsProvider;

    internal const int MaxExeNameLength = 64;

    /// <summary>
    /// Single owner for executable-name policy: empty/whitespace falls back to
    /// <c>myapp</c>; otherwise the trimmed name must be ASCII letters/digits plus
    /// <c>.</c>, <c>_</c>, <c>-</c> (max 64 chars) and start with an ASCII letter
    /// or <c>_</c> (leading <c>-</c>/<c>.</c>/digits break shell shims).
    /// Anything else is rejected with a usage error (exit 2) naming the allowed set.
    /// </summary>
    internal static string RequireValidExe(string? exe)
    {
        if (string.IsNullOrWhiteSpace(exe))
            return "myapp";
        var trimmed = exe.Trim();
        if (trimmed.Length == 0)
            return "myapp";
        if (trimmed.Length > MaxExeNameLength || !IsValidExeChars(trimmed) || !IsValidExeStart(trimmed[0]))
            throw new CommandException(
                $"Invalid executable name '{SanitizeForMessage(trimmed)}'. Allowed: letters, digits, '.', '_' and '-' (max {MaxExeNameLength} characters), starting with a letter or '_'.",
                2,
                CommandErrorKind.InvalidValue);
        return trimmed;
    }

    private static bool IsValidExeChars(string value)
    {
        foreach (var c in value)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')
                continue;
            return false;
        }

        return true;
    }

    private static bool IsValidExeStart(char first)
    {
        return (first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_';
    }

    private static string SanitizeForMessage(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsControl(chars[i]))
                chars[i] = '?';
        }

        return new string(chars);
    }

    internal static string StartMarker(string exe) => $"# >>> {RequireValidExe(exe)} completion >>>";

    internal static string EndMarker(string exe) => $"# <<< {RequireValidExe(exe)} completion <<<";

    internal static string ManagedLine(string exe) => $"# managed by {RequireValidExe(exe)} completions install; do not edit.";

    internal static bool TryCanonicalizeShell(string? shell, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(shell))
            return false;

        switch (shell.Trim().ToLowerInvariant())
        {
            case "bash":
                canonical = "bash";
                return true;
            case "zsh":
                canonical = "zsh";
                return true;
            case "pwsh":
            case "powershell":
                canonical = "pwsh";
                return true;
            case "fish":
                canonical = "fish";
                return true;
            default:
                return false;
        }
    }

    internal static string? DetectShellFromEnvironment()
    {
        string? raw;
        try
        {
            raw = EnvironmentProvider("SHELL");
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var basename = raw.Trim().Replace('\\', '/');
        var slash = basename.LastIndexOf('/');
        if (slash >= 0)
            basename = basename[(slash + 1)..];
        basename = basename.Trim().ToLowerInvariant();
        if (basename.EndsWith(".exe", StringComparison.Ordinal))
            basename = basename[..^4];
        return string.IsNullOrWhiteSpace(basename) ? null : basename;
    }

    internal static string RenderScript(string canonicalShell, string exe)
    {
        using var writer = new StringWriter();
        var output = new ConsoleOutput(writer, writer);
        switch (canonicalShell)
        {
            case "bash":
                CompletionScriptWriter.WriteBash(output, exe);
                break;
            case "zsh":
                CompletionScriptWriter.WriteZsh(output, exe);
                break;
            case "pwsh":
                CompletionScriptWriter.WritePwsh(output, exe);
                break;
            case "fish":
                CompletionScriptWriter.WriteFish(output, exe);
                break;
            default:
                throw new InvalidOperationException($"Unknown shell '{canonicalShell}'.");
        }

        return writer.ToString();
    }

    internal static string BuildBlock(string exe, string scriptBody)
    {
        var normalized = RequireValidExe(exe);
        var body = scriptBody ?? string.Empty;
        if (body.Length > 0 && !body.EndsWith('\n'))
            body += "\n";
        return $"{StartMarker(normalized)}\n{ManagedLine(normalized)}\n{body}{EndMarker(normalized)}\n";
    }

    internal static string GetTargetPath(string canonicalShell, string exe)
    {
        var normalized = RequireValidExe(exe);
        switch (canonicalShell)
        {
            case "bash":
                return Path.Combine(HomeDirectory(), ".bashrc");
            case "zsh":
                return Path.Combine(HomeDirectory(), ".zshrc");
            case "pwsh":
                return PwshProfilePath();
            case "fish":
                return Path.Combine(ConfigDirectory(), "fish", "completions", $"{normalized}.fish");
            default:
                throw new InvalidOperationException($"Unknown shell '{canonicalShell}'.");
        }
    }

    internal static InstallOutcome Install(string canonicalShell, string exe, bool dryRun, ConsoleOutput output)
    {
        var normalized = RequireValidExe(exe);
        var target = GetTargetPath(canonicalShell, normalized);
        var block = BuildBlock(normalized, RenderScript(canonicalShell, normalized));

        if (canonicalShell == "fish")
        {
            if (File.Exists(target))
            {
                // Byte-exact comparison: managed fish files are always written
                // UTF8-no-BOM, so stale encodings count as drift and reinstall.
                byte[] existingBytes;
                try
                {
                    existingBytes = File.ReadAllBytes(target);
                }
                catch (DirectoryNotFoundException)
                {
                    existingBytes = [];
                }

                if (existingBytes.SequenceEqual(new UTF8Encoding(false).GetBytes(block)))
                    return AlreadyInstalled(target, output);
            }

            if (dryRun)
            {
                output.WriteLine($"would-write: {target}");
                output.Write(block);
                return new InstallOutcome(0, true);
            }

            using (AcquireTargetLock(target))
            {
                if (File.Exists(target))
                {
                    var reread = File.ReadAllBytes(target);
                    if (reread.SequenceEqual(new UTF8Encoding(false).GetBytes(block)))
                        return AlreadyInstalled(target, output);
                }

                WriteFileAtomic(target, block);
            }

            output.WriteLine($"installed: {target}");
            return new InstallOutcome(0, true);
        }

        if (dryRun)
        {
            string? preview = File.Exists(target) ? File.ReadAllText(target, Encoding.UTF8) : null;
            var previewUpdated = ComputeRcUpdate(preview, normalized, block);
            if (preview != null && string.Equals(preview, previewUpdated, StringComparison.Ordinal))
                return AlreadyInstalled(target, output);
            output.WriteLine($"would-write: {target}");
            output.Write(previewUpdated);
            return new InstallOutcome(0, true);
        }

        string? existingLocked;
        string updatedLocked;
        using (AcquireTargetLock(target))
        {
            existingLocked = File.Exists(target) ? File.ReadAllText(target, Encoding.UTF8) : null;
            updatedLocked = ComputeRcUpdate(existingLocked, normalized, block);
            if (existingLocked != null && string.Equals(existingLocked, updatedLocked, StringComparison.Ordinal))
            {
                AlreadyInstalledLocked(target, output);
            }
            else
            {
                WriteFileAtomic(target, updatedLocked);
            }
        }

        if (existingLocked != null && string.Equals(existingLocked, updatedLocked, StringComparison.Ordinal))
            return new InstallOutcome(0, true);

        output.WriteLine($"installed: {target}");
        return new InstallOutcome(0, true);
    }

    private static void AlreadyInstalledLocked(string target, ConsoleOutput output)
    {
        output.WriteLine($"already installed: {target}");
    }

    private static string ComputeRcUpdate(string? existing, string normalized, string block)
    {
        if (existing == null)
            return block;
        if (TryReplaceBlock(existing, normalized, block, out var replaced))
            return replaced;
        if (existing.Length == 0)
            return block;
        return existing.EndsWith('\n') ? existing + block : existing + "\n" + block;
    }

    internal static InstallOutcome Uninstall(string canonicalShell, string exe, ConsoleOutput output)
    {
        var normalized = RequireValidExe(exe);
        var target = GetTargetPath(canonicalShell, normalized);

        if (canonicalShell == "fish")
        {
            if (!File.Exists(target))
            {
                output.WriteLine($"not installed: {target}");
                return new InstallOutcome(0, true);
            }

            using (AcquireTargetLock(target))
            {
                if (!File.Exists(target))
                {
                    output.WriteLine($"not installed: {target}");
                    return new InstallOutcome(0, true);
                }

                var existingLocked = File.ReadAllText(target, Encoding.UTF8);
                if (!TryExciseBlock(existingLocked, normalized, out var remainderLocked))
                    throw new IOException($"Refusing to remove foreign fish completion file (no managed block for '{normalized}'): {target}");
                if (string.IsNullOrWhiteSpace(remainderLocked))
                    File.Delete(target);
                else
                    WriteFileAtomic(target, remainderLocked);
            }

            output.WriteLine($"uninstalled: {target}");
            return new InstallOutcome(0, true);
        }

        if (!File.Exists(target))
        {
            output.WriteLine($"not installed: {target}");
            return new InstallOutcome(0, true);
        }

        string? remainder;
        bool found;
        using (AcquireTargetLock(target))
        {
            if (!File.Exists(target))
            {
                remainder = null;
                found = false;
            }
            else
            {
                var existing = File.ReadAllText(target, Encoding.UTF8);
                found = TryExciseBlock(existing, normalized, out var excised);
                remainder = found ? excised : null;
                if (found)
                    WriteFileAtomic(target, excised);
            }
        }

        if (!found)
        {
            output.WriteLine($"not installed: {target}");
            return new InstallOutcome(0, true);
        }

        output.WriteLine($"uninstalled: {target}");
        return new InstallOutcome(0, true);
    }

    private static InstallOutcome AlreadyInstalled(string target, ConsoleOutput output)
    {
        output.WriteLine($"already installed: {target}");
        return new InstallOutcome(0, true);
    }

    internal static bool TryReplaceBlock(string content, string exe, string newBlock, out string updated)
    {
        updated = content;
        var start = StartMarker(exe);
        var end = EndMarker(exe);
        var startIndex = content.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            return false;
        var endIndex = content.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            return false;

        var afterEnd = endIndex + end.Length;
        if (afterEnd < content.Length && content[afterEnd] == '\r')
            afterEnd++;
        if (afterEnd < content.Length && content[afterEnd] == '\n')
            afterEnd++;

        updated = content[..startIndex] + newBlock + content[afterEnd..];
        return true;
    }

    internal static bool TryExciseBlock(string content, string exe, out string remainder)
    {
        remainder = content;
        var start = StartMarker(exe);
        var end = EndMarker(exe);
        var startIndex = content.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            return false;
        var endIndex = content.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            return false;

        var afterEnd = endIndex + end.Length;
        if (afterEnd < content.Length && content[afterEnd] == '\r')
            afterEnd++;
        if (afterEnd < content.Length && content[afterEnd] == '\n')
            afterEnd++;

        remainder = content[..startIndex] + content[afterEnd..];
        return true;
    }

    internal static string LockPathFor(string target) => target + ".lock";

    internal static IDisposable AcquireTargetLock(string target)
    {
        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var lockPath = LockPathFor(target);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (true)
        {
            var token = TryCreateLockFile(lockPath);
            if (token != null)
                return new TargetLock(lockPath, token);
            if (DateTime.UtcNow >= deadline)
                throw new IOException($"Timed out waiting for lock file '{lockPath}'. Another install is in progress.");
            Thread.Sleep(100);
        }
    }

    private static string? TryCreateLockFile(string lockPath)
    {
        try
        {
            var token = Guid.NewGuid().ToString("N");
            using var stream = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var payload = $"token={token} pid={Environment.ProcessId} host={Environment.MachineName} time={DateTime.UtcNow:O}\n";
            var bytes = Encoding.UTF8.GetBytes(payload);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
            return token;
        }
        catch (IOException)
        {
            if (IsLockStale(lockPath))
            {
                try { File.Delete(lockPath); } catch { }
            }

            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException($"Access denied creating lock file '{lockPath}'. Check directory permissions. ({ex.Message})");
        }
    }

    private static bool IsLockStale(string lockPath)
    {
        DateTime lastWrite;
        try
        {
            lastWrite = File.GetLastWriteTimeUtc(lockPath);
        }
        catch
        {
            return false;
        }

        return DateTime.UtcNow - lastWrite >= TimeSpan.FromSeconds(10);
    }

    private sealed class TargetLock : IDisposable
    {
        private readonly string _lockPath;
        private readonly string _token;
        private readonly Timer _heartbeat;
        private bool _disposed;

        internal TargetLock(string lockPath, string token)
        {
            _lockPath = lockPath;
            _token = token;
            _heartbeat = new Timer(static state =>
            {
                var self = (TargetLock)state!;
                try
                {
                    if (LockTokenMatches(self._lockPath, self._token))
                        File.SetLastWriteTimeUtc(self._lockPath, DateTime.UtcNow);
                }
                catch
                {
                }
            }, this, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try { _heartbeat.Dispose(); } catch { }
            try
            {
                if (LockTokenMatches(_lockPath, _token))
                    File.Delete(_lockPath);
            }
            catch { }
        }
    }

    private static bool LockTokenMatches(string lockPath, string token)
    {
        try
        {
            using var stream = new FileStream(lockPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[Math.Min(128, (int)Math.Min(stream.Length, 128))];
            var read = stream.Read(buffer, 0, buffer.Length);
            var content = Encoding.UTF8.GetString(buffer, 0, read);
            return content.StartsWith($"token={token} ", StringComparison.Ordinal)
                || content.StartsWith($"token={token}\n", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsWindows() => IsWindowsProvider?.Invoke() ?? OperatingSystem.IsWindows();

    private static void WriteFileAtomic(string target, string content)
    {
        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temp = Path.Combine(
            string.IsNullOrEmpty(directory) ? Path.GetTempPath() : directory,
            $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.tmp");
        WriteAllTextDurable(temp, content);
        MoveWithWindowsRetry(temp, target);
    }

    private static void WriteAllTextDurable(string path, string content)
    {
        var bytes = new UTF8Encoding(false).GetBytes(content);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        SyncParentDirectory(path);
    }

    private static void MoveWithWindowsRetry(string temp, string target)
    {
        const int attempts = 5;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                File.Move(temp, target, overwrite: true);
                SyncParentDirectory(target);
                return;
            }
            catch (IOException) when (IsWindows() && attempt + 1 < attempts)
            {
                Thread.Sleep(50 * (attempt + 1));
            }
            catch
            {
                try { File.Delete(temp); } catch { }
                throw;
            }
        }

        try { File.Delete(temp); } catch { }
        throw new IOException($"Could not replace '{target}' (destination busy).");
    }

    private static void SyncParentDirectory(string path)
    {
        if (IsWindows())
            return;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (string.IsNullOrEmpty(directory))
            return;
        try
        {
            using var dir = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            dir.Flush(true);
        }
        catch
        {
            // Best effort: directory fsync is advisory; the file fsync above already landed the content.
        }
    }

    private static string HomeDirectory()
    {
        var home = HomeProvider();
        if (string.IsNullOrWhiteSpace(home))
            throw new IOException("Could not resolve home directory.");
        return home;
    }

    private static string ConfigDirectory()
    {
        string? xdg = null;
        try
        {
            xdg = EnvironmentProvider("XDG_CONFIG_HOME");
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(xdg))
        {
            var trimmed = xdg.Trim();
            if (Path.IsPathFullyQualified(trimmed))
                return trimmed;
        }

        return Path.Combine(HomeDirectory(), ".config");
    }

    private static string PwshProfilePath()
    {
        if (IsWindows())
            return Path.Combine(HomeDirectory(), "Documents", "PowerShell", "Microsoft.PowerShell_profile.ps1");
        return Path.Combine(ConfigDirectory(), "powershell", "Microsoft.PowerShell_profile.ps1");
    }

    private static string DefaultHomeDirectory()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
            return profile;
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(home))
            return home;
        throw new IOException("Could not resolve home directory.");
    }
}

internal sealed record InstallOutcome(int ExitCode, bool Handled);
