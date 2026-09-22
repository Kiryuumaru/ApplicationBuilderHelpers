using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Content provider for help output: signatures, descriptions, categorization,
/// hierarchy policy, and default-value resolution. Moved verbatim from
/// <see cref="HelpFormatter"/> (mechanical split, no behavior change).
/// Has no Console, no ConsoleOutput, no width math, and no theme knowledge.
/// </summary>
internal sealed class HelpContentProvider(
    ICommandBuilder commandBuilder,
    SubCommandInfo? rootCommand,
    Dictionary<string, SubCommandInfo> allCommands)
{
    private readonly ICommandBuilder _commandBuilder = commandBuilder;
    private readonly SubCommandInfo? _rootCommand = rootCommand;
    private readonly Dictionary<string, SubCommandInfo> _allCommands = allCommands;
    private readonly ICommandTypeParserCollection _typeParserCollection = commandBuilder;

    internal HelpModel BuildGlobalModel()
    {
        // Title with version - use auto-detection for null values
        var executableName = _commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
        var executableTitle = _commandBuilder.ExecutableTitle ?? AssemblyHelpers.GetAutoDetectedExecutableTitle();
        var executableVersion = _commandBuilder.ExecutableVersion ?? AssemblyHelpers.GetAutoDetectedVersion();

        // Description section - use auto-detection for null values
        var executableDescription = _commandBuilder.ExecutableDescription ?? AssemblyHelpers.GetAutoDetectedExecutableDescription();

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
                else if (option.IsGlobal)
                {
                    baseCommandOptions.Add(option);
                }
                else
                {
                    rootCommandOptions.Add(option);
                }
            }
        }

        var sections = new List<HelpSection>();

        if (rootCommandOptions.Count > 0)
        {
            var entries = new List<HelpEntry>();
            foreach (var opt in rootCommandOptions)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildOptionSignature(opt),
                    Right = BuildOptionDescription(opt),
                });
            }
            sections.Add(new HelpSection { Header = "OPTIONS:", Entries = entries });
        }

        var topLevelCommands = _rootCommand?.Children.Values.ToList() ?? [];
        if (topLevelCommands.Count > 0)
        {
            var entries = new List<HelpEntry>();
            foreach (var cmd in topLevelCommands)
            {
                entries.Add(new HelpEntry
                {
                    Left = $"    {cmd.Name}",
                    Right = cmd.Description ?? "",
                });
            }
            sections.Add(new HelpSection { Header = "COMMANDS:", Entries = entries });
        }

        var allGlobalOptions = new List<SubCommandOptionInfo>(baseCommandOptions);
        allGlobalOptions.AddRange(globalOptions);

        var globalEntries = new List<HelpEntry>();
        foreach (var opt in allGlobalOptions)
        {
            globalEntries.Add(new HelpEntry
            {
                Left = BuildOptionSignature(opt),
                Right = BuildOptionDescription(opt),
            });
        }
        globalEntries.Add(new HelpEntry
        {
            Left = "    -V, --version",
            Right = "Show version information",
        });
        sections.Add(new HelpSection { Header = "GLOBAL OPTIONS:", Entries = globalEntries });

        return new HelpModel
        {
            TitleLine = $"{executableName} v{executableVersion} - {executableTitle}",
            UsageText = $"    {executableName} [OPTIONS] <COMMAND> [ARGS...]",
            DescriptionText = !string.IsNullOrEmpty(executableDescription) ? $"    {executableDescription}" : null,
            Sections = sections,
            FooterText = $"Run '{executableName} <command> --help' for more information on specific commands.",
        };
    }

    internal HelpModel BuildCommandModel(SubCommandInfo commandInfo)
    {
        // Use the same header format as global help - use auto-detection for null values
        var executableName = _commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
        var executableTitle = _commandBuilder.ExecutableTitle ?? AssemblyHelpers.GetAutoDetectedExecutableTitle();
        var executableVersion = _commandBuilder.ExecutableVersion ?? AssemblyHelpers.GetAutoDetectedVersion();

        var usage = new StringBuilder($"    {executableName}");
        if (!string.IsNullOrEmpty(commandInfo.FullCommandName))
            usage.Append($" {commandInfo.FullCommandName}");

        if (commandInfo.AllOptions.Count > 0)
            usage.Append(" [OPTIONS]");

        foreach (var arg in commandInfo.AllArguments.OrderBy(a => a.Position))
        {
            usage.Append($" {arg.GetSignature()}");
        }

        var commandSpecificOptions = new List<SubCommandOptionInfo>();
        var hierarchySpecificOptions = new List<SubCommandOptionInfo>();
        var baseOptions = new List<SubCommandOptionInfo>();
        var globalOptions = new List<SubCommandOptionInfo>();

        CategorizeOptionsForHelp(commandInfo, commandSpecificOptions, hierarchySpecificOptions, baseOptions, globalOptions);

        var sections = new List<HelpSection>();

        if (commandSpecificOptions.Count > 0)
        {
            // Use "OPTIONS (command):" for simple commands, "OPTIONS:" for subcommands
            var isSubCommand = commandInfo.CommandParts.Length > 1;
            var sectionName = isSubCommand ? "OPTIONS:" : "OPTIONS (command):";
            var entries = new List<HelpEntry>();
            foreach (var opt in commandSpecificOptions)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildOptionSignature(opt),
                    Right = BuildOptionDescription(opt),
                });
            }
            sections.Add(new HelpSection { Header = sectionName, Entries = entries });
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

            var entries = new List<HelpEntry>();
            foreach (var opt in hierarchySpecificOptions)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildOptionSignature(opt),
                    Right = BuildOptionDescription(opt),
                });
            }
            sections.Add(new HelpSection { Header = sectionName, Entries = entries });
        }

        if (commandInfo.Arguments.Count > 0)
        {
            var sortedArguments = commandInfo.Arguments.OrderBy(a => a.Position).ToList();
            var entries = new List<HelpEntry>();
            foreach (var arg in sortedArguments)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildArgumentSignature(arg),
                    Right = BuildArgumentDescription(arg),
                });
            }
            sections.Add(new HelpSection { Header = "ARGUMENTS:", Entries = entries });
        }

        // Merge base options and global options into a single GLOBAL OPTIONS section.
        // -V, --version is listed unconditionally: it is handled by the gateway,
        // never declared as a command option, so every help screen shows it.
        var allGlobalOptions = new List<SubCommandOptionInfo>(baseOptions);
        allGlobalOptions.AddRange(globalOptions);

        var globalEntries = new List<HelpEntry>();
        foreach (var opt in allGlobalOptions)
        {
            globalEntries.Add(new HelpEntry
            {
                Left = BuildOptionSignature(opt),
                Right = BuildOptionDescription(opt),
            });
        }
        globalEntries.Add(new HelpEntry
        {
            Left = "    -V, --version",
            Right = "Show version information",
        });
        sections.Add(new HelpSection { Header = "GLOBAL OPTIONS:", Entries = globalEntries });

        return new HelpModel
        {
            TitleLine = $"{executableName} v{executableVersion} - {executableTitle}",
            UsageText = usage.ToString(),
            DescriptionText = !string.IsNullOrEmpty(commandInfo.Description) ? $"    {commandInfo.Description}" : null,
            Sections = sections,
            FooterText = null,
        };
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
                else if (option.IsGlobal)
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

    private static string BuildOptionSignature(SubCommandOptionInfo option)
    {
        return "    " + option.GetSignature();
    }

    private string BuildOptionDescription(SubCommandOptionInfo option)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(option.Description))
        {
            parts.Add(option.Description);
        }

        if (option.IsRequired)
        {
            parts.Add("(required)");
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

        if (!option.IsRequired && option.LongName != "help" && option.LongName != "version")
        {
            var defaultValue = GetOptionDefaultValue(option);
            if (defaultValue != null && !IsDefaultValueEmpty(defaultValue))
                parts.Add($"Default: {SecretRedaction.GetDefaultDisplay(defaultValue, option.IsSecret)}");
        }

        // Use proper line breaks between different description parts for better readability
        return string.Join("\n", parts);
    }

    private static string BuildArgumentSignature(SubCommandArgumentInfo argument)
    {
        // Use lowercase format like <key> instead of <KEY>
        var name = argument.DisplayName;

        if (argument.IsCollection)
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

    // NOTE: option placeholder logic lives in HelpTypeDisplay (#454);
    // this provider only calls HelpTypeDisplay.GetParameterPlaceholder.

    private static string? GetParentCommandName(SubCommandInfo commandInfo)
    {
        if (commandInfo.CommandParts.Length > 1)
        {
            return commandInfo.CommandParts[^2];
        }

        var hierarchyOption = commandInfo.Options.FirstOrDefault(IsHierarchySpecificOption);
        return hierarchyOption?.OwnerCommand?.Name
            ?? hierarchyOption?.BindTarget?.Name
            ?? commandInfo.Parent?.Name;
    }

    private static bool IsHierarchySpecificOption(SubCommandOptionInfo option)
    {
        if (!option.IsInherited || option.IsGlobal)
        {
            return false;
        }

        var declaringType = option.Property.DeclaringType;
        if (declaringType is null || !declaringType.IsAbstract)
        {
            return false;
        }

        // Structural: the declaring type must sit inside the framework command
        // lineage — walk the abstract BaseType chain and terminate at the
        // framework Command root (generic or non-generic, the ICommand owner).
        // Never compare simple type names and never reference a sample command
        // type. The framework roots themselves are not hierarchy-specific
        // (their options are global or command-local, as before).
        if (IsFrameworkCommandRoot(declaringType))
        {
            return false;
        }

        for (var current = declaringType.BaseType;
            current is not null && current != typeof(object);
            current = current.BaseType)
        {
            if (IsFrameworkCommandRoot(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFrameworkCommandRoot(Type type) =>
        type == typeof(Command) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Command<>));

    private object? GetOptionDefaultValue(SubCommandOptionInfo option)
    {
        try
        {
            // #453 Step 1: OwnerCommand is the definition site, BindTarget is the
            // scope holding this copy. Definition-site first (coincides with
            // the legacy first-scan-hit for identical globals: no behavior
            // change), then the copy-holding scope, then the declaring-type
            // holder fallback (#487: snapshot-first, live only when
            // !IsInstanceRegistration), then the type-parser fallback.
            // For caller-supplied instance registrations the live
            // definition-site instance may already carry a prior run's bound
            // value, so consult the registration-time snapshot first (same
            // seam the promotion gate uses). Snapshot-miss falls back to the
            // existing live reads to preserve behavior.
            if (option.OwnerCommand?.Command != null)
            {
                var holder = FindHolder(option.OwnerCommand.Command.GetType());
                if (holder?.TryGetInitializerDefault(option.Property, out var snapshot) == true)
                {
                    return snapshot;
                }

                return option.Property.GetValue(option.OwnerCommand.Command);
            }

            if (option.BindTarget?.Command != null && !ReferenceEquals(option.BindTarget, option.OwnerCommand))
            {
                return option.Property.GetValue(option.BindTarget.Command);
            }

            var declaringType = option.Property.DeclaringType;
            if (declaringType != null)
            {
                var fallbackHolder = FindHolder(declaringType);
                if (fallbackHolder != null)
                {
                    if (fallbackHolder.TryGetInitializerDefault(option.Property, out var fallbackSnapshot))
                    {
                        return fallbackSnapshot;
                    }

                    if (!fallbackHolder.IsInstanceRegistration)
                    {
                        return option.Property.GetValue(fallbackHolder.Command);
                    }
                }
            }

            // Use type parsers to get default values in an AOT-compatible way
            if (option.PropertyType.IsValueType)
            {
                return GetDefaultValueFromTypeParser(option.PropertyType);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Finds the registration holder for a command type. Multiple registrations
    /// of one type are rejected elsewhere (duplicate-command validation), so
    /// first match is the definition site. Deliberate divergence from the
    /// promotion-gate lookup (<c>CommandHierarchyBuilder.FindHolder</c>): the
    /// gate keys by the option property's declaring type and fails closed on
    /// ambiguity because it compares initializers across definition sites,
    /// while this lookup keys by the holding scope's concrete command type
    /// because it reads one scope's default. Shared idiom: both consult the
    /// holder's registration-time snapshot before any live instance read.
    /// </summary>
    private TypedCommandHolder? FindHolder(Type commandType)
    {
        foreach (var holder in _commandBuilder.Commands)
        {
            if (holder.CommandType == commandType)
                return holder;
        }

        return null;
    }

    /// <summary>
    /// Gets the default value for a type using the registered type parsers, fallback to AOT-compatible defaults
    /// </summary>
    private object? GetDefaultValueFromTypeParser(Type type)
    {
        // First try to get the default value from a registered type parser
        if (_typeParserCollection.TypeParsers.TryGetValue(type, out var parser))
        {
            try
            {
                return parser.GetDefaultValue();
            }
            catch
            {
                // If the type parser fails, fall back to manual defaults
            }
        }

        // Handle nullable types by getting the underlying type
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return null; // Nullable types default to null
        }

        // Handle enums with AOT-compatible approach
        if (type.IsEnum)
        {
            // For enums, return null as we can't determine default safely in AOT
            return null;
        }

        // For arrays, return null
        if (type.IsArray)
            return null;

        // For unknown types, return null
        return null;
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
}
