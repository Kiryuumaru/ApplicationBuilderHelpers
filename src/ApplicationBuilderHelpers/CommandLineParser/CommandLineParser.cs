using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Owns the run order: hierarchy → completion → help → parse → version → validate/bind → execute.</summary>
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

    /// <summary>Creates the pipeline owner with its stage collaborators.</summary>
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

    /// <summary>Runs prep → build → gates → parse → bind → execute.</summary>
    /// <remarks>Catch chain is ordered: ExternalCancel → canceled; CommandException → Kind + exit; outer-requested OCE → canceled; bare OCE → 0; generic → exit 1, never stack.</remarks>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            args = ResponseFileExpander.Expand(args);
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

            var forgiveness = HelpVersionGateway.DecideValidationForgiveness(parseResult, args);
            if (forgiveness == HelpVersionGateway.HelpVersionForgiveness.ForgiveHelp)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            if (forgiveness == HelpVersionGateway.HelpVersionForgiveness.ForgiveVersion)
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

    /// <summary>Builds the hierarchy snapshot and caches the root plus the flat lookup.</summary>
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

    /// <summary>Runs hierarchy validation.</summary>
    private void ValidateCommandHierarchy() => _hierarchy.ValidateCommandHierarchy();

    /// <summary>Parses argv into the target command plus option/argument occurrences.</summary>
    private ParseResult ParseCommandLine(string[] args) =>
        _parser.ParseCommandLine(GetRootCommandOrThrow(), args);

    /// <summary>Returns the built root; throws when the hierarchy was never built.</summary>
    private SubCommandInfo GetRootCommandOrThrow() =>
        _rootCommand ?? throw new InvalidOperationException("Command hierarchy has not been built.");

    /// <summary>Merges required-missing and binding errors into one usage error (exit 2).</summary>
    private void ValidateAndBindParameters(ParseResult result)
    {
        List<string> duplicateErrors = [];
        if (CommandBuilder.RejectDuplicateOptions)
            duplicateErrors = _validator.CollectDuplicateErrors(result);
        var missingErrors = _validator.CollectRequiredErrors(result);
        var bindingErrors = _binder.CollectBindingErrors(result);

        var allErrors = new List<string>(duplicateErrors.Count + missingErrors.Count + bindingErrors.Count);
        allErrors.AddRange(duplicateErrors);
        allErrors.AddRange(missingErrors);
        allErrors.AddRange(bindingErrors);
        if (allErrors.Count == 0)
            return;

        var kind = bindingErrors.Count != 0
            ? CommandErrorKind.InvalidValue
            : duplicateErrors.Count != 0
                ? CommandErrorKind.DuplicateOption
                : CommandErrorKind.MissingRequired;
        throw new CommandException(
            string.Join(Environment.NewLine, allErrors),
            2,
            kind,
            result.TargetCommand.FullCommandName);
    }

    /// <summary>Applies bound values onto the command instance.</summary>
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

    /// <summary>Delegates execution to the execute stage.</summary>
    private Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken) =>
        _executor.ExecuteCommand(commandInfo, cancellationToken);

    #region Help and Version Methods

    private static bool ShouldShowGlobalHelp(string[] args) => HelpVersionGateway.ShouldShowGlobalHelp(args);

    private void ShowGlobalHelp() => _helpGateway.ShowGlobalHelp(_rootCommand, _allCommands);

    private void ShowCommandHelp(SubCommandInfo commandInfo) => _helpGateway.ShowCommandHelp(commandInfo, _rootCommand, _allCommands);

    private void ShowVersion() => _helpGateway.ShowVersion();

    /// <summary>Shows an error message with footer information.</summary>
    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName, bool showHelpRequested = false) => _helpGateway.ShowErrorMessage(message, kind, commandName, showHelpRequested);

    #endregion
}
