using System;
using System.IO;
using System.Text;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Installs and removes managed shell-completion blocks.
/// Block format: start marker + managed line + script body + end marker.
/// Bash/Zsh/Pwsh targets are rc/profile files (replace-in-place or append);
/// fish target is a file-drop. Home/XDG/SHELL/OS lookups are injectable for tests.
/// </summary>
internal static class CompletionInstaller
{
    internal static Func<string> HomeProvider = DefaultHomeDirectory;
    internal static Func<string, string?> EnvironmentProvider = static name => Environment.GetEnvironmentVariable(name);
    internal static Func<bool>? IsWindowsProvider;

    internal static string StartMarker(string exe) => $"# >>> {NormalizeExe(exe)} completion >>>";

    internal static string EndMarker(string exe) => $"# <<< {NormalizeExe(exe)} completion <<<";

    internal static string ManagedLine(string exe) => $"# managed by {NormalizeExe(exe)} completions install; do not edit.";

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
        var normalized = NormalizeExe(exe);
        var body = scriptBody ?? string.Empty;
        if (body.Length > 0 && !body.EndsWith('\n'))
            body += "\n";
        return $"{StartMarker(normalized)}\n{ManagedLine(normalized)}\n{body}{EndMarker(normalized)}\n";
    }

    internal static string GetTargetPath(string canonicalShell, string exe)
    {
        var normalized = NormalizeExe(exe);
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
        var normalized = NormalizeExe(exe);
        var target = GetTargetPath(canonicalShell, normalized);
        var block = BuildBlock(normalized, RenderScript(canonicalShell, normalized));

        if (canonicalShell == "fish")
        {
            if (File.Exists(target) && File.ReadAllText(target, Encoding.UTF8) == block)
                return AlreadyInstalled(target, output);
            if (dryRun)
            {
                output.WriteLine($"would-write: {target}");
                output.Write(block);
                return new InstallOutcome(0, true);
            }

            WriteFileAtomic(target, block);
            output.WriteLine($"installed: {target}");
            return new InstallOutcome(0, true);
        }

        string? existing = File.Exists(target) ? File.ReadAllText(target, Encoding.UTF8) : null;
        string updated;
        if (existing == null)
        {
            updated = block;
        }
        else if (TryReplaceBlock(existing, normalized, block, out var replaced))
        {
            updated = replaced;
        }
        else if (existing.Length == 0)
        {
            updated = block;
        }
        else
        {
            updated = existing.EndsWith('\n') ? existing + block : existing + "\n" + block;
        }

        if (existing != null && string.Equals(existing, updated, StringComparison.Ordinal))
            return AlreadyInstalled(target, output);

        if (dryRun)
        {
            output.WriteLine($"would-write: {target}");
            output.Write(updated);
            return new InstallOutcome(0, true);
        }

        WriteFileAtomic(target, updated);
        output.WriteLine($"installed: {target}");
        return new InstallOutcome(0, true);
    }

    internal static InstallOutcome Uninstall(string canonicalShell, string exe, ConsoleOutput output)
    {
        var normalized = NormalizeExe(exe);
        var target = GetTargetPath(canonicalShell, normalized);

        if (!File.Exists(target))
        {
            output.WriteLine($"not installed: {target}");
            return new InstallOutcome(0, true);
        }

        var existing = File.ReadAllText(target, Encoding.UTF8);
        if (!TryExciseBlock(existing, normalized, out var remainder))
        {
            if (canonicalShell == "fish")
                throw new IOException($"Refusing to remove foreign fish completion file (no managed block for '{normalized}'): {target}");
            output.WriteLine($"not installed: {target}");
            return new InstallOutcome(0, true);
        }

        if (canonicalShell == "fish" && string.IsNullOrWhiteSpace(remainder))
        {
            File.Delete(target);
            output.WriteLine($"uninstalled: {target}");
            return new InstallOutcome(0, true);
        }

        WriteFileAtomic(target, remainder);
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

    private static void WriteFileAtomic(string target, string content)
    {
        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temp = Path.Combine(
            string.IsNullOrEmpty(directory) ? Path.GetTempPath() : directory,
            $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temp, content, new UTF8Encoding(false));
        try
        {
            File.Move(temp, target, overwrite: true);
        }
        catch
        {
            try { File.Delete(temp); } catch { }
            throw;
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
            return xdg.Trim();
        return Path.Combine(HomeDirectory(), ".config");
    }

    private static string PwshProfilePath()
    {
        var isWindows = IsWindowsProvider?.Invoke() ?? OperatingSystem.IsWindows();
        if (isWindows)
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

    private static string NormalizeExe(string? exe) =>
        string.IsNullOrWhiteSpace(exe) ? "myapp" : exe.Trim();
}

internal sealed record InstallOutcome(int ExitCode, bool Handled);
