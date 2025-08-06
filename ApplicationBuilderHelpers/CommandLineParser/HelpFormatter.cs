using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Handles formatting and display of help content with theming and two-column layout
/// </summary>
internal class HelpFormatter(ICommandBuilder commandBuilder, SubCommandInfo? rootCommand, Dictionary<string, SubCommandInfo> allCommands)
{
    private readonly ICommandBuilder _commandBuilder = commandBuilder;
    private readonly SubCommandInfo? _rootCommand = rootCommand;
    private readonly Dictionary<string, SubCommandInfo> _allCommands = allCommands;

    public void ShowGlobalHelp()
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;

        // Title with version
        HelpFormatter.WriteColored($"{_commandBuilder.ExecutableName} v{_commandBuilder.ExecutableVersion ?? "0.0.0"} - {_commandBuilder.ExecutableTitle}", theme?.HeaderColor);
        Console.WriteLine();

        // Usage section
        HelpFormatter.WriteColored("USAGE:", theme?.HeaderColor);
        var usageText = $"    {_commandBuilder.ExecutableName} [OPTIONS] <COMMAND> [ARGS...]";
        WriteWrappedContent(usageText, helpWidth, 0, theme);
        Console.WriteLine();

        // Description section
        if (!string.IsNullOrEmpty(_commandBuilder.ExecutableDescription))
        {
            HelpFormatter.WriteColored("DESCRIPTION:", theme?.HeaderColor);
            var descriptionText = $"    {_commandBuilder.ExecutableDescription}";
            WriteWrappedContent(descriptionText, helpWidth, 0, theme);
            Console.WriteLine();
        }

        // Separate options into categories
        var rootCommandOptions = new List<SubCommandOptionInfo>();
        var baseCommandOptions = new List<SubCommandOptionInfo>();  
        var globalOptions = new List<SubCommandOptionInfo>();

        if (_rootCommand?.Options.Count > 0)
        {
            foreach (var option in _rootCommand.Options)
            {
                if (option.LongName == "help" && option.IsGlobal)
                {
                    globalOptions.Add(option);
                }
                else if (HelpFormatter.IsBaseCommandOption(option))
                {
                    baseCommandOptions.Add(option);
                }
                else
                {
                    rootCommandOptions.Add(option);
                }
            }
        }

        // Collect ALL left column items for consistent width calculation
        var allLeftColumnItems = new List<string>();

        // Add root command options with version
        var allRootOptions = new List<object>(rootCommandOptions);
        if (rootCommandOptions.Count > 0)
        {
            allRootOptions.Add("VERSION"); // Special marker for version
        }

        if (allRootOptions.Count > 0)
        {
            foreach (var item in allRootOptions)
            {
                var leftColumn = item is SubCommandOptionInfo opt ? HelpFormatter.BuildOptionSignature(opt) : "    -V, --version";
                allLeftColumnItems.Add(leftColumn);
            }
        }

        // Add commands
        var topLevelCommands = _rootCommand?.Children.Values.ToList() ?? [];
        if (topLevelCommands.Count > 0)
        {
            foreach (var cmd in topLevelCommands)
            {
                allLeftColumnItems.Add($"    {cmd.Name}");
            }
        }

        // Add global options
        var allGlobalOptions = new List<SubCommandOptionInfo>(baseCommandOptions);
        allGlobalOptions.AddRange(globalOptions);

        if (allGlobalOptions.Count > 0)
        {
            foreach (var opt in allGlobalOptions)
            {
                allLeftColumnItems.Add(HelpFormatter.BuildOptionSignature(opt));
            }
        }

        // Calculate single optimal left column width for ALL sections
        var optimalLeftColumnWidth = CalculateOptimalLeftColumnWidth(allLeftColumnItems, helpWidth);

        // Now display all sections with the same left column width
        if (allRootOptions.Count > 0)
        {
            ShowSectionWithFixedLayout("OPTIONS:", allRootOptions, optimalLeftColumnWidth, helpWidth, theme,
                item => item is SubCommandOptionInfo opt ? HelpFormatter.BuildOptionSignature(opt) : "    -V, --version",
                item => item is SubCommandOptionInfo opt ? BuildOptionDescription(opt) : "Show version information");
        }

        if (topLevelCommands.Count > 0)
        {
            ShowSectionWithFixedLayout("COMMANDS:", topLevelCommands, optimalLeftColumnWidth, helpWidth, theme,
                cmd => $"    {cmd.Name}",
                cmd => cmd.Description ?? "");
        }

        if (allGlobalOptions.Count > 0)
        {
            ShowSectionWithFixedLayout("GLOBAL OPTIONS:", allGlobalOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => BuildOptionSignature(opt),
                opt => BuildOptionDescription(opt));
        }

        var helpMessage = $"Run '{_commandBuilder.ExecutableName} <command> --help' for more information on specific commands.";
        WriteWrappedContent(helpMessage, helpWidth, 0, theme);
    }

    public void ShowCommandHelp(SubCommandInfo commandInfo)
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;

        // Use the same header format as global help
        WriteColored($"{_commandBuilder.ExecutableName} v{_commandBuilder.ExecutableVersion ?? "0.0.0"} - {_commandBuilder.ExecutableTitle}", theme?.HeaderColor);
        Console.WriteLine();

        WriteColored("USAGE:", theme?.HeaderColor);

        var usage = new StringBuilder($"    {_commandBuilder.ExecutableName}");
        if (!string.IsNullOrEmpty(commandInfo.FullCommandName))
            usage.Append($" {commandInfo.FullCommandName}");
        
        if (commandInfo.AllOptions.Count > 0)
            usage.Append(" [OPTIONS]");
            
        foreach (var arg in commandInfo.AllArguments.OrderBy(a => a.Position))
        {
            usage.Append($" {arg.GetSignature()}");
        }
        
        WriteWrappedContent(usage.ToString(), helpWidth, 0, theme);
        Console.WriteLine();

        if (!string.IsNullOrEmpty(commandInfo.Description))
        {
            HelpFormatter.WriteColored("DESCRIPTION:", theme?.HeaderColor);
            var descriptionText = $"    {commandInfo.Description}";
            WriteWrappedContent(descriptionText, helpWidth, 0, theme);
            Console.WriteLine();
        }

        var commandSpecificOptions = new List<SubCommandOptionInfo>();
        var hierarchySpecificOptions = new List<SubCommandOptionInfo>();
        var baseOptions = new List<SubCommandOptionInfo>();
        var globalOptions = new List<SubCommandOptionInfo>();

        CategorizeOptionsForHelp(commandInfo, commandSpecificOptions, hierarchySpecificOptions, baseOptions, globalOptions);

        // Collect ALL left column items for consistent width calculation
        var allLeftColumnItems = new List<string>();

        // Add command-specific options
        if (commandSpecificOptions.Count > 0)
        {
            foreach (var opt in commandSpecificOptions)
            {
                allLeftColumnItems.Add(HelpFormatter.BuildOptionSignature(opt));
            }
        }

        // Add hierarchy-specific options
        if (hierarchySpecificOptions.Count > 0)
        {
            foreach (var opt in hierarchySpecificOptions)
            {
                allLeftColumnItems.Add(HelpFormatter.BuildOptionSignature(opt));
            }
        }

        // Add arguments
        if (commandInfo.Arguments.Count > 0)
        {
            var sortedArguments = commandInfo.Arguments.OrderBy(a => a.Position).ToList();
            foreach (var arg in sortedArguments)
            {
                allLeftColumnItems.Add(HelpFormatter.BuildArgumentSignature(arg));
            }
        }

        // Add global options
        var allGlobalOptions = new List<SubCommandOptionInfo>(baseOptions);
        allGlobalOptions.AddRange(globalOptions);

        if (allGlobalOptions.Count > 0)
        {
            foreach (var opt in allGlobalOptions)
            {
                allLeftColumnItems.Add(HelpFormatter.BuildOptionSignature(opt));
            }
        }

        // Calculate single optimal left column width for ALL sections
        var optimalLeftColumnWidth = CalculateOptimalLeftColumnWidth(allLeftColumnItems, helpWidth);

        // Now display all sections with the same left column width
        if (commandSpecificOptions.Count > 0)
        {
            // Use "OPTIONS (command):" for simple commands, "OPTIONS:" for subcommands
            var isSubCommand = commandInfo.CommandParts.Length > 1;
            var sectionName = isSubCommand ? "OPTIONS:" : "OPTIONS (command):";
            ShowSectionWithFixedLayout(sectionName, commandSpecificOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => HelpFormatter.BuildOptionSignature(opt),
                opt => BuildOptionDescription(opt));
        }

        if (hierarchySpecificOptions.Count > 0)
        {
            var parentCommandName = HelpFormatter.GetParentCommandName(commandInfo);
            
            // For immediate parent options (like ConfigCommand options for config), 
            // use "command" instead of the specific parent name
            var isImmediateParent = commandInfo.CommandParts.Length == 1; // Single-level command like "config"
            var sectionName = isImmediateParent 
                ? "OPTIONS (command):" 
                : (!string.IsNullOrEmpty(parentCommandName) ? $"OPTIONS ({parentCommandName}):" : "INHERITED OPTIONS:");
                
            ShowSectionWithFixedLayout(sectionName, hierarchySpecificOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => HelpFormatter.BuildOptionSignature(opt),
                opt => BuildOptionDescription(opt));
        }

        if (commandInfo.Arguments.Count > 0)
        {
            var sortedArguments = commandInfo.Arguments.OrderBy(a => a.Position).ToList();
            ShowSectionWithFixedLayout("ARGUMENTS:", sortedArguments, optimalLeftColumnWidth, helpWidth, theme,
                arg => BuildArgumentSignature(arg),
                arg => BuildArgumentDescription(arg));
        }

        // Merge base options and global options into a single GLOBAL OPTIONS section
        if (allGlobalOptions.Count > 0)
        {
            ShowSectionWithFixedLayout("GLOBAL OPTIONS:", allGlobalOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => BuildOptionSignature(opt),
                opt => BuildOptionDescription(opt));
        }
    }

    private void CategorizeOptionsForHelp(SubCommandInfo commandInfo,
        List<SubCommandOptionInfo> commandSpecific,
        List<SubCommandOptionInfo> hierarchySpecific,
        List<SubCommandOptionInfo> baseOptions,
        List<SubCommandOptionInfo> global)
    {
        var seenOptions = new HashSet<string>();

        foreach (var option in commandInfo.Options)
        {
            var signature = option.GetDisplayName();
            if (seenOptions.Add(signature))
            {
                if (option.LongName == "help" && option.IsGlobal)
                {
                    global.Add(option);
                }
                else if (IsBaseCommandOption(option))
                {
                    baseOptions.Add(option);
                }
                else if (IsHierarchySpecificOption(option))
                {
                    hierarchySpecific.Add(option);
                }
                else
                {
                    commandSpecific.Add(option);
                }
            }
        }

        if (_rootCommand != null)
        {
            foreach (var rootOption in _rootCommand.Options)
            {
                var signature = rootOption.GetDisplayName();
                // CA1868: Remove Contains check, just use Add and check result
                if (rootOption.IsGlobal && rootOption.LongName == "help" && seenOptions.Add(signature))
                {
                    global.Add(rootOption);
                }
            }
        }
    }

    private void ShowSectionWithFixedLayout<T>(string sectionHeader, List<T> items, int leftColumnWidth, int totalWidth, IConsoleTheme? theme, 
        Func<T, string> getLeftColumn, Func<T, string> getRightColumn)
    {
        if (items.Count == 0) return;

        HelpFormatter.WriteColored(sectionHeader, theme?.HeaderColor);

        const int Padding = 2;
        
        // Display all items with the fixed left column width
        foreach (var item in items)
        {
            var leftColumn = getLeftColumn(item);
            var rightColumn = getRightColumn(item);
            var leftDisplayWidth = GetDisplayWidth(leftColumn);

            if (leftDisplayWidth > leftColumnWidth)
            {
                // Left content is too long - put right content on next line
                Console.WriteLine(leftColumn);
                WriteWrappedText(rightColumn, totalWidth - 4, 4, theme);
            }
            else
            {
                // Standard two-column layout with fixed left column width
                var rightColumnWidth = totalWidth - leftColumnWidth - Padding;

                Console.Write(leftColumn);
                Console.Write(new string(' ', leftColumnWidth - leftDisplayWidth + Padding));
                WriteWrappedText(rightColumn, rightColumnWidth, leftColumnWidth + Padding, theme);
            }
        }
        Console.WriteLine();
    }

    private int CalculateOptimalLeftColumnWidth(List<string> leftColumnItems, int totalWidth)
    {
        if (leftColumnItems.Count == 0) return 25; // Default reasonable width

        // Calculate the maximum width needed for the left column
        var maxLeftWidth = leftColumnItems.Max(GetDisplayWidth);
        
        // Set reasonable bounds for the left column
        const int MinLeftColumnWidth = 20;
        const int MaxLeftColumnWidth = 35;  // More reasonable maximum
        
        // Ensure the right column has enough space
        var maxAllowedLeftWidth = totalWidth - 40; // Ensure at least 40 chars for right column
        
        var leftColumnWidth = Math.Min(Math.Max(maxLeftWidth, MinLeftColumnWidth), 
                                      Math.Min(MaxLeftColumnWidth, maxAllowedLeftWidth));
        
        return leftColumnWidth;
    }

    private static string BuildOptionSignature(SubCommandOptionInfo option)
    {
        var signature = new StringBuilder("    ");
        
        if (option.ShortName.HasValue)
        {
            signature.Append($"-{option.ShortName}");
            if (!string.IsNullOrEmpty(option.LongName))
            {
                signature.Append(", ");
            }
        }
        
        if (!string.IsNullOrEmpty(option.LongName))
        {
            signature.Append($"--{option.LongName}");
        }

        if (option.PropertyType != typeof(bool))
        {
            var paramName = HelpFormatter.GetParameterName(option);
            signature.Append($" {paramName}");
        }

        return signature.ToString();
    }

    private string BuildOptionDescription(SubCommandOptionInfo option)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(option.Description))
        {
            parts.Add(option.Description);
        }

        if (option.ValidValues?.Length > 0)
        {
            var values = string.Join(", ", option.ValidValues);
            parts.Add($"Possible values: {values}");
        }

        if (!string.IsNullOrEmpty(option.EnvironmentVariable))
        {
            parts.Add($"Environment variable: {option.EnvironmentVariable}");
        }

        if (option.LongName != "help" && option.LongName != "version")
        {
            var defaultValue = GetOptionDefaultValue(option);
            if (defaultValue != null && !IsDefaultValueEmpty(defaultValue))
                parts.Add($"Default: {defaultValue}");
        }

        // Use proper line breaks between different description parts for better readability
        return string.Join("\n", parts);
    }

    private static string BuildArgumentSignature(SubCommandArgumentInfo argument)
    {
        // Use lowercase format like <key> instead of <KEY>
        var name = argument.DisplayName;
        
        if (argument.IsArray)
            name += "...";
            
        var bracketedName = argument.IsRequired ? $"<{name}>" : $"[{name}]";
        return $"    {bracketedName}";
    }

    private static string BuildArgumentDescription(SubCommandArgumentInfo argument)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(argument.Description))
        {
            parts.Add(argument.Description);
        }

        if (argument.ValidValues?.Length > 0)
        {
            var values = string.Join(", ", argument.ValidValues);
            parts.Add($"Possible values: {values}");
        }

        // Use proper line breaks between different description parts for better readability
        return string.Join("\n", parts);
    }

    private static string GetParameterName(SubCommandOptionInfo option)
    {
        // Check if this is an array type
        var isArray = option.PropertyType.IsArray;
        var elementType = isArray ? option.PropertyType.GetElementType() : option.PropertyType;
        
        if (isArray)
        {
            var elementTypeName = elementType?.Name.ToUpperInvariant() ?? "VALUE";
            return elementTypeName switch
            {
                "STRING" => "<STRING...>",
                "INT32" => "<NUMBER...>",
                "DOUBLE" => "<NUMBER...>",
                "FLOAT" => "<NUMBER...>",
                "DECIMAL" => "<NUMBER...>",
                _ => "<VALUE...>"
            };
        }
        
        var typeName = option.PropertyType.Name.ToUpperInvariant();
        
        if (option.ValidValues?.Length > 0)
        {
            return typeName switch
            {
                "STRING" => "<STRING>",
                "INT32" => "<NUMBER>",
                "DOUBLE" => "<NUMBER>",
                "FLOAT" => "<NUMBER>",
                "DECIMAL" => "<NUMBER...>",
                _ => "<VALUE>"
            };
        }

        return typeName switch
        {
            "STRING" => "<STRING>",
            "INT32" => "<NUMBER>",
            "DOUBLE" => "<NUMBER>",
            "FLOAT" => "<NUMBER>",
            "DECIMAL" => "<NUMBER...>",
            "BOOLEAN" => "",
            _ => "<VALUE>"
        };
    }

    private void WriteWrappedText(string text, int width, int indent, IConsoleTheme? theme)
    {
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine();
            return;
        }

        // Split the text by explicit line breaks first
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var indentStr = new string(' ', indent);
        var firstLine = true;

        foreach (var line in lines)
        {
            if (!firstLine)
            {
                Console.WriteLine();
                Console.Write(indentStr);
            }

            // Special handling for "Possible values:" lines to maximize space usage
            if (line.Contains("Possible values:"))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex != -1)
                {
                    var prefix = line[..(colonIndex + 1)]; // "Possible values:"
                    var valuesText = line[(colonIndex + 1)..].Trim(); // The actual values

                    // Write the prefix with secondary color
                    HelpFormatter.WriteColoredText(prefix, theme?.SecondaryColor);
                    var currentPos = GetDisplayWidth(prefix);
                    
                    if (!string.IsNullOrEmpty(valuesText))
                    {
                        // Split values by comma and fit as many as possible per line
                        var values = valuesText.Split(',').Select(v => v.Trim()).ToArray();
                        
                        for (int i = 0; i < values.Length; i++)
                        {
                            var value = values[i];
                            var textToAdd = i == 0 ? $" {value}" : $", {value}";
                            var textLength = GetDisplayWidth(textToAdd);
                            
                            // Check if it fits on current line with some buffer
                            if (currentPos + textLength < width - 2) // Leave 2 chars buffer
                            {
                                HelpFormatter.WriteColoredText(textToAdd, theme?.ParameterColor);
                                currentPos += textLength;
                            }
                            else
                            {
                                // Move to next line
                                Console.WriteLine();
                                Console.Write(indentStr);
                                HelpFormatter.WriteColoredText(value, theme?.ParameterColor);
                                currentPos = indent + GetDisplayWidth(value);
                            }
                        }
                    }
                }
                else
                {
                    // Fallback
                    WriteWrappedLine(line, width, firstLine ? 0 : indent, indentStr);
                }
            }
            else if (line.Contains("Environment variable:"))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex != -1)
                {
                    var prefix = line[..(colonIndex + 1)]; // "Environment variable:"
                    var valueText = line[(colonIndex + 1)..].Trim(); // The env var name

                    WriteColoredText(prefix, theme?.SecondaryColor);

                    HelpFormatter.WriteColoredText($" {valueText}", theme?.ParameterColor);
                }
                else
                {
                    WriteWrappedLine(line, width, firstLine ? 0 : indent, indentStr);
                }
            }
            else if (line.Contains("Default:"))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex != -1)
                {
                    var prefix = line[..(colonIndex + 1)]; // "Default:"
                    var valueText = line[(colonIndex + 1)..].Trim(); // The default value


                    HelpFormatter.WriteColoredText(prefix, theme?.SecondaryColor);
                    HelpFormatter.WriteColoredText($" {valueText}", theme?.ParameterColor);
                }
                else
                {
                    WriteWrappedLine(line, width, firstLine ? 0 : indent, indentStr);
                }
            }
            else
            {
                // Normal word wrapping for other lines
                WriteWrappedLine(line, width, firstLine ? 0 : indent, indentStr);
            }
            
            firstLine = false;
        }
        
        Console.WriteLine();
    }

    private void WriteWrappedLine(string line, int width, int currentIndent, string indentStr)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLineLength = currentIndent;
        var lineStarted = false;

        foreach (var word in words)
        {
            var wordLength = GetDisplayWidth(word);
            
            if (lineStarted && currentLineLength + wordLength + 1 > width)
            {
                Console.WriteLine();
                Console.Write(indentStr);
                currentLineLength = indentStr.Length;
                lineStarted = false;
            }

            if (lineStarted)
            {
                Console.Write(" ");
                currentLineLength++;
            }

            HelpFormatter.WriteColoredText(word, null); // No coloring for regular words
            currentLineLength += wordLength;
            lineStarted = true;
        }
    }

    private static void WriteColored(string text, ConsoleColor? color)
    {
        if (color.HasValue)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.WriteLine(text);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine(text);
        }
    }

    private static void WriteColoredText(string text, ConsoleColor? color)
    {
        if (color.HasValue)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.Write(text);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.Write(text);
        }
    }

    private void WriteWrappedContent(string text, int maxWidth, int indent, IConsoleTheme? theme)
    {
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine();
            return;
        }

        var indentStr = new string(' ', indent);
        var availableWidth = maxWidth - indent;
        
        // Split text into words while preserving console color formatting
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLineLength = 0;
        var lineStarted = false;

        foreach (var word in words)
        {
            var wordLength = GetDisplayWidth(word);
            
            // Check if we need to wrap to next line
            if (lineStarted && currentLineLength + wordLength + 1 > availableWidth)
            {
                Console.WriteLine();
                Console.Write(indentStr);
                currentLineLength = 0;
                lineStarted = false;
            }

            // Add space before word if not at line start
            if (lineStarted)
            {
                Console.Write(" ");
                currentLineLength++;
            }
            else if (indent > 0)
            {
                Console.Write(indentStr);
                currentLineLength = indent;
            }

            HelpFormatter.WriteColoredText(word, theme?.DescriptionColor);
            currentLineLength += wordLength;
            lineStarted = true;
        }
        
        Console.WriteLine();
    }

    private static string? GetParentCommandName(SubCommandInfo commandInfo)
    {
        if (commandInfo.CommandParts.Length > 1)
        {
            return commandInfo.CommandParts[^2];
        }
        
        var hierarchyOption = commandInfo.Options.FirstOrDefault(IsHierarchySpecificOption);
        if (hierarchyOption != null && hierarchyOption.Property.DeclaringType != null)
        {
            var declaringTypeName = hierarchyOption.Property.DeclaringType.Name;
            if (declaringTypeName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
            {
                return declaringTypeName[..^"Command".Length].ToLowerInvariant();
            }
            return declaringTypeName.ToLowerInvariant();
        }
        
        return null;
    }

    private static bool IsBaseCommandOption(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        return declaringType != null && declaringType.Name == "BaseCommand";
    }

    private static bool IsHierarchySpecificOption(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        
        if (declaringType != null && declaringType.IsAbstract && 
            declaringType != typeof(object) && declaringType.Name != "BaseCommand" &&
            declaringType.Name != "Command")
        {
            return true;
        }
        
        return false;
    }

    private object? GetOptionDefaultValue(SubCommandOptionInfo option)
    {
        try
        {
            if (option.OwnerCommand?.Command != null)
            {
                return option.Property.GetValue(option.OwnerCommand.Command);
            }
            
            foreach (var command in _allCommands.Values)
            {
                if (command.Command != null && command.Options.Any(o => o.Property == option.Property))
                {
                    return option.Property.GetValue(command.Command);
                }
            }
            
            if (option.PropertyType.IsValueType)
            {
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
                return Activator.CreateInstance(option.PropertyType);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
#pragma warning restore IDE0079 // Remove unnecessary suppression
            }

            return null;
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

    private int GetDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        // Since we're not using ANSI codes anymore, just return the string length
        return text.Length;
    }
}