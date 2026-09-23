using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Pre-parse completion handler reading <c>complete</c> and
/// <c>completions script|install|uninstall</c> directly from the raw args before any
/// help/parse/registered-command handling, so user-registered commands
/// with those names never run.
/// </summary>
internal sealed class CompletionGateway(
    ICommandBuilder commandBuilder,
    ConsoleOutput consoleOutput)
{
    /// <summary>
    /// Pre-parse completion check. Returns true when handled (exit with <paramref name="exitCode"/>).
    /// </summary>
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

    private void HandleCompleteProbe(SubCommandInfo? rootCommand, string[] rest)
    {
        try
        {
            var position = -1;
            string? commandline = null;

            for (var i = 0; i < rest.Length; i++)
            {
                var token = rest[i];
                if (string.Equals(token, "--position", StringComparison.Ordinal) && i + 1 < rest.Length)
                {
                    if (int.TryParse(rest[i + 1], out var parsed))
                        position = parsed;
                    i++;
                }
                else if (token.StartsWith("--position=", StringComparison.Ordinal)
                    && int.TryParse(token["--position=".Length..], out var inline))
                {
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
            var candidates = CompletionEngine.Complete(rootCommand, probeArgs, partial);
            foreach (var candidate in candidates)
                consoleOutput.WriteLine(candidate);
        }
        catch
        {
        }
    }

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
            else if (string.Equals(token, "--shell", StringComparison.Ordinal) && i + 1 < rest.Length)
            {
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

    private int HandleCompletionUninstall(string[] rest)
    {
        string? shellOption = null;
        for (var i = 0; i < rest.Length; i++)
        {
            var token = rest[i];
            if (string.Equals(token, "--shell", StringComparison.Ordinal) && i + 1 < rest.Length)
            {
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

    private static List<string> TokenizeCompletionPrefix(string prefix)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char? quote = null;
        var hasToken = false;

        for (var i = 0; i < prefix.Length; i++)
        {
            var c = prefix[i];
            if (quote.HasValue)
            {
                if (c == quote.Value)
                    quote = null;
                else
                    current.Append(c);
                hasToken = true;
            }
            else if (c == '"' || c == '\'')
            {
                quote = c;
                hasToken = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (hasToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }

        if (hasToken)
            tokens.Add(current.ToString());

        return tokens;
    }
}
