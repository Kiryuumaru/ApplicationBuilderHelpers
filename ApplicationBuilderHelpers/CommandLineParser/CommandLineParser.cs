using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Common;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Command line parser that supports hierarchical subcommands with option/argument inheritance.
/// Implements the processing order: Build hierarchy first, then parse arguments.
/// </summary>
internal class CommandLineParser(ApplicationBuilder applicationBuilder)
{
    public ApplicationBuilder ApplicationBuilder { get; } = applicationBuilder;
    public ICommandBuilder CommandBuilder { get; } = applicationBuilder;
    public ICommandTypeParserCollection CommandTypeParserCollection { get; } = applicationBuilder;
    public IApplicationDependencyCollection ApplicationDependencyCollection { get; } = applicationBuilder;

    private SubCommandInfo? _rootCommand;
    private readonly Dictionary<string, SubCommandInfo> _allCommands = [];

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

            // Step 9: Execute the command
            await ExecuteCommand(parseResult.TargetCommand, cancellationToken);
            return 0;
        }
        catch (CommandException ex)
        {
            ShowErrorMessage(ex.Message);
            return ex.ExitCode;
        }
    }

    /// <summary>
    /// Builds the command hierarchy from registered commands
    /// </summary>
    private void BuildCommandHierarchy()
    {
        _rootCommand = new SubCommandInfo
        {
            CommandParts = [],
            // Use auto-detection for null ExecutableDescription
            Description = CommandBuilder.ExecutableDescription ?? AssemblyHelpers.GetAutoDetectedExecutableDescription()
        };

        // Process all commands and build hierarchy
        foreach (var typedCommandHolder in CommandBuilder.Commands)
        {
            _ = typedCommandHolder.CommandType.GetCustomAttribute<CommandAttribute>();

            // Create SubCommandInfo for this command
            var subCommandInfo = SubCommandInfo.FromCommand(typedCommandHolder.CommandType, typedCommandHolder.Command);

            // Extract options and arguments - pass the type parser collection
            subCommandInfo.Options = SubCommandOptionInfo.FromCommandType(typedCommandHolder.CommandType, subCommandInfo);
            subCommandInfo.Arguments = SubCommandArgumentInfo.FromCommandType(typedCommandHolder.CommandType, subCommandInfo);

            // Insert into hierarchy
            InsertCommandIntoHierarchy(subCommandInfo);
        }

        // After building hierarchy, determine global options
        DetermineGlobalOptions();
    }

    /// <summary>
    /// Inserts a command into the appropriate place in the hierarchy
    /// </summary>
    private void InsertCommandIntoHierarchy(SubCommandInfo commandInfo)
    {
        if (commandInfo.CommandParts.Length == 0)
        {
            // Root command - add ALL options from the root command
            if (_rootCommand!.HasImplementation)
                throw new InvalidOperationException("Cannot have more than one root command");
            
            _rootCommand.Command = commandInfo.Command;
            
            // Add ALL options from the root command (both BaseCommand and MainCommand options)
            _rootCommand.Options.AddRange(commandInfo.Options);
            _rootCommand.Arguments.AddRange(commandInfo.Arguments);
            return;
        }

        // Navigate to the correct parent and create intermediate commands if needed
        var current = _rootCommand!;
        
        for (int i = 0; i < commandInfo.CommandParts.Length; i++)
        {
            var part = commandInfo.CommandParts[i];
            
            if (i == commandInfo.CommandParts.Length - 1)
            {
                // This is the final part - add the actual command
                current.AddChild(commandInfo);
                _allCommands[commandInfo.FullCommandName] = commandInfo;
            }
            else
            {
                // Intermediate part - create parent command if it doesn't exist
                var child = current.FindChild(part);
                if (child == null)
                {
                    var intermediateParts = commandInfo.CommandParts[0..(i + 1)];
                    child = CreateIntermediateCommand(intermediateParts, commandInfo.Command!.GetType());
                    current.AddChild(child);
                    _allCommands[child.FullCommandName] = child;
                }
                current = child;
            }
        }
    }

    /// <summary>
    /// Creates an intermediate command, checking for abstract base class information
    /// </summary>
    private SubCommandInfo CreateIntermediateCommand(string[] commandParts, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type leafCommandType)
    {
        // Look for abstract base class that matches this intermediate command path
        var intermediateCommandInfo = FindAbstractBaseCommandInfo(commandParts, leafCommandType);
        
        SubCommandInfo result;
        if (intermediateCommandInfo != null)
        {
            // Use information from the abstract base class
            result = new SubCommandInfo
            {
                CommandParts = commandParts,
                Description = intermediateCommandInfo.Description,
                Options = intermediateCommandInfo.Options,
                Arguments = intermediateCommandInfo.Arguments
            };
        }
        else
        {
            // Fallback to generic description
            result = new SubCommandInfo
            {
                CommandParts = commandParts,
                Description = $"Commands for {commandParts[^1]}"
            };
        }

        // Ensure intermediate commands inherit global options from root
        if (_rootCommand != null)
        {
            foreach (var globalOption in _rootCommand.Options.Where(o => o.IsGlobal))
            {
                // Only add if not already present
                if (!result.Options.Any(o => o.GetDisplayName() == globalOption.GetDisplayName()))
                {
                    result.Options.Add(globalOption);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Searches the inheritance hierarchy for an abstract base class with Command attribute matching the path
    /// </summary>
    private SubCommandInfo? FindAbstractBaseCommandInfo(string[] commandParts, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type leafCommandType)
    {
        var currentType = leafCommandType.BaseType;
        var targetCommandName = string.Join(" ", commandParts);
        
        while (currentType != null && currentType != typeof(object))
        {
            var commandAttr = currentType.GetCustomAttribute<CommandAttribute>();
            if (commandAttr != null && 
                currentType.IsAbstract && 
                commandAttr.Term == targetCommandName)
            {
                // Found matching abstract base class
                var baseCommandInfo = new SubCommandInfo
                {
                    CommandParts = commandParts,
                    Description = commandAttr.Description
                };

                // For AOT compatibility, we need to avoid using methods that require specific DynamicallyAccessedMembers
                // on types that don't have those annotations. Instead, we'll extract options and arguments manually.
                baseCommandInfo.Options = ExtractOptionsFromTypeManually(currentType, baseCommandInfo);
                baseCommandInfo.Arguments = ExtractArgumentsFromTypeManually(currentType, baseCommandInfo);

                return baseCommandInfo;
            }
            currentType = currentType.BaseType;
        }
        
        return null;
    }

    /// <summary>
    /// Manually extracts options from a type to avoid AOT warnings
    /// </summary>
    private List<SubCommandOptionInfo> ExtractOptionsFromTypeManually([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand)
    {
        var options = new List<SubCommandOptionInfo>();
        var properties = commandType.GetProperties(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        foreach (var property in properties)
        {
            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>();
            if (optionAttr != null)
            {
                var optionInfo = SubCommandOptionInfo.FromProperty(property, optionAttr, ownerCommand, CommandTypeParserCollection);
                options.Add(optionInfo);
            }
        }

        return options;
    }

    /// <summary>
    /// Manually extracts arguments from a type to avoid AOT warnings
    /// </summary>
    private static List<SubCommandArgumentInfo> ExtractArgumentsFromTypeManually([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand)
    {
        var arguments = new List<SubCommandArgumentInfo>();
        var properties = commandType.GetProperties(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        foreach (var property in properties)
        {
            var argumentAttr = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (argumentAttr != null)
            {
                var argumentInfo = SubCommandArgumentInfo.FromProperty(property, argumentAttr, ownerCommand);
                arguments.Add(argumentInfo);
            }
        }

        return [.. arguments.OrderBy(a => a.Position)];
    }

    /// <summary>
    /// Determines which options should be global based on commonality across commands
    /// </summary>
    private void DetermineGlobalOptions()
    {
        // Get all concrete commands (those with implementations)
        var concreteCommands = _allCommands.Values.Where(c => c.HasImplementation).ToList();
        
        if (concreteCommands.Count == 0)
            return;

        // Find options that appear in ALL commands with the same signature
        var optionsBySignature = new Dictionary<string, List<(SubCommandOptionInfo option, SubCommandInfo command)>>();

        foreach (var command in concreteCommands)
        {
            foreach (var option in command.Options)
            {
                var signature = option.GetDisplayName();
                if (!optionsBySignature.ContainsKey(signature))
                    optionsBySignature[signature] = [];
                optionsBySignature[signature].Add((option, command));
            }
        }

        // Mark options as global ONLY if they appear in ALL commands and are identical
        foreach (var (signature, optionInfos) in optionsBySignature)
        {
            // Must appear in ALL concrete commands to be considered global
            if (optionInfos.Count == concreteCommands.Count)
            {
                // Check if all options have identical signatures (name, term, short term, type)
                var firstOption = optionInfos[0].option;
                var allIdentical = optionInfos.All(oi => 
                    oi.option.PropertyType == firstOption.PropertyType &&
                    oi.option.IsRequired == firstOption.IsRequired &&
                    oi.option.ShortName == firstOption.ShortName &&
                    oi.option.LongName == firstOption.LongName &&
                    ArraysEqual(oi.option.ValidValues, firstOption.ValidValues));

                if (allIdentical)
                {
                    // These options appear in ALL commands with identical signatures
                    foreach (var (option, _) in optionInfos)
                    {
                        option.IsGlobal = true;
                        option.IsInherited = true;
                    }

                    // Add one instance to root command
                    if (!_rootCommand!.Options.Any(o => o.GetDisplayName() == signature))
                    {
                        var globalOption = CreateGlobalOptionCopy(firstOption);
                        globalOption.OwnerCommand = _rootCommand;
                        _rootCommand.Options.Add(globalOption);
                    }
                }
            }
        }

        // Add built-in global options (help only - version is not global)
        AddBuiltInGlobalOptions();
    }

    /// <summary>
    /// Compares two arrays for equality
    /// </summary>
    private static bool ArraysEqual(object[]? arr1, object[]? arr2)
    {
        if (arr1 == null && arr2 == null) return true;
        if (arr1 == null || arr2 == null) return false;
        if (arr1.Length != arr2.Length) return false;
        
        for (int i = 0; i < arr1.Length; i++)
        {
            if (!Equals(arr1[i], arr2[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// Adds built-in global options (help only) to all commands
    /// </summary>
    private void AddBuiltInGlobalOptions()
    {
        // Create a dummy property to satisfy the Property requirement
        var dummyProperty = typeof(SubCommandOptionInfo).GetProperty(nameof(SubCommandOptionInfo.Property))!;

        // Add help option as a true global option (appears in all commands)
        var helpOption = new SubCommandOptionInfo
        {
            ShortName = 'h',
            LongName = "help",
            Description = "Show help information",
            IsGlobal = true,
            IsInherited = true,
            OwnerCommand = _rootCommand,
            Property = dummyProperty,
            // Set PropertyType directly to avoid AOT warnings
            PropertyType = typeof(bool)
        };

        if (!_rootCommand!.Options.Any(o => o.GetDisplayName() == helpOption.GetDisplayName()))
        {
            _rootCommand.Options.Add(helpOption);
        }

        // Mark help as global in all commands
        foreach (var command in _allCommands.Values)
        {
            var existingHelpOption = command.Options.FirstOrDefault(o => o.LongName == "help");
            if (existingHelpOption == null)
            {
                var globalHelpOption = CreateGlobalOptionCopy(helpOption);
                globalHelpOption.OwnerCommand = command;
                command.Options.Add(globalHelpOption);
            }
            else
            {
                existingHelpOption.IsGlobal = true;
                existingHelpOption.IsInherited = true;
            }
        }
    }

    /// <summary>
    /// Validates the built command hierarchy
    /// </summary>
    private void ValidateCommandHierarchy()
    {
        _rootCommand?.Validate();

        // Additional validation: ensure all commands have either implementation or children
        foreach (var command in _allCommands.Values)
        {
            if (!command.HasImplementation && command.IsLeaf)
            {
                throw new InvalidOperationException(
                    $"Command '{command.FullCommandName}' has no implementation and no subcommands");
            }
        }
    }

    /// <summary>
    /// Parses the command line arguments against the built hierarchy
    /// </summary>
    private ParseResult ParseCommandLine(string[] args)
    {
        var result = new ParseResult();
        var argIndex = 0;

        // Find the target command by consuming command parts
        result.TargetCommand = _rootCommand!;
        
        while (argIndex < args.Length && !args[argIndex].StartsWith('-'))
        {
            var child = result.TargetCommand.FindChild(args[argIndex]);
            if (child != null)
            {
                result.TargetCommand = child;
                argIndex++;
            }
            else
            {
                break; // No more matching subcommands
            }
        }

        // Check for help flag in remaining arguments before validating
        var hasHelpFlag = args.Skip(argIndex).Any(arg => arg == "--help" || arg == "-h");
        if (hasHelpFlag)
        {
            result.ShowHelp = true;
            return result;
        }

        // If we ended up on a command without implementation, check if it requires subcommands
        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // This is an abstract command that requires a subcommand
            var availableSubcommands = string.Join(", ", result.TargetCommand.Children.Keys.OrderBy(k => k));
            var commandName = string.IsNullOrEmpty(result.TargetCommand.FullCommandName) ? "" : result.TargetCommand.FullCommandName;
            throw new CommandException($"'{commandName}' requires a subcommand. Available subcommands: {availableSubcommands}", 1);
        }

        if (!result.TargetCommand.HasImplementation)
        {
            throw new CommandException($"No implementation found for command '{result.TargetCommand.FullCommandName}'", 1);
        }

        // Parse remaining arguments as options and arguments
        ParseOptionsAndArguments(args, argIndex, result);

        return result;
    }

    /// <summary>
    /// Parses options and arguments from the command line
    /// </summary>
    private static void ParseOptionsAndArguments(string[] args, int startIndex, ParseResult result)
    {
        var allOptions = result.TargetCommand.AllOptions;
        var allArguments = result.TargetCommand.AllArguments;
        var argumentValues = new List<string>();
        var argumentIndex = 0;

        for (int i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];

            // Check for help flag
            if (arg == "--help" || arg == "-h")
            {
                result.ShowHelp = true;
                continue;
            }

            // Check if this is an option
            var matchedOption = allOptions.FirstOrDefault(o => o.MatchesArgument(arg));
            if (matchedOption != null)
            {
                var nextArg = i + 1 < args.Length ? args[i + 1] : null;
                var value = matchedOption.ExtractValue(arg, nextArg);
                
                // If value came from next argument, skip it
                if (value == nextArg && !(nextArg?.StartsWith('-') == true && !IsNumericValue(nextArg)))
                    i++;

                AddOptionValue(result, matchedOption, value);
            }
            else if (arg.StartsWith('-') && !IsNumericValue(arg))
            {
                throw new CommandException($"Unknown option: {arg}", 1);
            }
            else
            {
                // This is a positional argument
                argumentValues.Add(arg);
            }
        }

        // Assign argument values to their respective arguments
        foreach (var argumentValue in argumentValues)
        {
            var targetArgument = allArguments.FirstOrDefault(a => a.CanAcceptValueAtPosition(argumentIndex));
            if (targetArgument != null)
            {
                AddArgumentValue(result, targetArgument, argumentValue);
                if (!targetArgument.IsArray)
                    argumentIndex++;
            }
            else
            {
                throw new CommandException($"No command found for '{argumentValue}'", 1);
            }
        }
    }

    /// <summary>
    /// Adds an option value to the parse result
    /// </summary>
    private static void AddOptionValue(ParseResult result, SubCommandOptionInfo option, string? value)
    {
        if (!result.OptionValues.ContainsKey(option))
            result.OptionValues[option] = [];

        if (value != null)
            result.OptionValues[option].Add(value);
    }

    /// <summary>
    /// Adds an argument value to the parse result
    /// </summary>
    private static void AddArgumentValue(ParseResult result, SubCommandArgumentInfo argument, string value)
    {
        if (!result.ArgumentValues.ContainsKey(argument))
            result.ArgumentValues[argument] = [];

        result.ArgumentValues[argument].Add(value);
    }

    /// <summary>
    /// Validates that all required parameters are provided
    /// </summary>
    private static void ValidateRequiredParameters(ParseResult result)
    {
        // Check required options
        foreach (var option in result.TargetCommand.AllOptions.Where(o => o.IsRequired))
        {
            if (!result.OptionValues.TryGetValue(option, out List<string>? value) || value.Count == 0)
            {
                // Check for environment variable fallback
                if (!string.IsNullOrEmpty(option.EnvironmentVariable))
                {
                    var envValue = Environment.GetEnvironmentVariable(option.EnvironmentVariable);
                    if (!string.IsNullOrEmpty(envValue))
                    {
                        AddOptionValue(result, option, envValue);
                        continue;
                    }
                }

                throw new CommandException($"Missing required option: {option.GetDisplayName()}", 1);
            }
        }

        // Check required arguments
        foreach (var argument in result.TargetCommand.AllArguments.Where(a => a.IsRequired))
        {
            if (!result.ArgumentValues.TryGetValue(argument, out List<string>? value) || value.Count == 0)
            {
                throw new CommandException($"Missing required argument: {argument.DisplayName}", 1);
            }
        }
    }

    /// <summary>
    /// Sets the parsed values on the command instance properties
    /// </summary>
    private void SetCommandValues(ParseResult result)
    {
        var command = result.TargetCommand.Command!;

        // First, check for environment variable fallback for all options with environment variables
        foreach (var option in result.TargetCommand.AllOptions.Where(o => !string.IsNullOrEmpty(o.EnvironmentVariable)))
        {
            // Only apply environment variable if option was not explicitly set
            if (!result.OptionValues.TryGetValue(option, out List<string>? value) || value.Count == 0)
            {
                if (option.EnvironmentVariable is string envVar &&
                    Environment.GetEnvironmentVariable(envVar) is string envValue &&
                    !string.IsNullOrEmpty(envValue))
                {
                    AddOptionValue(result, option, envValue);
                }
            }
        }

        // Set option values
        foreach (var (option, values) in result.OptionValues)
        {
            if (values.Count == 0) continue;

            object? propertyValue;

            if (option.IsArray)
            {
                var elementType = option.ElementType!;
                var array = CreateTypedArray(elementType, values.Count);
                
                for (int i = 0; i < values.Count; i++)
                {
                    // Validate string value first if ValidValues are specified
                    if (option.ValidValues?.Length > 0)
                    {
                        ValidateStringValue(values[i], option);
                    }
                    
                    var convertedValue = ConvertValue(values[i], elementType, option.IsCaseSensitive);
                    array.SetValue(convertedValue, i);
                }
                
                propertyValue = array;
            }
            else
            {
                // Validate string value first if ValidValues are specified
                if (option.ValidValues?.Length > 0)
                {
                    ValidateStringValue(values[0], option);
                }
                
                propertyValue = ConvertValue(values[0], option.PropertyType, option.IsCaseSensitive);
            }

            // Note: We don't call option.ValidateValue here anymore for ValidValues validation
            // since we already validated the string value above. ValidateValue is now only used
            // for required field validation in ValidateRequiredParameters.

            option.Property.SetValue(command, propertyValue);
        }

        // Set argument values
        foreach (var (argument, values) in result.ArgumentValues)
        {
            if (values.Count == 0) continue;

            object? propertyValue;

            if (argument.IsArray)
            {
                var elementType = argument.ElementType!;
                var array = CreateTypedArray(elementType, values.Count);
                
                for (int i = 0; i < values.Count; i++)
                {
                    var convertedValue = argument.ConvertValue(values[i], CommandTypeParserCollection);
                    array.SetValue(convertedValue, i);
                }
                
                propertyValue = array;
            }
            else
            {
                propertyValue = argument.ConvertValue(values[0], CommandTypeParserCollection);
            }

            argument.Property.SetValue(command, propertyValue);
        }
    }

    /// <summary>
    /// Validates a string value against the option's ValidValues before conversion
    /// </summary>
    private static void ValidateStringValue(string value, SubCommandOptionInfo option)
    {
        if (option.ValidValues?.Length > 0)
        {
            var comparisonType = option.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            var isValid = option.ValidValues.Any(validValue => 
                string.Equals(validValue?.ToString(), value, comparisonType));

            if (!isValid)
            {
                var validValuesString = string.Join(", ", option.ValidValues.Select(v => v?.ToString()));
                throw new CommandException(
                    $"Value '{value}' is not valid for option '--{option.LongName ?? option.ShortName?.ToString()}'. " +
                    $"Must be one of: {validValuesString}", 1);
            }
        }
    }

    /// <summary>
    /// Converts a string value to the specified type
    /// </summary>
    private object? ConvertValue(string? value, Type targetType, bool isCaseSensitive)
    {
        if (value == null) return null;

        if (CommandTypeParserCollection.TypeParsers.TryGetValue(targetType, out var parser))
        {
            var result = parser.Parse(value, out var error);
            if (error != null)
                throw new CommandException(error, 1);
            return result;
        }

        if (targetType == typeof(string))
            return value;

        if (targetType.IsEnum && Enum.TryParse(targetType, value, !isCaseSensitive, out var typedVal))
            return typedVal;

        // Handle nullable types
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType)!;
            return ConvertValue(value, underlyingType, isCaseSensitive);
        }

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception)
        {
            throw new CommandException($"Invalid format for value '{value}' of type {targetType.FullName}", 1);
        }
    }

    /// <summary>
    /// Creates a typed array for AOT compatibility
    /// </summary>
    private Array CreateTypedArray(Type elementType, int length)
    {
        // First try to use the registered type parser to create the array
        if (CommandTypeParserCollection.TypeParsers.TryGetValue(elementType, out var parser))
        {
            try
            {
                return parser.CreateTypedArray(length);
            }
            catch
            {
                // If the type parser fails, fall back to manual creation
            }
        }

        // For other types, create a generic object array to maintain AOT compatibility
        return new object?[length];
    }

    /// <summary>
    /// Executes the target command
    /// </summary>
    private async Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken)
    {
        var command = commandInfo.Command!;

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var applicationBuilder = await command.ApplicationBuilderInternal(cancellationTokenSource.Token);
        LifetimeGlobalService lifetimeGlobalService = new();

        cancellationTokenSource.Token.Register(lifetimeGlobalService.CancellationTokenSource.Cancel);
        Console.CancelKeyPress += (sender, e) =>
        {
            cancellationTokenSource.Cancel();
            e.Cancel = true;
        };

        applicationBuilder.Services.AddSingleton(lifetimeGlobalService);
        applicationBuilder.Services.AddScoped<LifetimeService>();

        applicationBuilder.ApplicationDependencies.Add(command);
        foreach (var dependency in ApplicationDependencyCollection.ApplicationDependencies)
        {
            applicationBuilder.ApplicationDependencies.Add(dependency);
        }

        var applicationHost = applicationBuilder.BuildInternal();
        var commanRun = command.RunInternal(applicationHost, cancellationTokenSource);

        if (cancellationTokenSource.Token.IsCancellationRequested)
        {
            await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
            await lifetimeGlobalService.InvokeApplicationExitedCallbacksAsync();
            return;
        }

        try
        {
            await Task.WhenAll(
                Task.Run(async () => await commanRun, cancellationToken),
                Task.Run(async () =>
                {
                    int exitCode = await applicationHost.Run(cancellationTokenSource.Token);
                    if (exitCode != 0)
                    {
                        throw new CommandException($"Command '{commandInfo.FullCommandName}' exited with code {exitCode}", exitCode);
                    }
                }, cancellationTokenSource.Token),
                Task.Run(async () =>
                {
                    await cancellationTokenSource.Token.WhenCanceled();
                    await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
                }, cancellationTokenSource.Token));
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                throw; // Rethrow if cancellation was not requested by the caller
            }
        }
        finally
        {
            // Ensure we clean up the lifetime service
            await lifetimeGlobalService.InvokeApplicationExitedCallbacksAsync();
        }
    }

    /// <summary>
    /// Creates a copy of an option for global use
    /// </summary>
    private static SubCommandOptionInfo CreateGlobalOptionCopy(SubCommandOptionInfo original)
    {
        return new SubCommandOptionInfo
        {
            Property = original.Property,
            PropertyType = original.PropertyType,
            ShortName = original.ShortName,
            LongName = original.LongName,
            Description = original.Description,
            IsRequired = original.IsRequired,
            EnvironmentVariable = original.EnvironmentVariable,
            ValidValues = original.ValidValues,
            IsCaseSensitive = original.IsCaseSensitive,
            DefaultValue = original.DefaultValue,
            IsGlobal = true,
            IsInherited = true
        };
    }

    private static bool IsNumericValue(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('-') || value.Length < 2)
            return false;

        // Check if what follows the dash is a digit or decimal point
        var afterDash = value[1];
        if (!char.IsDigit(afterDash) && afterDash != '.')
            return false;

        // Try to parse as a double to confirm it's a valid numeric value
        return double.TryParse(value, out _);
    }

    #region Help and Version Methods

    private static bool ShouldShowGlobalHelp(string[] args)
    {
        // Only show global help if explicitly requested with --help/-h
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            return true;
        }
        
        // Never show global help for empty args - let ParseCommandLine handle it
        // This allows root commands to execute normally or show subcommand requirements
        return false;
    }

    private static bool ShouldShowVersion(string[] args)
    {
        return args.Contains("--version") || args.Contains("-V");
    }

    private void ShowGlobalHelp()
    {
        var helpFormatter = new HelpFormatter(CommandBuilder, _rootCommand, _allCommands);
        helpFormatter.ShowGlobalHelp();
    }

    private void ShowCommandHelp(SubCommandInfo commandInfo)
    {
        var helpFormatter = new HelpFormatter(CommandBuilder, _rootCommand, _allCommands);
        helpFormatter.ShowCommandHelp(commandInfo);
    }

    private void ShowVersion()
    {
        var version = CommandBuilder.ExecutableVersion ?? AssemblyHelpers.GetAutoDetectedVersion();
        Console.WriteLine(version);
    }

    /// <summary>
    /// Shows a styled error message with helpful footer information
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        var theme = CommandBuilder.Theme;
        // Use auto-detection for null ExecutableName
        var executableName = CommandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
        
        // Show the error message in red color if theme is available
        var errorColor = theme?.RequiredColor ?? ConsoleColor.Red;
        var originalColor = Console.ForegroundColor;
        
        try
        {
            Console.ForegroundColor = errorColor;
            Console.Error.WriteLine($"Error: {message}");
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }

        // Add helpful footer message based on error type
        Console.Error.WriteLine();
        
        if (message.Contains("requires a subcommand"))
        {
            // Extract command name if present for more specific help
            if (message.StartsWith('\'') && message.Contains('\''))
            {
                var commandName = message[1..message.IndexOf('\'', 1)];
                if (!string.IsNullOrEmpty(commandName))
                {
                    Console.Error.WriteLine($"Run '{executableName} {commandName} --help' to see available subcommands and options.");
                }
                else
                {
                    Console.Error.WriteLine($"Run '{executableName} --help' to see available commands and options.");
                }
            }
            else
            {
                Console.Error.WriteLine($"Run '{executableName} --help' to see available commands and options.");
            }
        }
        else if (message.Contains("Unknown option") || message.Contains("Missing required"))
        {
            Console.Error.WriteLine($"Run '{executableName} <command> --help' for more information on specific command options.");
        }
        else
        {
            Console.Error.WriteLine($"Run '{executableName} --help' for more information on available commands and options.");
        }
    }

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
}
