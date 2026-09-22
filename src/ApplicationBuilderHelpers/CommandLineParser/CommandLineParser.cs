using ApplicationBuilderHelpers.Exceptions;
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
/// Thin orchestrator over internal collaborators (mechanical splits, no behavior change).
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
    private readonly CompletionGateway _completionGateway;

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
        _completionGateway = new CompletionGateway(CommandBuilder, ConsoleOutput);
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
            if (_completionGateway.TryHandle(_rootCommand, args, out var completionExitCode))
                return completionExitCode;

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

            // Step 7: Validate required options and arguments, then collect
            // binding errors (#496 aggregation + #483 help precedence + #509
            // help-beats-missing): missing errors are suppressed when help was
            // requested so help-with-values renders at Step 7b, while binding
            // errors still collect through the same conversion pipeline, join
            // with newlines, exit 2 and keep per-line message formats unchanged.
            // Parse path errors (unknown option/command, duplicate,
            // RequiresSubcommand) stay fail-fast. Invalid values beat
            // help-with-values (#483 x #509): binding collection runs even
            // when help was requested (bare-ledger keys skipped to preserve
            // the --config --help carve-out), while missing errors yield to
            // help per #509 (help always wins over missing required).
            ValidateAndBindParameters(parseResult);

            // Step 7b: Handle command help when values were collected
            if (parseResult.ShowHelp)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            // Step 8: Bind the already-validated values onto the command instance.
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
            // #509: thread whether the failing invocation already requested
            // help so the footer can suppress the circular --help hint.
            // RequestedHelp mirrors the parser (stops at --, covers -h
            // clusters); footer-only, exit codes unaffected.
            ShowErrorMessage(ex.Message, ex.Kind, ex.CommandName, HelpVersionGateway.RequestedHelp(args));
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
            // No help-signal threading here by design: Fault keeps the
            // --help-only single-sentence footer regardless (#509 carve-out).
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

    private ParseResult ParseCommandLine(string[] args) =>
        _parser.ParseCommandLine(GetRootCommandOrThrow(), args);

    private SubCommandInfo GetRootCommandOrThrow() =>
        _rootCommand ?? throw new InvalidOperationException("Command hierarchy has not been built.");

    private void ValidateAndBindParameters(ParseResult result)
    {
        // Dry runs only: neither collector mutates bound state (the missing
        // pass may still inject env fallback values into the parse result, but
        // that is the same idempotent merge the throwing path performed before
        // binding; no success-path command instance is touched). Collect-all so
        // a missing parameter no longer masks an invalid value. Binding
        // collection runs even when help was requested (#483 x #509: invalid
        // beats help-with-values), skipping only bare-ledger keys to preserve
        // the --config --help carve-out, while required errors yield to help
        // per #509 (suppressed inside the validator when ShowHelp is set).
        var missingErrors = _validator.CollectRequiredErrors(result);
        var bindingErrors = _binder.CollectBindingErrors(result, skipBareWhenHelpRequested: true);

        var allErrors = new List<string>(missingErrors.Count + bindingErrors.Count);
        allErrors.AddRange(missingErrors);
        allErrors.AddRange(bindingErrors);
        if (allErrors.Count == 0)
            return;

        // Mixed usage failure stays a usage error (exit 2). Both kinds already
        // share the same per-command footer; InvalidValue names the dominant
        // (binding) failure while MissingRequired-only paths keep their kind.
        throw new CommandException(
            string.Join(Environment.NewLine, allErrors),
            2,
            bindingErrors.Count == 0 ? CommandErrorKind.MissingRequired : CommandErrorKind.InvalidValue,
            result.TargetCommand.FullCommandName);
    }

    private void SetCommandValues(ParseResult result)
    {
        try
        {
            _binder.SetCommandValues(result);
        }
        catch (CommandException ex) when (ex.CommandName is null)
        {
            throw new CommandException(ex.Message, ex.ExitCode, ex.Kind, result.TargetCommand.FullCommandName);
        }
    }

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
    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName, bool showHelpRequested = false) => _helpGateway.ShowErrorMessage(message, kind, commandName, showHelpRequested);

    #endregion
}
