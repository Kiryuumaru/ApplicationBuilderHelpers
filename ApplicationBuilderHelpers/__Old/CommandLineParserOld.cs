using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Themes;
using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.__Old;

internal class CommandLineParserOld
{
    private readonly ICommandBuilder _commandBuilder;
    private readonly ICommandTypeParserCollection _typeParserCollection;
    private readonly IAnsiTheme _theme;

    public CommandLineParserOld(ApplicationBuilder builder)
    {
        _commandBuilder = builder;
        _typeParserCollection = builder;
        
        // Use configured theme or fallback to Monokai Dimmed as default
        _theme = _commandBuilder.Theme ?? new MonokaiDimmedTheme();
    }

    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            // Handle basic help and version first
            if (args.Length == 0)
            {
                var defaultCommand = FindDefaultCommand();
                if (defaultCommand != null)
                {
                    await ExecuteCommand(defaultCommand);
                    return 0;
                }
                else
                {
                    ShowHelp(null);
                    return 0;
                }
            }

            if (args.Contains("--help") || args.Contains("-h"))
            {
                var command = args.Length > 1 && !args[0].StartsWith("-") ? FindCommand(args[0]) : null;
                ShowHelp(command);
                return 0;
            }

            if (args.Contains("--version") || args.Contains("-V"))
            {
                ShowVersion();
                return 0;
            }

            // Find command
            var targetCommand = args.Length > 0 && !args[0].StartsWith("-") ? FindCommand(args[0]) : FindDefaultCommand();
            if (targetCommand == null)
            {
                Console.Error.WriteLine("No command found.");
                Console.WriteLine();
                ShowHelp(null);
                return 1;
            }

            // Parse and set values
            SetCommandValues(targetCommand, args);

            // Execute command
            await ExecuteCommand(targetCommand);
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

    private void SetCommandValues(ICommand command, string[] args)
    {
        var properties = command.GetType().GetProperties();
        var parsedOptions = new HashSet<string>();
        var nonOptionArgs = new List<string>();
        
        // First pass: collect non-option arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("-"))
            {
                nonOptionArgs.Add(args[i]);
            }
        }
        
        // Skip the command name if present
        if (nonOptionArgs.Count > 0)
        {
            var commandName = FindCommandName(command);
            if (commandName != null)
            {
                // Handle commands with spaces (like "remote add")
                if (commandName.Contains(' '))
                {
                    var commandParts = commandName.Split(' ');
                    int partsMatched = 0;
                    
                    // Check how many command parts match the beginning of nonOptionArgs
                    for (int i = 0; i < commandParts.Length && i < nonOptionArgs.Count; i++)
                    {
                        if (nonOptionArgs[i] == commandParts[i])
                        {
                            partsMatched++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    // Remove the matched command parts
                    for (int i = 0; i < partsMatched; i++)
                    {
                        nonOptionArgs.RemoveAt(0);
                    }
                }
                else if (nonOptionArgs[0] == commandName)
                {
                    nonOptionArgs.RemoveAt(0);
                }
            }
        }
        
        foreach (var property in properties)
        {
            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>();
            var argumentAttr = property.GetCustomAttribute<CommandArgumentAttribute>();

            if (optionAttr != null)
            {
                // Handle options (including arrays)
                var optionValues = new List<string>();
                var term = optionAttr.Term ?? property.Name.ToLowerInvariant();
                
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    
                    // Handle long options with equals (--option=value)
                    if (arg.StartsWith($"--{term}="))
                    {
                        var value = arg.Substring($"--{term}=".Length);
                        optionValues.Add(value);
                        parsedOptions.Add(term);
                    }
                    // Handle short options with equals (-o=value)
                    else if (optionAttr.ShortTerm.HasValue && arg.StartsWith($"-{optionAttr.ShortTerm}="))
                    {
                        var value = arg.Substring($"-{optionAttr.ShortTerm}=".Length);
                        optionValues.Add(value);
                        parsedOptions.Add(term);
                    }
                    // Handle compact short options (-ovalue, like -ldebug)
                    else if (optionAttr.ShortTerm.HasValue && property.PropertyType != typeof(bool) && 
                             arg.StartsWith($"-{optionAttr.ShortTerm}") && arg.Length > 2)
                    {
                        var value = arg.Substring(2); // Skip "-" and the short option character
                        optionValues.Add(value);
                        parsedOptions.Add(term);
                    }
                    // Handle regular long and short options (--option value, -o value)
                    else if (arg == $"--{term}" || optionAttr.ShortTerm.HasValue && arg == $"-{optionAttr.ShortTerm}")
                    {
                        if (property.PropertyType == typeof(bool))
                        {
                            // For boolean options, check if next argument is a valid boolean value
                            if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            {
                                var nextArg = args[i + 1];
                                // Check if next argument is a boolean value
                                if (IsBooleanValue(nextArg))
                                {
                                    optionValues.Add(nextArg);
                                    parsedOptions.Add(term);
                                    i++; // Skip the value we just consumed
                                }
                                else
                                {
                                    // Next argument is not a boolean value, treat as flag
                                    property.SetValue(command, true);
                                    parsedOptions.Add(term);
                                }
                            }
                            else
                            {
                                // No next argument or next argument starts with -, treat as flag
                                property.SetValue(command, true);
                                parsedOptions.Add(term);
                            }
                        }
                        else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            optionValues.Add(args[i + 1]);
                            parsedOptions.Add(term);
                            i++; // Skip the value we just consumed
                        }
                    }
                }
                
                // Handle environment variables
                if (optionValues.Count == 0 && !string.IsNullOrEmpty(optionAttr.EnvironmentVariable))
                {
                    var envValue = Environment.GetEnvironmentVariable(optionAttr.EnvironmentVariable);
                    if (!string.IsNullOrEmpty(envValue))
                    {
                        optionValues.Add(envValue);
                    }
                }
                
                // Set property value
                if (optionValues.Count > 0)
                {
                    if (property.PropertyType.IsArray)
                    {
                        var elementType = property.PropertyType.GetElementType()!;
                        var array = Array.CreateInstance(elementType, optionValues.Count);
                        for (int j = 0; j < optionValues.Count; j++)
                        {
                            // Validate FromAmong for array elements
                            if (optionAttr.FromAmong?.Length > 0)
                            {
                                var isValidValue = optionAttr.FromAmong.Any(validValue => 
                                    string.Equals(validValue?.ToString(), optionValues[j], StringComparison.OrdinalIgnoreCase));
                                
                                if (!isValidValue)
                                {
                                    var validValues = string.Join(", ", optionAttr.FromAmong.Select(v => v?.ToString()));
                                    throw new CommandException($"Value '{optionValues[j]}' is not valid for option '--{term}'. Must be one of: {validValues}", 1);
                                }
                            }
                            
                            var value = ConvertSingleValue(optionValues[j], elementType);
                            array.SetValue(value, j);
                        }
                        property.SetValue(command, array);
                    }
                    else
                    {
                        // Validate FromAmong for single values
                        if (optionAttr.FromAmong?.Length > 0)
                        {
                            var isValidValue = optionAttr.FromAmong.Any(validValue => 
                                string.Equals(validValue?.ToString(), optionValues[0], StringComparison.OrdinalIgnoreCase));
                            
                            if (!isValidValue)
                            {
                                var validValues = string.Join(", ", optionAttr.FromAmong.Select(v => v?.ToString()));
                                throw new CommandException($"Value '{optionValues[0]}' is not valid for option '--{term}'. Must be one of: {validValues}", 1);
                            }
                        }
                        
                        var value = ConvertSingleValue(optionValues[0], property.PropertyType);
                        property.SetValue(command, value);
                    }
                }
                
                // Check for required options
                if (optionAttr.Required && optionValues.Count == 0 && !parsedOptions.Contains(term))
                {
                    throw new CommandException($"Missing required option: --{term}", 1);
                }
            }
            else if (argumentAttr != null)
            {
                // Handle arguments
                if (argumentAttr.Position < nonOptionArgs.Count)
                {
                    var value = ConvertSingleValue(nonOptionArgs[argumentAttr.Position], property.PropertyType);
                    property.SetValue(command, value);
                }
                else if (argumentAttr.Required)
                {
                    throw new CommandException($"Missing required argument: {argumentAttr.Name ?? property.Name}", 1);
                }
            }
        }
    }

    private string? FindCommandName(ICommand command)
    {
        var attr = command.GetType().GetCustomAttribute<CommandAttribute>();
        return attr?.Name;
    }

    private void ValidateFromAmong(string value, object[]? fromAmong, string optionName)
    {
        if (fromAmong?.Length > 0)
        {
            var isValidValue = fromAmong.Any(validValue => 
                string.Equals(validValue?.ToString(), value, StringComparison.OrdinalIgnoreCase));
            
            if (!isValidValue)
            {
                var validValues = string.Join(", ", fromAmong.Select(v => v?.ToString()));
                throw new CommandException($"Value '{value}' is not valid for option '--{optionName}'. Must be one of: {validValues}", 1);
            }
        }
    }

    private object? ConvertSingleValue(string? value, Type targetType, object[]? fromAmong = null)
    {
        if (value == null) return null;

        // FromAmong validation
        if (fromAmong?.Length > 0)
        {
            var isValidValue = fromAmong.Any(validValue => 
                string.Equals(validValue?.ToString(), value, StringComparison.OrdinalIgnoreCase));
            
            if (!isValidValue)
            {
                var validValues = string.Join(", ", fromAmong.Select(v => v?.ToString()));
                throw new CommandException($"Value '{value}' is not valid for this option. Must be one of: {validValues}", 1);
            }
        }

        if (_typeParserCollection.TypeParsers.TryGetValue(targetType, out var parser))
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

    private ICommand? FindCommand(string name)
    {
        // First try exact match
        var exactMatch = _commandBuilder.Commands.FirstOrDefault(cmd =>
        {
            var attr = cmd.GetType().GetCustomAttribute<CommandAttribute>();
            return attr?.Name == name;
        });
        
        if (exactMatch != null) return exactMatch;
        
        // Try to find commands with spaces (like "remote add")
        return _commandBuilder.Commands.FirstOrDefault(cmd =>
        {
            var attr = cmd.GetType().GetCustomAttribute<CommandAttribute>();
            if (attr?.Name?.Contains(' ') == true)
            {
                var parts = attr.Name.Split(' ');
                return parts.Length > 0 && parts[0] == name;
            }
            return false;
        });
    }

    private ICommand? FindDefaultCommand()
    {
        return _commandBuilder.Commands.FirstOrDefault(cmd =>
        {
            var attr = cmd.GetType().GetCustomAttribute<CommandAttribute>();
            return attr?.Name == null;
        });
    }

    private async Task ExecuteCommand(ICommand command)
    {
        command.CommandPreparationInternal((ApplicationBuilder)_commandBuilder);

        using var cancellationTokenSource = new CancellationTokenSource();
        var hostBuilder = await command.ApplicationBuilderInternal(cancellationTokenSource.Token);
        var host = hostBuilder.Build();

        await command.RunInternal(host, cancellationTokenSource);
    }

    private void ShowHelp(ICommand? specificCommand)
    {
        var totalWidth = _commandBuilder.HelpWidth ?? 80; // Default to 80 total characters
        var borderWidth = _commandBuilder.HelpBorderWidth ?? 2; // Default to 2 characters border

        if (specificCommand != null)
        {
            ShowCommandHelp(specificCommand, totalWidth, borderWidth);
        }
        else
        {
            ShowGlobalHelp(totalWidth, borderWidth);
        }
    }

    private void ShowGlobalHelp(int totalWidth, int borderWidth)
    {
        var version = _commandBuilder.ExecutableVersion ?? VersionHelpers.GetAutoDetectedVersion();

        // Header with theme colors
        Console.WriteLine($"{_theme.HeaderColor}{_commandBuilder.ExecutableTitle} v{version}{_theme.Reset}");
        if (!string.IsNullOrEmpty(_commandBuilder.ExecutableDescription))
        {
            Console.WriteLine($"{_theme.DescriptionColor}{_commandBuilder.ExecutableDescription}{_theme.Reset}");
        }
        Console.WriteLine();

        // Usage
        Console.WriteLine($"{_theme.HeaderColor}USAGE:{_theme.Reset}");
        Console.WriteLine($"    {_theme.DescriptionColor}{_commandBuilder.ExecutableName}{_theme.Reset} {_theme.SecondaryColor}[OPTIONS]{_theme.Reset} {_theme.ParameterColor}<COMMAND>{_theme.Reset} {_theme.SecondaryColor}[ARGS]{_theme.Reset}");
        Console.WriteLine($"    {_theme.DescriptionColor}{_commandBuilder.ExecutableName}{_theme.Reset} {_theme.SecondaryColor}[OPTIONS]{_theme.Reset} {_theme.ParameterColor}<COMMAND>{_theme.Reset} {_theme.FlagColor}--help{_theme.Reset}");
        Console.WriteLine();

        // Collect all items for dynamic left column calculation
        var items = new List<HelpItem>();

        // Global options
        var globalOptions = GetGlobalOptions();
        if (globalOptions.Any())
        {
            foreach (var option in globalOptions)
            {
                items.Add(CreateOptionHelpItem(option));
            }
            items.Add(new HelpItem($"    {_theme.FlagColor}-h, --help{_theme.Reset}", $"{_theme.DescriptionColor}Show this help message{_theme.Reset}"));
            items.Add(new HelpItem($"    {_theme.FlagColor}-V, --version{_theme.Reset}", $"{_theme.DescriptionColor}Show version information{_theme.Reset}"));
        }

        // Commands
        var namedCommands = _commandBuilder.Commands
            .Where(cmd => cmd.GetType().GetCustomAttribute<CommandAttribute>()?.Name != null)
            .ToList();

        if (namedCommands.Any())
        {
            foreach (var command in namedCommands)
            {
                var attr = command.GetType().GetCustomAttribute<CommandAttribute>();
                items.Add(new HelpItem($"    {_theme.FlagColor}{attr!.Name}{_theme.Reset}", $"{_theme.DescriptionColor}{attr.Description ?? ""}{_theme.Reset}"));
            }
        }

        // Calculate optimal left column width
        var leftColumnWidth = CalculateOptimalLeftColumnWidth(items, totalWidth, borderWidth);

        // Display sections
        if (globalOptions.Any())
        {
            Console.WriteLine($"{_theme.HeaderColor}GLOBAL OPTIONS:{_theme.Reset}");
            foreach (var option in globalOptions)
            {
                ShowOption(option, leftColumnWidth, totalWidth, borderWidth);
            }
            Console.WriteLine(FormatTwoColumn($"    {_theme.FlagColor}-h, --help{_theme.Reset}", $"{_theme.DescriptionColor}Show this help message{_theme.Reset}", leftColumnWidth, totalWidth, borderWidth));
            Console.WriteLine(FormatTwoColumn($"    {_theme.FlagColor}-V, --version{_theme.Reset}", $"{_theme.DescriptionColor}Show version information{_theme.Reset}", leftColumnWidth, totalWidth, borderWidth));
            Console.WriteLine();
        }

        if (namedCommands.Any())
        {
            Console.WriteLine($"{_theme.HeaderColor}COMMANDS:{_theme.Reset}");
            foreach (var command in namedCommands)
            {
                var attr = command.GetType().GetCustomAttribute<CommandAttribute>();
                var leftText = $"    {_theme.FlagColor}{attr!.Name}{_theme.Reset}";
                var rightText = $"{_theme.DescriptionColor}{attr.Description ?? ""}{_theme.Reset}";

                Console.WriteLine(FormatTwoColumn(leftText, rightText, leftColumnWidth, totalWidth, borderWidth));
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Run '{_theme.ParameterColor}{_commandBuilder.ExecutableName} <command> --help{_theme.Reset}' for more information on specific commands.");
    }

    private void ShowCommandHelp(ICommand command, int totalWidth, int borderWidth)
    {
        var attr = command.GetType().GetCustomAttribute<CommandAttribute>();

        Console.WriteLine();

        // Description
        if (!string.IsNullOrEmpty(attr?.Description))
        {
            Console.WriteLine($"{_theme.DescriptionColor}{attr.Description}{_theme.Reset}");
            Console.WriteLine();
        }

        // Usage
        Console.WriteLine($"{_theme.HeaderColor}USAGE:{_theme.Reset}");
        Console.WriteLine($"    {_theme.DescriptionColor}{_commandBuilder.ExecutableName}{_theme.Reset} {_theme.FlagColor}{attr?.Name}{_theme.Reset} {_theme.SecondaryColor}[OPTIONS] [GLOBAL OPTIONS] [ARGS]{_theme.Reset}");
        Console.WriteLine();

        // Collect all items for dynamic left column calculation
        var items = new List<HelpItem>();

        // Command options
        var commandOptions = GetCommandOptions(command);
        if (commandOptions.Any())
        {
            foreach (var option in commandOptions)
            {
                items.Add(CreateOptionHelpItem(option));
            }
        }

        // Arguments
        var arguments = GetCommandArguments(command);
        if (arguments.Any())
        {
            foreach (var argument in arguments)
            {
                items.Add(CreateArgumentHelpItem(argument));
            }
        }

        // Global options
        var globalOptions = GetGlobalOptions();
        if (globalOptions.Any())
        {
            foreach (var option in globalOptions)
            {
                items.Add(CreateOptionHelpItem(option));
            }
            items.Add(new HelpItem($"    {_theme.FlagColor}-h, --help{_theme.Reset}", $"{_theme.DescriptionColor}Show this help message{_theme.Reset}"));
            items.Add(new HelpItem($"    {_theme.FlagColor}-V, --version{_theme.Reset}", $"{_theme.DescriptionColor}Show version information{_theme.Reset}"));
        }

        // Calculate optimal left column width
        var leftColumnWidth = CalculateOptimalLeftColumnWidth(items, totalWidth, borderWidth);

        // Display sections
        if (commandOptions.Any())
        {
            Console.WriteLine($"{_theme.HeaderColor}OPTIONS:{_theme.Reset}");
            foreach (var option in commandOptions)
            {
                ShowOption(option, leftColumnWidth, totalWidth, borderWidth);
            }
            Console.WriteLine();
        }

        if (arguments.Any())
        {
            Console.WriteLine($"{_theme.HeaderColor}ARGUMENTS:{_theme.Reset}");
            foreach (var argument in arguments)
            {
                ShowArgument(argument, leftColumnWidth, totalWidth, borderWidth);
            }
            Console.WriteLine();
        }

        if (globalOptions.Any())
        {
            Console.WriteLine($"{_theme.HeaderColor}GLOBAL OPTIONS:{_theme.Reset}");
            foreach (var option in globalOptions)
            {
                ShowOption(option, leftColumnWidth, totalWidth, borderWidth);
            }
            Console.WriteLine(FormatTwoColumn($"    {_theme.FlagColor}-h, --help{_theme.Reset}", $"{_theme.DescriptionColor}Show this help message{_theme.Reset}", leftColumnWidth, totalWidth, borderWidth));
            Console.WriteLine(FormatTwoColumn($"    {_theme.FlagColor}-V, --version{_theme.Reset}", $"{_theme.DescriptionColor}Show version information{_theme.Reset}", leftColumnWidth, totalWidth, borderWidth));
        }
    }

    private HelpItem CreateOptionHelpItem(OptionInfo option)
    {
        var leftText = new StringBuilder("    ");

        if (option.ShortTerm.HasValue)
        {
            leftText.Append($"{_theme.FlagColor}-{option.ShortTerm}{_theme.Reset}");
            if (!string.IsNullOrEmpty(option.Term))
                leftText.Append($", {_theme.FlagColor}--{option.Term}{_theme.Reset}");
        }
        else
        {
            leftText.Append($"{_theme.FlagColor}--{option.Term}{_theme.Reset}");
        }

        if (option.PropertyType != typeof(bool))
        {
            var typeName = GetTypeName(option.PropertyType);
            leftText.Append($" {_theme.ParameterColor}<{typeName}>{_theme.Reset}");
        }

        var description = new StringBuilder($"{_theme.DescriptionColor}{option.Description ?? ""}{_theme.Reset}");

        if (!string.IsNullOrEmpty(option.EnvironmentVariable))
        {
            description.AppendLine();
            description.Append($"Environment variable: {_theme.SecondaryColor}{option.EnvironmentVariable}{_theme.Reset}");
        }

        if (option.FromAmong?.Length > 0)
        {
            description.AppendLine();
            description.Append($"Possible values: {_theme.SecondaryColor}{string.Join(", ", option.FromAmong)}{_theme.Reset}");
        }

        if (option.DefaultValue != null && !IsDefaultValueEmpty(option.DefaultValue))
        {
            description.AppendLine();
            description.Append($"Default: {_theme.SecondaryColor}{option.DefaultValue}{_theme.Reset}");
        }

        return new HelpItem(leftText.ToString(), description.ToString());
    }

    private HelpItem CreateArgumentHelpItem(ArgumentInfo argument)
    {
        var leftText = $"    {_theme.ParameterColor}<{argument.Name ?? argument.Property.Name.ToLower()}>{_theme.Reset}";
        var description = $"{_theme.DescriptionColor}{argument.Description ?? ""}{_theme.Reset}";

        if (argument.Required)
        {
            description += $" {_theme.RequiredColor}(REQUIRED){_theme.Reset}";
        }

        return new HelpItem(leftText, description);
    }

    private int CalculateOptimalLeftColumnWidth(List<HelpItem> items, int totalWidth, int borderWidth)
    {
        if (!items.Any()) return Math.Min(35, totalWidth / 2);

        // Calculate the maximum width needed for the left column
        var maxLeftWidth = items.Max(item => GetDisplayWidth(item.Left));
        
        // Account for border width in calculations
        var availableWidth = totalWidth - borderWidth;
        
        // Ensure minimum space for right column (at least 30 characters)
        var maxAllowedLeftWidth = Math.Max(availableWidth - 30, 20);
        
        // Use the smaller of the two to ensure good layout
        var optimalWidth = Math.Min(maxLeftWidth, maxAllowedLeftWidth);
        
        // Ensure minimum left column width
        return Math.Max(optimalWidth, 20);
    }

    private int GetDisplayWidth(string text)
    {
        // Remove ANSI color codes for accurate width calculation
        return Regex.Replace(text, @"\u001b\[[0-9;]*m", "").Length;
    }

    private void ShowOption(OptionInfo option, int leftColumnWidth, int totalWidth, int borderWidth)
    {
        var helpItem = CreateOptionHelpItem(option);
        Console.WriteLine(FormatTwoColumn(helpItem.Left, helpItem.Right, leftColumnWidth, totalWidth, borderWidth));
    }

    private void ShowArgument(ArgumentInfo argument, int leftColumnWidth, int totalWidth, int borderWidth)
    {
        var helpItem = CreateArgumentHelpItem(argument);
        Console.WriteLine(FormatTwoColumn(helpItem.Left, helpItem.Right, leftColumnWidth, totalWidth, borderWidth));
    }

    private string FormatTwoColumn(string left, string right, int leftColumnWidth, int totalWidth, int borderWidth)
    {
        var leftDisplayWidth = GetDisplayWidth(left);
        var rightColumnWidth = totalWidth - leftColumnWidth - borderWidth;
        
        if (leftDisplayWidth > leftColumnWidth)
        {
            // If left content is too long, put right content on next line
            var indentWidth = leftColumnWidth + borderWidth;
            return left + Environment.NewLine + new string(' ', indentWidth) + WrapText(right, rightColumnWidth, indentWidth);
        }

        var padding = leftColumnWidth - leftDisplayWidth;
        var border = new string(' ', borderWidth);
        return left + new string(' ', padding) + border + WrapText(right, rightColumnWidth, leftColumnWidth + borderWidth);
    }

    private string WrapText(string text, int maxWidth, int leftColumnWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            return text;

        var lines = text.Split('\n');
        var result = new StringBuilder();

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineWithoutColors = Regex.Replace(line, @"\u001b\[[0-9;]*m", "");
            
            if (lineWithoutColors.Length <= maxWidth)
            {
                if (lineIndex > 0)
                {
                    result.AppendLine();
                    result.Append(new string(' ', leftColumnWidth));
                }
                result.Append(line);
            }
            else
            {
                // Word wrap the line
                var wrappedLines = WrapLine(line, maxWidth);
                for (int wrapIndex = 0; wrapIndex < wrappedLines.Count; wrapIndex++)
                {
                    if (lineIndex > 0 || wrapIndex > 0)
                    {
                        result.AppendLine();
                        result.Append(new string(' ', leftColumnWidth));
                    }
                    result.Append(wrappedLines[wrapIndex]);
                }
            }
        }

        return result.ToString();
    }

    private List<string> WrapLine(string line, int maxWidth)
    {
        var result = new List<string>();
        var words = line.Split(' ');
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            var wordWithoutColors = Regex.Replace(word, @"\u001b\[[0-9;]*m", "");
            var currentLineWithoutColors = Regex.Replace(currentLine.ToString(), @"\u001b\[[0-9;]*m", "");

            if (currentLineWithoutColors.Length + wordWithoutColors.Length + 1 <= maxWidth)
            {
                if (currentLine.Length > 0)
                    currentLine.Append(' ');
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    result.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                
                // Handle words longer than maxWidth
                if (wordWithoutColors.Length > maxWidth)
                {
                    result.Add(word.Substring(0, Math.Min(word.Length, maxWidth - 3)) + "...");
                }
                else
                {
                    currentLine.Append(word);
                }
            }
        }

        if (currentLine.Length > 0)
        {
            result.Add(currentLine.ToString());
        }

        return result.Any() ? result : [line];
    }

    private string GetTypeName(Type type)
    {
        if (type.IsArray)
            type = type.GetElementType()!;

        return type.Name.ToUpper();
    }

    private void ShowVersion()
    {
        var version = _commandBuilder.ExecutableVersion ?? VersionHelpers.GetAutoDetectedVersion();
        Console.WriteLine($"{_theme.HeaderColor}{version}{_theme.Reset}");
    }

    private List<OptionInfo> GetGlobalOptions()
    {
        var globalOptions = new List<OptionInfo>();

        // Find options that are common to all commands
        var allCommandOptions = _commandBuilder.Commands
            .SelectMany(cmd => GetCommandOptions(cmd))
            .GroupBy(opt => opt.Property.Name)
            .Where(g => g.Count() == _commandBuilder.Commands.Count)
            .Select(g => g.First())
            .ToList();

        globalOptions.AddRange(allCommandOptions);

        return globalOptions;
    }

    private List<OptionInfo> GetCommandOptions(ICommand command)
    {
        var options = new List<OptionInfo>();
        var properties = command.GetType().GetProperties();

        foreach (var property in properties)
        {
            var attr = property.GetCustomAttribute<CommandOptionAttribute>();
            if (attr != null)
            {
                options.Add(new OptionInfo
                {
                    Property = property,
                    PropertyType = property.PropertyType,
                    ShortTerm = attr.ShortTerm,
                    Term = attr.Term ?? property.Name.ToLower(),
                    Description = attr.Description,
                    Required = attr.Required,
                    EnvironmentVariable = attr.EnvironmentVariable,
                    FromAmong = attr.FromAmong,
                    DefaultValue = GetDefaultValue(property, command)
                });
            }
        }

        return options;
    }

    private List<ArgumentInfo> GetCommandArguments(ICommand command)
    {
        var arguments = new List<ArgumentInfo>();
        var properties = command.GetType().GetProperties();

        foreach (var property in properties)
        {
            var attr = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (attr != null)
            {
                arguments.Add(new ArgumentInfo
                {
                    Property = property,
                    Name = attr.Name,
                    Description = attr.Description,
                    Position = attr.Position,
                    Required = attr.Required
                });
            }
        }

        return arguments.OrderBy(a => a.Position).ToList();
    }

    private object? GetDefaultValue(PropertyInfo property, ICommand command)
    {
        try
        {
            return property.GetValue(command);
        }
        catch
        {
            return null;
        }
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

    private static bool IsBooleanValue(string value)
    {
        return value.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
               value.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
               value.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
               value.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
               value.Equals("1", StringComparison.InvariantCultureIgnoreCase) ||
               value.Equals("0", StringComparison.InvariantCultureIgnoreCase);
    }
}

internal class OptionInfo
{
    public PropertyInfo Property { get; set; } = null!;
    public Type PropertyType { get; set; } = null!;
    public char? ShortTerm { get; set; }
    public string? Term { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public string? EnvironmentVariable { get; set; }
    public object[]? FromAmong { get; set; }
    public object? DefaultValue { get; set; }
}

internal class ArgumentInfo
{
    public PropertyInfo Property { get; set; } = null!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Position { get; set; }
    public bool Required { get; set; }
}

internal class HelpItem
{
    public string Left { get; }
    public string Right { get; }

    public HelpItem(string left, string right)
    {
        Left = left;
        Right = right;
    }
}
