using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
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

    internal CommandLineParser(ApplicationBuilder applicationBuilder, ConsoleOutput? consoleOutput = null)
    {
        ApplicationBuilder = applicationBuilder;
        CommandBuilder = applicationBuilder;
        CommandTypeParserCollection = applicationBuilder;
        ApplicationDependencyCollection = applicationBuilder;
        ConsoleOutput = consoleOutput ?? new ConsoleOutput();

        _hierarchy = new CommandHierarchyBuilder(CommandBuilder, CommandTypeParserCollection);
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

            // Step 3: Handle basic help/version before parsing
            if (ShouldShowGlobalHelp(args))
            {
                ShowGlobalHelp();
                return 0;
            }

            if (ShouldShowVersion(args))
            {
                ShowVersion();
                return 0;
            }

            // Step 4: Parse command line arguments
            var parseResult = ParseCommandLine(args);

            // Step 5: Prepare application builder with dependencies
            parseResult.TargetCommand.Command?.CommandPreparation(ApplicationBuilder);

            // Step 6: Handle command-specific help
            if (parseResult.ShowHelp)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            // Step 7: Validate required options and arguments
            ValidateRequiredParameters(parseResult);

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
            ShowErrorMessage(ex.Message);
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

    private void ValidateRequiredParameters(ParseResult result) => _validator.ValidateRequiredParameters(result);

    private void SetCommandValues(ParseResult result) => _binder.SetCommandValues(result);

    private Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken) =>
        _executor.ExecuteCommand(commandInfo, cancellationToken);

    #region Help and Version Methods

    private static bool ShouldShowGlobalHelp(string[] args) => HelpVersionGateway.ShouldShowGlobalHelp(args);

    private static bool ShouldShowVersion(string[] args) => HelpVersionGateway.ShouldShowVersion(args);

    private void ShowGlobalHelp() => _helpGateway.ShowGlobalHelp(_rootCommand, _allCommands);

    private void ShowCommandHelp(SubCommandInfo commandInfo) => _helpGateway.ShowCommandHelp(commandInfo, _rootCommand, _allCommands);

    private void ShowVersion() => _helpGateway.ShowVersion();

    /// <summary>
    /// Shows a styled error message with helpful footer information
    /// </summary>
    private void ShowErrorMessage(string message) => _helpGateway.ShowErrorMessage(message);

    #endregion
}

/// <summary>
/// Result of parsing command line arguments
/// </summary>
internal class ParseResult
{
    public SubCommandInfo TargetCommand { get; set; } = null!;
    public bool ShowHelp { get; set; }
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
}
