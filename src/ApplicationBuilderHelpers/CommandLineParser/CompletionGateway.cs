using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Completion pre-parse gate: answers shell completion before anything else runs.</summary>
internal sealed class CompletionGateway(
    ICommandBuilder commandBuilder,
    ConsoleOutput consoleOutput)
{
    /// <summary>Pre-parse completion check; true means handled with exit <paramref name="exitCode"/>.</summary>
    internal bool TryHandle(SubCommandInfo? rootCommand, string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0)
            return false;

        if (string.Equals(args[0], "complete", StringComparison.Ordinal))
        {
            HandleCompleteProbe(rootCommand, args[1..]);
            return true;
        }

        if (!string.Equals(args[0], "completions", StringComparison.Ordinal) || args.Length < 2)
            return false;

        if (string.Equals(args[1], "script", StringComparison.Ordinal))
        {
            if (args.Length < 3)
                return false;
            exitCode = HandleCompletionScript(args[2]);
            return true;
        }

        if (string.Equals(args[1], "install", StringComparison.Ordinal)
            || string.Equals(args[1], "uninstall", StringComparison.Ordinal))
        {
            var install = string.Equals(args[1], "install", StringComparison.Ordinal);
            exitCode = install
                ? HandleCompletionInstall(args[2..])
                : HandleCompletionUninstall(args[2..]);
            return true;
        }

        return false;
    }

    /// <summary>Handles the <c>complete</c> probe: parses position and prints candidates.</summary>
    private void HandleCompleteProbe(SubCommandInfo? rootCommand, string[] rest)
    {
        try
        {
            var position = -1;
            string? commandline = null;

            for (var i = 0; i < rest.Length; i++)
            {
                var token = rest[i];
                if (string.Equals(token, "--position", StringComparison.Ordinal))
                {
                    if (i + 1 >= rest.Length || !int.TryParse(rest[i + 1], out var parsed))
                        return;
                    position = parsed;
                    i++;
                }
                else if (token.StartsWith("--position=", StringComparison.Ordinal))
                {
                    if (!int.TryParse(token["--position=".Length..], out var inline))
                        return;
                    position = inline;
                }
                else if (commandline == null)
                {
                    commandline = token;
                }
                else
                {
                    commandline += " " + token;
                }
            }

            commandline ??= string.Empty;
            if (position < 0)
                position = commandline.Length;
            position = Math.Max(0, Math.Min(position, commandline.Length));

            var (probeArgs, partial) = SplitCompletionPrefix(commandline, position);
            if (partial.StartsWith("@", StringComparison.Ordinal) && !partial.StartsWith("@@", StringComparison.Ordinal))
                return;
            probeArgs = ResponseFileExpander.Expand(probeArgs);
            var candidates = CompletionEngine.Complete(rootCommand, probeArgs, partial);
            foreach (var candidate in candidates)
                consoleOutput.WriteLine(candidate);
        }
        catch
        {
            // TAB completion never throws; faults yield no candidates.
        }
    }

    /// <summary>Handles <c>completions install</c>; usage errors exit 2, I/O faults exit 1.</summary>
    private int HandleCompletionInstall(string[] rest)
    {
        string? shellOption = null;
        var dryRun = false;
        for (var i = 0; i < rest.Length; i++)
        {
            var token = rest[i];
            if (string.Equals(token, "--dry-run", StringComparison.Ordinal))
            {
                dryRun = true;
            }
            else if (string.Equals(token, "--shell", StringComparison.Ordinal))
            {
                if (i + 1 >= rest.Length)
                {
                    consoleOutput.WriteLineError("Missing value for '--shell'. Expected bash, zsh, pwsh, or fish.");
                    return 2;
                }

                shellOption = rest[++i];
            }
            else if (token.StartsWith("--shell=", StringComparison.Ordinal))
            {
                shellOption = token["--shell=".Length..];
            }
            else
            {
                consoleOutput.WriteLineError($"Unknown option '{token}' for 'completions install'.");
                return 2;
            }
        }

        if (!TryResolveShell(shellOption, out var canonical))
            return 2;

        try
        {
            var exe = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
            CompletionInstaller.Install(canonical, exe, dryRun, consoleOutput);
            return 0;
        }
        catch (IOException ex)
        {
            consoleOutput.WriteLineError(ex.Message);
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            consoleOutput.WriteLineError(ex.Message);
            return 1;
        }
    }

    /// <summary>Handles <c>completions uninstall</c>; usage errors exit 2, I/O faults exit 1.</summary>
    private int HandleCompletionUninstall(string[] rest)
    {
        string? shellOption = null;
        for (var i = 0; i < rest.Length; i++)
        {
            var token = rest[i];
            if (string.Equals(token, "--shell", StringComparison.Ordinal))
            {
                if (i + 1 >= rest.Length)
                {
                    consoleOutput.WriteLineError("Missing value for '--shell'. Expected bash, zsh, pwsh, or fish.");
                    return 2;
                }

                shellOption = rest[++i];
            }
            else if (token.StartsWith("--shell=", StringComparison.Ordinal))
            {
                shellOption = token["--shell=".Length..];
            }
            else
            {
                consoleOutput.WriteLineError($"Unknown option '{token}' for 'completions uninstall'.");
                return 2;
            }
        }

        if (!TryResolveShell(shellOption, out var canonical))
            return 2;

        try
        {
            var exe = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
            CompletionInstaller.Uninstall(canonical, exe, consoleOutput);
            return 0;
        }
        catch (IOException ex)
        {
            consoleOutput.WriteLineError(ex.Message);
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            consoleOutput.WriteLineError(ex.Message);
            return 1;
        }
    }

    /// <summary>Handles <c>completions script</c>; unknown shells exit 2.</summary>
    private int HandleCompletionScript(string shell)
    {
        if (!CompletionInstaller.TryCanonicalizeShell(shell, out var canonical))
        {
            consoleOutput.WriteLineError($"Unknown shell '{shell}'. Expected bash, zsh, pwsh, or fish.");
            return 2;
        }

        var exe = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
        switch (canonical)
        {
            case "bash":
                CompletionScriptWriter.WriteBash(consoleOutput, exe);
                return 0;
            case "zsh":
                CompletionScriptWriter.WriteZsh(consoleOutput, exe);
                return 0;
            case "pwsh":
                CompletionScriptWriter.WritePwsh(consoleOutput, exe);
                return 0;
            case "fish":
                CompletionScriptWriter.WriteFish(consoleOutput, exe);
                return 0;
            default:
                consoleOutput.WriteLineError($"Unknown shell '{shell}'. Expected bash, zsh, pwsh, or fish.");
                return 2;
        }
    }

    /// <summary>Resolves the target shell from the flag or the environment.</summary>
    private bool TryResolveShell(string? shellOption, out string canonical)
    {
        var shell = shellOption ?? CompletionInstaller.DetectShellFromEnvironment();
        if (!CompletionInstaller.TryCanonicalizeShell(shell, out canonical))
        {
            consoleOutput.WriteLineError(string.IsNullOrWhiteSpace(shell)
                ? "Could not detect shell from $SHELL. Pass --shell <bash|zsh|pwsh|fish>."
                : $"Unknown shell '{shell}'. Expected bash, zsh, pwsh, or fish.");
            return false;
        }

        return true;
    }

    /// <summary>Slices the command line at the cursor into probe args plus the partial token.</summary>
    private static (string[] Args, string Partial) SplitCompletionPrefix(string commandline, int position)
    {
        var prefix = commandline[..position];
        var tokens = TokenizeCompletionPrefix(prefix);
        if (tokens.Count == 0)
            return ([], string.Empty);

        var withoutExe = tokens.Skip(1).ToArray();
        if (withoutExe.Length == 0)
            return ([], string.Empty);

        if (prefix.Length > 0 && char.IsWhiteSpace(prefix[^1]))
            return (withoutExe, string.Empty);

        return (withoutExe[..^1], withoutExe[^1]);
    }

    /// <summary>Tokenizes the pre-cursor prefix via the shared command-line splitter.</summary>
    private static List<string> TokenizeCompletionPrefix(string prefix) => CommandLineTokenizer.Tokenize(prefix);
}
