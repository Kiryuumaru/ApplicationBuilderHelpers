using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Command line parser that supports hierarchical subcommands with option/argument inheritance.
/// Implements the processing order: Build hierarchy first, then parse arguments.
/// Thin orchestrator over internal collaborators (mechanical split, no behavior change).
/// </summary>
internal class CommandLineParser
{
    public ApplicationBuilder ApplicationBuilder { get; }
    public ICommandBuilder CommandBuilder { get; }
    public ICommandTypeParserCollection CommandTypeParserCollection { get; }
    public IApplicationDependencyCollection ApplicationDependencyCollection { get; }
    internal ConsoleOutput ConsoleOutput { get; }

    private SubCommandInfo? _rootCommand;
    private readonly Dictionary<string, SubCommandInfo> _allCommands = [];

    private readonly CommandHierarchyBuilder _hierarchy;
    private readonly ArgumentParser _parser;
    private readonly ParameterValidator _validator;
    private readonly ValueBinder _binder;
    private readonly CommandExecutor _executor;
    private readonly HelpVersionGateway _helpGateway;

    internal CommandLineParser(ApplicationBuilder applicationBuilder, CommandReflectionCache? reflectionCache = null, ConsoleOutput? consoleOutput = null)
    {
        ApplicationBuilder = applicationBuilder;
        CommandBuilder = applicationBuilder;
        CommandTypeParserCollection = applicationBuilder;
        ApplicationDependencyCollection = applicationBuilder;
        ConsoleOutput = consoleOutput ?? new ConsoleOutput();

        _hierarchy = new CommandHierarchyBuilder(CommandBuilder, CommandTypeParserCollection, reflectionCache ?? new CommandReflectionCache());
        _parser = new ArgumentParser();
        _validator = new ParameterValidator();
        _binder = new ValueBinder(CommandTypeParserCollection);
        _executor = new CommandExecutor(ApplicationDependencyCollection, ConsoleOutput);
        _helpGateway = new HelpVersionGateway(CommandBuilder, ConsoleOutput);
    }

    /// <summary>
    /// Main entry point - builds hierarchy then parses and executes
    /// </summary>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Prepare application builder with dependencies
            foreach (var dependency in ApplicationDependencyCollection.ApplicationDependencies)
            {
                dependency.CommandPreparation(ApplicationBuilder);
            }

            // Step 2: Build and validate command hierarchy
            BuildCommandHierarchy();
            ValidateCommandHierarchy();

            // Step 2b: Pre-parse completion gateway (never touches the registered-command path)
            if (TryHandleCompletionGateway(args))
                return 0;

            // Step 3: Handle bare single --help before parsing
            if (ShouldShowGlobalHelp(args))
            {
                ShowGlobalHelp();
                return 0;
            }

            // Step 4: Parse command line arguments
            var parseResult = ParseCommandLine(args);

            // Step 4b: Post-parse version check on leftover unconsumed tokens (consumed option values never trigger)
            if (parseResult.ShowVersion)
            {
                ShowVersion();
                return 0;
            }

            // Step 5: Prepare application builder with dependencies
            parseResult.TargetCommand.Command?.CommandPreparation(ApplicationBuilder);

            // Step 6: Handle bare command help (no values collected)
            if (parseResult.ShowHelp && parseResult.OptionValues.Count == 0 && parseResult.ArgumentValues.Count == 0)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            // Step 7: Validate required options and arguments
            ValidateRequiredParameters(parseResult);

            // Step 7b: Handle command help when values were collected
            if (parseResult.ShowHelp)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            // Step 8: Set property values on command instance
            SetCommandValues(parseResult);

            // Step 9: Execute the command. A normal return means success (exit code 0).
            await ExecuteCommand(parseResult.TargetCommand, cancellationToken);
            return 0;
        }
        catch (CommandExecutor.ExternalCancellationException)
        {
            // Cancellation was requested: 128 + SIGINT.
            return CommandExecutor.CanceledExitCode;
        }
        catch (CommandException ex)
        {
            ShowErrorMessage(ex.Message, ex.Kind, ex.CommandName);
            return ex.ExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelled before execution could map the outcome (e.g. pre-cancelled token).
            return CommandExecutor.CanceledExitCode;
        }
        catch (OperationCanceledException)
        {
            // Success path — never let OperationCanceledException escape the int contract.
            return 0;
        }
        catch (Exception ex)
        {
            // Unknown fault: styled stderr, exit 1. Never throws for expected failures.
            ShowErrorMessage(ex.Message, CommandErrorKind.Fault, null);
            return 1;
        }
    }

    private void BuildCommandHierarchy()
    {
        _hierarchy.BuildCommandHierarchy();
        _rootCommand = _hierarchy.RootCommand;
        _allCommands.Clear();
        foreach (var (key, value) in _hierarchy.AllCommands)
        {
            _allCommands[key] = value;
        }
    }

    private void ValidateCommandHierarchy() => _hierarchy.ValidateCommandHierarchy();

    /// <summary>
    /// Pre-parse completion gateway. Intercepts <c>complete</c> and
    /// <c>completions script</c> directly off the raw args before any
    /// help/parse/registered-command dispatch, so user-registered commands
    /// with those names never run. Returns true when handled (exit 0).
    /// </summary>
    private bool TryHandleCompletionGateway(string[] args)
    {
        if (args.Length == 0)
            return false;

        if (string.Equals(args[0], "complete", StringComparison.Ordinal))
        {
            HandleCompleteProbe(args[1..]);
            return true;
        }

        if (string.Equals(args[0], "completions", StringComparison.Ordinal)
            && args.Length >= 3
            && string.Equals(args[1], "script", StringComparison.Ordinal))
        {
            return TryHandleCompletionScript(args[2]);
        }

        return false;
    }

    private void HandleCompleteProbe(string[] rest)
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
            var candidates = CompletionEngine.Complete(_rootCommand, probeArgs, partial);
            foreach (var candidate in candidates)
                ConsoleOutput.WriteLine(candidate);
        }
        catch
        {
            // Tolerant probe: malformed input yields no candidates, still exit 0.
        }
    }

    private bool TryHandleCompletionScript(string shell)
    {
        var exe = CommandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
        switch (shell.ToLowerInvariant())
        {
            case "bash":
                CompletionScriptWriter.WriteBash(ConsoleOutput, exe);
                return true;
            case "zsh":
                CompletionScriptWriter.WriteZsh(ConsoleOutput, exe);
                return true;
            case "pwsh":
            case "powershell":
                CompletionScriptWriter.WritePwsh(ConsoleOutput, exe);
                return true;
            case "fish":
                CompletionScriptWriter.WriteFish(ConsoleOutput, exe);
                return true;
            default:
                return false;
        }
    }

    private static (string[] Args, string Partial) SplitCompletionPrefix(string commandline, int position)
    {
        var prefix = commandline[..position];
        var tokens = TokenizeCompletionPrefix(prefix);
        if (tokens.Count == 0)
            return ([], string.Empty);

        // First token is the executable name; the engine probes the rest.
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

    private ParseResult ParseCommandLine(string[] args) =>
        _parser.ParseCommandLine(GetRootCommandOrThrow(), args);

    private SubCommandInfo GetRootCommandOrThrow() =>
        _rootCommand ?? throw new InvalidOperationException("Command hierarchy has not been built.");

    private void ValidateRequiredParameters(ParseResult result) => _validator.ValidateRequiredParameters(result);

    private void SetCommandValues(ParseResult result) => _binder.SetCommandValues(result);

    private Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken) =>
        _executor.ExecuteCommand(commandInfo, cancellationToken);

    #region Help and Version Methods

    private static bool ShouldShowGlobalHelp(string[] args) => HelpVersionGateway.ShouldShowGlobalHelp(args);

    private void ShowGlobalHelp() => _helpGateway.ShowGlobalHelp(_rootCommand, _allCommands);

    private void ShowCommandHelp(SubCommandInfo commandInfo) => _helpGateway.ShowCommandHelp(commandInfo, _rootCommand, _allCommands);

    private void ShowVersion() => _helpGateway.ShowVersion();

    /// <summary>
    /// Shows a styled error message with helpful footer information
    /// </summary>
    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName) => _helpGateway.ShowErrorMessage(message, kind, commandName);

    #endregion
}

/// <summary>
/// Result of parsing command line arguments
/// </summary>
internal class ParseResult
{
    public SubCommandInfo TargetCommand { get; set; } = null!;
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public Dictionary<SubCommandOptionInfo, List<string>> OptionValues { get; set; } = [];
    public Dictionary<SubCommandArgumentInfo, List<string>> ArgumentValues { get; set; } = [];

    /// <summary>
    /// Adds an option value to the parse result
    /// </summary>
    internal void AddOptionValue(SubCommandOptionInfo option, string? value)
    {
        if (!OptionValues.ContainsKey(option))
            OptionValues[option] = [];

        if (value != null)
            OptionValues[option].Add(value);
    }

    /// <summary>
    /// Canonical key for one logical option across global-copy identities:
    /// long name, then short name, then property name (compared case-insensitively).
    /// </summary>
    internal static string GetCanonicalOptionKey(SubCommandOptionInfo option) =>
        option.LongName ?? option.ShortName?.ToString() ?? option.Property.Name;

    /// <summary>
    /// Merged values for one logical option across all copy identities.
    /// The merged list decides CLI-wins: non-empty means a value is present
    /// regardless of which copy identity holds it.
    /// </summary>
    internal bool TryGetMergedOptionValues(SubCommandOptionInfo option, out List<string> values)
    {
        var key = GetCanonicalOptionKey(option);
        values = [];
        foreach (var (storedOption, storedValues) in OptionValues)
        {
            if (string.Equals(GetCanonicalOptionKey(storedOption), key, StringComparison.OrdinalIgnoreCase))
                values.AddRange(storedValues);
        }
        return values.Count != 0;
    }
}
