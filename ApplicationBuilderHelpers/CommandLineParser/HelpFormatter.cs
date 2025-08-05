using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Handles formatting and display of help content with theming and two-column layout
/// </summary>
internal class HelpFormatter
{
    private readonly ICommandBuilder _commandBuilder;
    private readonly SubCommandInfo? _rootCommand;
    private readonly Dictionary<string, SubCommandInfo> _allCommands;

    public HelpFormatter(ICommandBuilder commandBuilder, SubCommandInfo? rootCommand, Dictionary<string, SubCommandInfo> allCommands)
    {
        _commandBuilder = commandBuilder;
        _rootCommand = rootCommand;
        _allCommands = allCommands;
    }

    public void ShowGlobalHelp()
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;
        
        // Title with version
        WriteColored($"{_commandBuilder.ExecutableName} v{_commandBuilder.ExecutableVersion ?? "0.0.0"} - {_commandBuilder.ExecutableTitle}", theme?.HeaderColor, theme);
        Console.WriteLine();

        // Usage section
        WriteColored("USAGE:", theme?.HeaderColor, theme);
        Console.WriteLine($"    {ApplyColor(_commandBuilder.ExecutableName, theme?.FlagColor, theme)} {ApplyColor("[OPTIONS]", theme?.SecondaryColor, theme)} {ApplyColor("<COMMAND>", theme?.ParameterColor, theme)} {ApplyColor("[ARGS...]", theme?.SecondaryColor, theme)}");
        Console.WriteLine();

        // Description section
        if (!string.IsNullOrEmpty(_commandBuilder.ExecutableDescription))
        {
            WriteColored("DESCRIPTION:", theme?.HeaderColor, theme);
            Console.WriteLine($"    {ApplyColor(_commandBuilder.ExecutableDescription, theme?.DescriptionColor, theme)}");
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
                else if (IsBaseCommandOption(option))
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
                var leftColumn = item is SubCommandOptionInfo opt ? BuildOptionSignature(opt, theme) : $"    {ApplyColor("-V, --version", theme?.FlagColor, theme)}";
                allLeftColumnItems.Add(leftColumn);
            }
        }

        // Add commands
        var topLevelCommands = _rootCommand?.Children.Values.ToList() ?? [];
        if (topLevelCommands.Count > 0)
        {
            foreach (var cmd in topLevelCommands)
            {
                allLeftColumnItems.Add($"    {ApplyColor(cmd.Name, theme?.FlagColor, theme)}");
            }
        }

        // Add global options
        var allGlobalOptions = new List<SubCommandOptionInfo>(baseCommandOptions);
        allGlobalOptions.AddRange(globalOptions);

        if (allGlobalOptions.Count > 0)
        {
            foreach (var opt in allGlobalOptions)
            {
                allLeftColumnItems.Add(BuildOptionSignature(opt, theme));
            }
        }

        // Calculate single optimal left column width for ALL sections
        var optimalLeftColumnWidth = CalculateOptimalLeftColumnWidth(allLeftColumnItems, helpWidth);

        // Now display all sections with the same left column width
        if (allRootOptions.Count > 0)
        {
            ShowSectionWithFixedLayout("OPTIONS:", allRootOptions, optimalLeftColumnWidth, helpWidth, theme,
                item => item is SubCommandOptionInfo opt ? BuildOptionSignature(opt, theme) : $"    {ApplyColor("-V, --version", theme?.FlagColor, theme)}",
                item => item is SubCommandOptionInfo opt ? BuildOptionDescription(opt, theme) : ApplyColor("Show version information", theme?.DescriptionColor, theme));
        }

        if (topLevelCommands.Count > 0)
        {
            ShowSectionWithFixedLayout("COMMANDS:", topLevelCommands, optimalLeftColumnWidth, helpWidth, theme,
                cmd => $"    {ApplyColor(cmd.Name, theme?.FlagColor, theme)}",
                cmd => ApplyColor(cmd.Description ?? "", theme?.DescriptionColor, theme));
        }

        if (allGlobalOptions.Count > 0)
        {
            ShowSectionWithFixedLayout("GLOBAL OPTIONS:", allGlobalOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => BuildOptionSignature(opt, theme),
                opt => BuildOptionDescription(opt, theme));
        }

        WriteColored($"Run '{ApplyColor(_commandBuilder.ExecutableName + " <command> --help", theme?.ParameterColor, theme)}' for more information on specific commands.", theme?.SecondaryColor, theme);
    }

    public void ShowCommandHelp(SubCommandInfo commandInfo)
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;
        
        WriteColored($"{_commandBuilder.ExecutableTitle}", theme?.HeaderColor, theme);
        Console.WriteLine();
        
        WriteColored("USAGE:", theme?.HeaderColor, theme);
        var usage = new StringBuilder($"    {ApplyColor(_commandBuilder.ExecutableName, theme?.FlagColor, theme)}");
        if (!string.IsNullOrEmpty(commandInfo.FullCommandName))
            usage.Append($" {ApplyColor(commandInfo.FullCommandName, theme?.FlagColor, theme)}");
        
        if (commandInfo.AllOptions.Count > 0)
            usage.Append($" {ApplyColor("[OPTIONS]", theme?.SecondaryColor, theme)}");
            
        foreach (var arg in commandInfo.AllArguments.OrderBy(a => a.Position))
        {
            usage.Append($" {ApplyColor(arg.GetSignature(), theme?.ParameterColor, theme)}");
        }
        
        Console.WriteLine(usage.ToString());
        Console.WriteLine();

        if (!string.IsNullOrEmpty(commandInfo.Description))
        {
            WriteColored("DESCRIPTION:", theme?.HeaderColor, theme);
            Console.WriteLine($"    {ApplyColor(commandInfo.Description, theme?.DescriptionColor, theme)}");
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
                allLeftColumnItems.Add(BuildOptionSignature(opt, theme));
            }
        }

        // Add hierarchy-specific options
        if (hierarchySpecificOptions.Count > 0)
        {
            foreach (var opt in hierarchySpecificOptions)
            {
                allLeftColumnItems.Add(BuildOptionSignature(opt, theme));
            }
        }

        // Add arguments
        if (commandInfo.Arguments.Count > 0)
        {
            var sortedArguments = commandInfo.Arguments.OrderBy(a => a.Position).ToList();
            foreach (var arg in sortedArguments)
            {
                allLeftColumnItems.Add(BuildArgumentSignature(arg, theme));
            }
        }

        // Add global options
        var allGlobalOptions = new List<SubCommandOptionInfo>(baseOptions);
        allGlobalOptions.AddRange(globalOptions);

        if (allGlobalOptions.Count > 0)
        {
            foreach (var opt in allGlobalOptions)
            {
                allLeftColumnItems.Add(BuildOptionSignature(opt, theme));
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
                opt => BuildOptionSignature(opt, theme),
                opt => BuildOptionDescription(opt, theme));
        }

        if (hierarchySpecificOptions.Count > 0)
        {
            var parentCommandName = GetParentCommandName(commandInfo);
            
            // For immediate parent options (like ConfigCommand options for config), 
            // use "command" instead of the specific parent name
            var isImmediateParent = commandInfo.CommandParts.Length == 1; // Single-level command like "config"
            var sectionName = isImmediateParent 
                ? "OPTIONS (command):" 
                : (!string.IsNullOrEmpty(parentCommandName) ? $"OPTIONS ({parentCommandName}):" : "INHERITED OPTIONS:");
                
            ShowSectionWithFixedLayout(sectionName, hierarchySpecificOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => BuildOptionSignature(opt, theme),
                opt => BuildOptionDescription(opt, theme));
        }

        if (commandInfo.Arguments.Count > 0)
        {
            var sortedArguments = commandInfo.Arguments.OrderBy(a => a.Position).ToList();
            ShowSectionWithFixedLayout("ARGUMENTS:", sortedArguments, optimalLeftColumnWidth, helpWidth, theme,
                arg => BuildArgumentSignature(arg, theme),
                arg => BuildArgumentDescription(arg, theme));
        }

        // Merge base options and global options into a single GLOBAL OPTIONS section
        if (allGlobalOptions.Count > 0)
        {
            ShowSectionWithFixedLayout("GLOBAL OPTIONS:", allGlobalOptions, optimalLeftColumnWidth, helpWidth, theme,
                opt => BuildOptionSignature(opt, theme),
                opt => BuildOptionDescription(opt, theme));
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
            if (!seenOptions.Contains(signature))
            {
                seenOptions.Add(signature);
                
                if (option.LongName == "help" && option.IsGlobal)
                {
                    global.Add(option);
                }
                else if (IsBaseCommandOption(option))
                {
                    baseOptions.Add(option);
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

        if (_rootCommand != null)
        {
            foreach (var rootOption in _rootCommand.Options)
            {
                var signature = rootOption.GetDisplayName();
                if (!seenOptions.Contains(signature) && rootOption.IsGlobal && rootOption.LongName == "help")
                {
                    seenOptions.Add(signature);
                    global.Add(rootOption);
                }
            }
        }
    }

    private void ShowSectionWithOptimalLayout<T>(string sectionHeader, List<T> items, int totalWidth, IAnsiTheme? theme, 
        Func<T, string> getLeftColumn, Func<T, string> getRightColumn)
    {
        if (items.Count == 0) return;

        WriteColored(sectionHeader, theme?.HeaderColor, theme);

        // Calculate optimal left column width dynamically but with reasonable limits
        var leftColumns = items.Select(getLeftColumn).ToList();
        var maxLeftWidth = leftColumns.Max(GetDisplayWidth);
        
        // Set reasonable bounds for the left column
        const int MinLeftColumnWidth = 20;
        const int MaxLeftColumnWidth = 35;  // More reasonable maximum
        const int Padding = 2;
        
        // Ensure the right column has enough space
        var maxAllowedLeftWidth = totalWidth - 40; // Ensure at least 40 chars for right column
        
        var leftColumnWidth = Math.Min(Math.Max(maxLeftWidth, MinLeftColumnWidth), 
                                      Math.Min(MaxLeftColumnWidth, maxAllowedLeftWidth));
        
        // Display all items with consistent left column width
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
                // Standard two-column layout with optimal left column width
                var rightColumnWidth = totalWidth - leftColumnWidth - Padding;

                Console.Write(leftColumn);
                Console.Write(new string(' ', leftColumnWidth - leftDisplayWidth + Padding));
                WriteWrappedText(rightColumn, rightColumnWidth, leftColumnWidth + Padding, theme);
            }
        }
        Console.WriteLine();
    }

    private void ShowSectionWithFixedLayout<T>(string sectionHeader, List<T> items, int leftColumnWidth, int totalWidth, IAnsiTheme? theme, 
        Func<T, string> getLeftColumn, Func<T, string> getRightColumn)
    {
        if (items.Count == 0) return;

        WriteColored(sectionHeader, theme?.HeaderColor, theme);

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

    private string BuildOptionSignature(SubCommandOptionInfo option, IAnsiTheme? theme)
    {
        var signature = new StringBuilder("    ");
        
        if (option.ShortName.HasValue)
        {
            signature.Append(ApplyColor($"-{option.ShortName}", theme?.FlagColor, theme));
            if (!string.IsNullOrEmpty(option.LongName))
            {
                signature.Append(", ");
            }
        }
        
        if (!string.IsNullOrEmpty(option.LongName))
        {
            signature.Append(ApplyColor($"--{option.LongName}", theme?.FlagColor, theme));
        }

        if (option.PropertyType != typeof(bool))
        {
            var paramName = GetParameterName(option);
            signature.Append($" {ApplyColor(paramName, theme?.ParameterColor, theme)}");
        }

        return signature.ToString();
    }

    private string BuildOptionDescription(SubCommandOptionInfo option, IAnsiTheme? theme)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(option.Description))
        {
            parts.Add(ApplyColor(option.Description, theme?.DescriptionColor, theme));
        }

        if (option.ValidValues?.Length > 0)
        {
            var values = string.Join(", ", option.ValidValues);
            parts.Add($"{ApplyColor("Possible values:", theme?.SecondaryColor, theme)} {ApplyColor(values, theme?.ParameterColor, theme)}");
        }

        if (!string.IsNullOrEmpty(option.EnvironmentVariable))
        {
            parts.Add($"{ApplyColor("EnvironmentVariable:", theme?.SecondaryColor, theme)} {ApplyColor(option.EnvironmentVariable, theme?.ParameterColor, theme)}");
        }

        if (option.LongName != "help" && option.LongName != "version")
        {
            var defaultValue = GetOptionDefaultValue(option);
            if (defaultValue != null && !IsDefaultValueEmpty(defaultValue))
                parts.Add($"{ApplyColor("Default:", theme?.SecondaryColor, theme)} {ApplyColor(defaultValue.ToString(), theme?.ParameterColor, theme)}");
        }

        // Use proper line breaks between different description parts for better readability
        return string.Join("\n", parts);
    }

    private string BuildArgumentSignature(SubCommandArgumentInfo argument, IAnsiTheme? theme)
    {
        // Use lowercase format like <key> instead of <KEY>
        var name = argument.DisplayName;
        
        if (argument.IsArray)
            name += "...";
            
        var bracketedName = argument.IsRequired ? $"<{name}>" : $"[{name}]";
        return $"    {ApplyColor(bracketedName, theme?.ParameterColor, theme)}";
    }

    private string BuildArgumentDescription(SubCommandArgumentInfo argument, IAnsiTheme? theme)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(argument.Description))
        {
            parts.Add(ApplyColor(argument.Description, theme?.DescriptionColor, theme));
        }

        if (argument.ValidValues?.Length > 0)
        {
            var values = string.Join(", ", argument.ValidValues);
            parts.Add($"{ApplyColor("Possible values:", theme?.SecondaryColor, theme)} {ApplyColor(values, theme?.ParameterColor, theme)}");
        }

        // Use proper line breaks between different description parts for better readability
        return string.Join("\n", parts);
    }

    private string GetParameterName(SubCommandOptionInfo option)
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
                "DECIMAL" => "<NUMBER>",
                _ => "<VALUE>"
            };
        }

        return typeName switch
        {
            "STRING" => "<STRING>",
            "INT32" => "<NUMBER>",
            "DOUBLE" => "<NUMBER>",
            "FLOAT" => "<NUMBER>",
            "DECIMAL" => "<NUMBER>",
            "BOOLEAN" => "",
            _ => "<VALUE>"
        };
    }

    private void WriteWrappedText(string text, int width, int indent, IAnsiTheme? theme)
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
                    var prefix = line.Substring(0, colonIndex + 1); // "Possible values:"
                    var valuesText = line.Substring(colonIndex + 1).Trim(); // The actual values
                    
                    // Write the prefix
                    Console.Write(prefix);
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
                                Console.Write(textToAdd);
                                currentPos += textLength;
                            }
                            else
                            {
                                // Move to next line
                                Console.WriteLine();
                                Console.Write(indentStr + value);
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

            Console.Write(word);
            currentLineLength += wordLength;
            lineStarted = true;
        }
    }

    private void WriteCommaSeparatedValues(string[] values, int width, int indent, string indentStr)
    {
        Console.WriteLine();
        Console.Write(indentStr);
        
        var currentLineLength = indent;
        var lineStarted = false;

        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var valueWithComma = i < values.Length - 1 ? value + "," : value;
            var valueLength = GetDisplayWidth(valueWithComma);
            
            if (lineStarted && currentLineLength + valueLength + 1 > width)
            {
                Console.WriteLine();
                Console.Write(indentStr);
                currentLineLength = indent;
                lineStarted = false;
            }

            if (lineStarted)
            {
                Console.Write(" ");
                currentLineLength++;
            }

            Console.Write(valueWithComma);
            currentLineLength += valueLength;
            lineStarted = true;
        }
    }

    private string? GetParentCommandName(SubCommandInfo commandInfo)
    {
        if (commandInfo.CommandParts.Length > 1)
        {
            return commandInfo.CommandParts[^2];
        }
        
        var hierarchyOption = commandInfo.Options.FirstOrDefault(o => IsHierarchySpecificOption(o, commandInfo));
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

    private bool IsBaseCommandOption(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        return declaringType != null && declaringType.Name == "BaseCommand";
    }

    private bool IsHierarchySpecificOption(SubCommandOptionInfo option, SubCommandInfo commandInfo)
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
                return Activator.CreateInstance(option.PropertyType);
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

    private void ShowTwoColumnContent(string leftColumn, string rightColumn, int totalWidth, IAnsiTheme? theme)
    {
        var leftDisplayWidth = GetDisplayWidth(leftColumn);
        
        // Use dynamic column width calculation (same as ShowSectionWithOptimalLayout)
        const int MinRightColumnWidth = 30;
        const int Padding = 2;
        var maxAllowedLeftWidth = totalWidth - MinRightColumnWidth - Padding;
        var leftColumnWidth = Math.Min(leftDisplayWidth, maxAllowedLeftWidth);
        
        if (leftDisplayWidth > leftColumnWidth)
        {
            // Left content is too long - put right content on next line
            Console.WriteLine(leftColumn);
            WriteWrappedText(rightColumn, totalWidth - 4, 4, theme);
        }
        else
        {
            // Standard two-column layout with dynamic left column width
            var rightColumnWidth = totalWidth - leftColumnWidth - Padding;
            
            Console.Write(leftColumn);
            Console.Write(new string(' ', leftColumnWidth - leftDisplayWidth + Padding));
            WriteWrappedText(rightColumn, rightColumnWidth, leftColumnWidth + Padding, theme);
        }
    }

    private int GetDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        var cleaned = Regex.Replace(text, @"\u001b\[[0-9;]*m", "");
        return cleaned.Length;
    }

    private void WriteColored(string text, string? color, IAnsiTheme? theme)
    {
        Console.WriteLine(ApplyColor(text, color, theme));
    }

    private string ApplyColor(string text, string? color, IAnsiTheme? theme)
    {
        if (theme == null || string.IsNullOrEmpty(color))
            return text;
        
        return $"{color}{text}{theme.Reset}";
    }
}