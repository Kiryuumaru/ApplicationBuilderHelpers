using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly Dictionary<string, SubCommandInfo> _allCommands = new();

    /// <summary>
    /// Main entry point - builds hierarchy then parses and executes
    /// </summary>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Build and validate command hierarchy
            BuildCommandHierarchy();
            ValidateCommandHierarchy();

            // Step 2: Handle basic help/version before parsing
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

            // Step 3: Parse command line arguments
            var parseResult = ParseCommandLine(args);

            // Step 4: Handle command-specific help
            if (parseResult.ShowHelp)
            {
                ShowCommandHelp(parseResult.TargetCommand);
                return 0;
            }

            // Step 5: Validate required options and arguments
            ValidateRequiredParameters(parseResult);

            // Step 6: Set property values on command instance
            SetCommandValues(parseResult);

            // Step 7: Execute the command
            await ExecuteCommand(parseResult.TargetCommand, cancellationToken);
            return 0;
        }
        catch (CommandException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
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
            Description = ((ICommandBuilder)ApplicationBuilder).ExecutableDescription
        };

        // Process all commands and build hierarchy
        foreach (var command in CommandBuilder.Commands)
        {
            var commandType = command.GetType();
            var commandAttr = commandType.GetCustomAttribute<CommandAttribute>();
            
            // Create SubCommandInfo for this command
            var subCommandInfo = SubCommandInfo.FromCommand(commandType, command);
            
            // Extract options and arguments
            subCommandInfo.Options = SubCommandOptionInfo.FromCommandType(commandType, subCommandInfo);
            subCommandInfo.Arguments = SubCommandArgumentInfo.FromCommandType(commandType, subCommandInfo);

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
            // Root command - only add options that are truly global (from BaseCommand)
            if (_rootCommand!.HasImplementation)
                throw new InvalidOperationException("Cannot have more than one root command");
            
            _rootCommand.Command = commandInfo.Command;
            
            // Only add BaseCommand options to root, not command-specific options
            foreach (var option in commandInfo.Options)
            {
                if (IsBaseCommandOption(option))
                {
                    _rootCommand.Options.Add(option);
                }
            }
            
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
    private SubCommandInfo CreateIntermediateCommand(string[] commandParts, Type leafCommandType)
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
    private SubCommandInfo? FindAbstractBaseCommandInfo(string[] commandParts, Type leafCommandType)
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

                // Extract only options and arguments that are declared directly in this abstract class
                // Not from its base classes (like BaseCommand) to avoid conflicts
                baseCommandInfo.Options = SubCommandOptionInfo.FromDeclaredType(currentType, baseCommandInfo);
                baseCommandInfo.Arguments = SubCommandArgumentInfo.FromDeclaredType(currentType, baseCommandInfo);
                
                return baseCommandInfo;
            }
            currentType = currentType.BaseType;
        }
        
        return null;
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
                    optionsBySignature[signature] = new List<(SubCommandOptionInfo, SubCommandInfo)>();
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

        // If we ended up on a command without implementation, try to find a default
        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // For now, commands without implementation will show help
            result.ShowHelp = true;
            return result;
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
    private void ParseOptionsAndArguments(string[] args, int startIndex, ParseResult result)
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
                if (value == nextArg && !nextArg?.StartsWith('-') == true)
                    i++;

                AddOptionValue(result, matchedOption, value);
            }
            else if (arg.StartsWith('-'))
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
    private void AddOptionValue(ParseResult result, SubCommandOptionInfo option, string? value)
    {
        if (!result.OptionValues.ContainsKey(option))
            result.OptionValues[option] = new List<string>();

        if (value != null)
            result.OptionValues[option].Add(value);
    }

    /// <summary>
    /// Adds an argument value to the parse result
    /// </summary>
    private void AddArgumentValue(ParseResult result, SubCommandArgumentInfo argument, string value)
    {
        if (!result.ArgumentValues.ContainsKey(argument))
            result.ArgumentValues[argument] = new List<string>();

        result.ArgumentValues[argument].Add(value);
    }

    /// <summary>
    /// Validates that all required parameters are provided
    /// </summary>
    private void ValidateRequiredParameters(ParseResult result)
    {
        // Check required options
        foreach (var option in result.TargetCommand.AllOptions.Where(o => o.IsRequired))
        {
            if (!result.OptionValues.ContainsKey(option) || result.OptionValues[option].Count == 0)
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
            if (!result.ArgumentValues.ContainsKey(argument) || result.ArgumentValues[argument].Count == 0)
            {
                throw new CommandException($"Missing required argument: {argument.DisplayName}", 1);
            }
        }
    }

    /// <summary>
    /// Sets the parsed values on the command instance properties
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Array creation is necessary for command line parsing")]
    private void SetCommandValues(ParseResult result)
    {
        var command = result.TargetCommand.Command!;

        // Set option values
        foreach (var (option, values) in result.OptionValues)
        {
            if (values.Count == 0) continue;

            object? propertyValue;

            if (option.IsArray)
            {
                var elementType = option.ElementType!;
                var array = Array.CreateInstance(elementType, values.Count);
                
                for (int i = 0; i < values.Count; i++)
                {
                    var convertedValue = ConvertValue(values[i], elementType);
                    option.ValidateValue(convertedValue);
                    array.SetValue(convertedValue, i);
                }
                
                propertyValue = array;
            }
            else
            {
                propertyValue = ConvertValue(values[0], option.PropertyType);
                option.ValidateValue(propertyValue);
            }

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
                var array = Array.CreateInstance(elementType, values.Count);
                
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
    /// Converts a string value to the specified type
    /// </summary>
    private object? ConvertValue(string? value, Type targetType)
    {
        if (value == null) return null;

        if (CommandTypeParserCollection.TypeParsers.TryGetValue(targetType, out var parser))
        {
            var result = parser.Parse(value, out var error);
            if (error != null)
                throw new CommandException($"Invalid value '{value}': {error}", 1);
            return result;
        }

        if (targetType == typeof(string))
            return value;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, true);

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    /// Executes the target command
    /// </summary>
    private async Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken)
    {
        var command = commandInfo.Command!;
        
        // Call preparation dependencies
        foreach (var dependency in ApplicationDependencyCollection.ApplicationDependencies)
        {
            dependency.CommandPreparation(ApplicationBuilder);
        }

        command.CommandPreparationInternal(ApplicationBuilder);

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var hostBuilder = await command.ApplicationBuilderInternal(cancellationTokenSource.Token);
        var host = hostBuilder.Build();

        await command.RunInternal(host, cancellationTokenSource);
    }

    /// <summary>
    /// Checks if an option comes from a base class
    /// </summary>
    private bool IsOptionFromBaseClass(SubCommandOptionInfo option, SubCommandInfo command)
    {
        if (command.Command == null) return false;
        
        var commandType = command.Command.GetType();
        var declaringType = option.Property.DeclaringType;
        
        // If the declaring type is different from the command type, it's from a base class
        return declaringType != commandType && declaringType != null && declaringType.IsAssignableFrom(commandType);
    }

    /// <summary>
    /// Creates a copy of an option for global use
    /// </summary>
    private SubCommandOptionInfo CreateGlobalOptionCopy(SubCommandOptionInfo original)
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

    #region Help and Version Methods

    private bool ShouldShowGlobalHelp(string[] args)
    {
        return args.Length == 0 || 
               (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"));
    }

    private bool ShouldShowVersion(string[] args)
    {
        return args.Contains("--version") || args.Contains("-V");
    }

    private void ShowGlobalHelp()
    {
        var commandBuilder = (ICommandBuilder)ApplicationBuilder;
        
        Console.WriteLine($"{commandBuilder.ExecutableTitle}");
        if (!string.IsNullOrEmpty(commandBuilder.ExecutableDescription))
        {
            Console.WriteLine($"{commandBuilder.ExecutableDescription}");
        }
        Console.WriteLine();

        Console.WriteLine("USAGE:");
        Console.WriteLine($"    {commandBuilder.ExecutableName} [OPTIONS] <COMMAND> [ARGS]");
        Console.WriteLine($"    {commandBuilder.ExecutableName} [OPTIONS] <COMMAND> --help");
        Console.WriteLine();

        if (_rootCommand?.Options.Count > 0)
        {
            Console.WriteLine("GLOBAL OPTIONS:");
            foreach (var option in _rootCommand.Options)
            {
                Console.WriteLine($"    {option.GetSignature()}");
                if (!string.IsNullOrEmpty(option.Description))
                    Console.WriteLine($"        {option.Description}");
            }
            Console.WriteLine();
        }

        var topLevelCommands = _rootCommand?.Children.Values.ToList() ?? [];
        if (topLevelCommands.Count > 0)
        {
            Console.WriteLine("COMMANDS:");
            foreach (var command in topLevelCommands)
            {
                Console.WriteLine($"    {command.Name}");
                if (!string.IsNullOrEmpty(command.Description))
                    Console.WriteLine($"        {command.Description}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Run '{commandBuilder.ExecutableName} <command> --help' for more information on specific commands.");
    }

    private void ShowCommandHelp(SubCommandInfo commandInfo)
    {
        var commandBuilder = (ICommandBuilder)ApplicationBuilder;
        
        Console.WriteLine($"{commandInfo.FullCommandName}");
        if (!string.IsNullOrEmpty(commandInfo.Description))
        {
            Console.WriteLine($"{commandInfo.Description}");
        }
        Console.WriteLine();

        Console.WriteLine("USAGE:");
        var usage = new StringBuilder($"    {commandBuilder.ExecutableName}");
        if (!string.IsNullOrEmpty(commandInfo.FullCommandName))
            usage.Append($" {commandInfo.FullCommandName}");
        
        if (commandInfo.AllOptions.Count > 0)
            usage.Append(" [OPTIONS]");
            
        foreach (var arg in commandInfo.AllArguments.OrderBy(a => a.Position))
        {
            usage.Append($" {arg.GetSignature()}");
        }
        
        Console.WriteLine(usage.ToString());
        Console.WriteLine();

        // Categorize options into three groups
        var commandSpecificOptions = new List<SubCommandOptionInfo>();
        var hierarchySpecificOptions = new List<SubCommandOptionInfo>();
        var globalOptions = new List<SubCommandOptionInfo>();

        CategorizeOptionsForHelp(commandInfo, commandSpecificOptions, hierarchySpecificOptions, globalOptions);

        // Show command-specific options (declared directly in this command)
        if (commandSpecificOptions.Count > 0)
        {
            Console.WriteLine("OPTIONS:");
            foreach (var option in commandSpecificOptions)
            {
                ShowDetailedOption(option);
            }
            Console.WriteLine();
        }

        // Show hierarchy-specific options (from intermediate parent commands like ConfigCommand)
        if (hierarchySpecificOptions.Count > 0)
        {
            Console.WriteLine("COMMAND OPTIONS:");
            foreach (var option in hierarchySpecificOptions)
            {
                ShowDetailedOption(option);
            }
            Console.WriteLine();
        }

        if (commandInfo.Arguments.Count > 0)
        {
            Console.WriteLine("ARGUMENTS:");
            foreach (var argument in commandInfo.Arguments.OrderBy(a => a.Position))
            {
                ShowDetailedArgument(argument);
            }
            Console.WriteLine();
        }

        // Show global options (from BaseCommand)
        if (globalOptions.Count > 0)
        {
            Console.WriteLine("GLOBAL OPTIONS:");
            foreach (var option in globalOptions)
            {
                ShowDetailedOption(option);
            }
        }

        if (commandInfo.Children.Count > 0)
        {
            Console.WriteLine("SUBCOMMANDS:");
            foreach (var child in commandInfo.Children.Values)
            {
                Console.WriteLine($"    {child.Name}");
                if (!string.IsNullOrEmpty(child.Description))
                    Console.WriteLine($"        {child.Description}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Categorizes options into command-specific, hierarchy-specific, and global categories for help display
    /// </summary>
    private void CategorizeOptionsForHelp(SubCommandInfo commandInfo, 
        List<SubCommandOptionInfo> commandSpecific,
        List<SubCommandOptionInfo> hierarchySpecific, 
        List<SubCommandOptionInfo> global)
    {
        var seenOptions = new HashSet<string>();

        // First, categorize all options from this command
        foreach (var option in commandInfo.Options)
        {
            var signature = option.GetDisplayName();
            if (!seenOptions.Contains(signature))
            {
                seenOptions.Add(signature);
                
                // Check if this is a global option (from BaseCommand)
                if (IsBaseCommandOption(option))
                {
                    global.Add(option);
                }
                else if (IsHierarchySpecificOption(option, commandInfo))
                {
                    hierarchySpecific.Add(option);
                }
                else
                {
                    commandSpecific.Add(option);
                }
            }
        }

        // Then, add any global options from the root command that haven't been shown yet
        if (_rootCommand != null)
        {
            foreach (var rootOption in _rootCommand.Options)
            {
                var signature = rootOption.GetDisplayName();
                if (!seenOptions.Contains(signature))
                {
                    seenOptions.Add(signature);
                    global.Add(rootOption);
                }
            }
        }
    }

    /// <summary>
    /// Determines if an option is from BaseCommand (should be treated as global)
    /// </summary>
    private bool IsBaseCommandOption(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        return declaringType != null && declaringType.Name == "BaseCommand";
    }

    /// <summary>
    /// Determines if an option is hierarchy-specific (like --format from ConfigCommand)
    /// </summary>
    private bool IsHierarchySpecificOption(SubCommandOptionInfo option, SubCommandInfo commandInfo)
    {
        var declaringType = option.Property.DeclaringType;
        
        // If the option is declared in an abstract intermediate command class (like ConfigCommand)
        // and this command is a descendant, then it's hierarchy-specific
        if (declaringType != null && declaringType.IsAbstract && 
            declaringType != typeof(object) && declaringType.Name != "BaseCommand" &&
            declaringType.Name != "Command")
        {
            return true;
        }
        
        return false;
    }

    private void ShowDetailedOption(SubCommandOptionInfo option)
    {
        Console.WriteLine($"    {option.GetSignature()}");
        if (!string.IsNullOrEmpty(option.Description))
            Console.WriteLine($"        {option.Description}");
        
        if (!string.IsNullOrEmpty(option.EnvironmentVariable))
            Console.WriteLine($"        Environment variable: {option.EnvironmentVariable}");
            
        if (option.ValidValues?.Length > 0)
            Console.WriteLine($"        Possible values: {string.Join(", ", option.ValidValues)}");
            
        // Get the default value from the actual command instance
        var defaultValue = GetOptionDefaultValue(option);
        if (defaultValue != null && !IsDefaultValueEmpty(defaultValue))
            Console.WriteLine($"        Default: {defaultValue}");
    }

    /// <summary>
    /// Gets the default value for an option from the command instance
    /// </summary>
    private object? GetOptionDefaultValue(SubCommandOptionInfo option)
    {
        try
        {
            // Try to get the default value from the owner command
            if (option.OwnerCommand?.Command != null)
            {
                return option.Property.GetValue(option.OwnerCommand.Command);
            }
            
            // If that doesn't work, try to get from any command that has this option
            foreach (var command in _allCommands.Values)
            {
                if (command.Command != null && command.Options.Any(o => o.Property == option.Property))
                {
                    return option.Property.GetValue(command.Command);
                }
            }
            
            // Fallback to getting the default value from the property type
            if (option.PropertyType.IsValueType)
            {
                return Activator.CreateInstance(option.PropertyType);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ShowDetailedArgument(SubCommandArgumentInfo argument)
    {
        Console.WriteLine($"    {argument.GetSignature()}");
        if (!string.IsNullOrEmpty(argument.Description))
            Console.WriteLine($"        {argument.Description}");
    }

    private static bool IsDefaultValueEmpty(object value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrEmpty(s),
            Array arr => arr.Length == 0,
            System.Collections.ICollection collection => collection.Count == 0,
            _ => false
        };
    }

    private void ShowVersion()
    {
        var commandBuilder = (ICommandBuilder)ApplicationBuilder;
        var version = commandBuilder.ExecutableVersion ?? VersionHelpers.GetAutoDetectedVersion();
        Console.WriteLine(version);
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
    public Dictionary<SubCommandOptionInfo, List<string>> OptionValues { get; set; } = new();
    public Dictionary<SubCommandArgumentInfo, List<string>> ArgumentValues { get; set; } = new();
}
