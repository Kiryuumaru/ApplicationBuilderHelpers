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
/// Coordinates hierarchy building, argument parsing, validation, and value binding.
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
            foreach (var dependency in ApplicationDependencyCollection.ApplicationDependencies)
            {
                dependency.CommandPreparation(ApplicationBuilder);
            }

            BuildCommandHierarchy();
            ValidateCommandHierarchy();

            if (_completionGateway.TryHandle(_rootCommand, args, out var completionExitCode))
                return completionExitCode;

            if (ShouldShowGlobalHelp(args))
            {
                ShowGlobalHelp();
                return 0;
            }

            var parseResult = ParseCommandLine(args);

            if (parseResult.ShowVersion)
            {
                ShowVersion();
                return 0;
            }

            parseResult.TargetCommand.Command?.CommandPreparation(ApplicationBuilder);

            if (parseResult.ShowHelp && parseResult.OptionValues.Count == 0 && parseResult.ArgumentValues.Count == 0)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            ValidateAndBindParameters(parseResult);

            if (parseResult.ShowHelp)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            SetCommandValues(parseResult);

            await ExecuteCommand(parseResult.TargetCommand, cancellationToken);
            return 0;
        }
        catch (CommandExecutor.ExternalCancellationException)
        {
            return CommandExecutor.CanceledExitCode;
        }
        catch (CommandException ex)
        {
            ShowErrorMessage(ex.Message, ex.Kind, ex.CommandName, HelpVersionGateway.RequestedHelp(args));
            return ex.ExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CommandExecutor.CanceledExitCode;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
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
        var missingErrors = _validator.CollectRequiredErrors(result);
        var bindingErrors = _binder.CollectBindingErrors(result, skipBareWhenHelpRequested: true);

        var allErrors = new List<string>(missingErrors.Count + bindingErrors.Count);
        allErrors.AddRange(missingErrors);
        allErrors.AddRange(bindingErrors);
        if (allErrors.Count == 0)
            return;

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
    /// Shows an error message with footer information.
    /// </summary>
    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName, bool showHelpRequested = false) => _helpGateway.ShowErrorMessage(message, kind, commandName, showHelpRequested);

    #endregion
}
