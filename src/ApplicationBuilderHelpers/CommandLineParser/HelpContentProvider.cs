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
/// hierarchy policy, and default-value resolution.
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
        var executableName = _commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();
        var executableTitle = _commandBuilder.ExecutableTitle ?? AssemblyHelpers.GetAutoDetectedExecutableTitle();
        var executableVersion = _commandBuilder.ExecutableVersion ?? AssemblyHelpers.GetAutoDetectedVersion();

        var executableDescription = _commandBuilder.ExecutableDescription ?? AssemblyHelpers.GetAutoDetectedExecutableDescription();

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

        var rootArguments = _rootCommand?.Arguments.OrderBy(a => a.Position).ToList() ?? [];
        if (rootArguments.Count > 0)
        {
            var entries = new List<HelpEntry>();
            foreach (var arg in rootArguments)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildArgumentSignature(arg),
                    Right = BuildArgumentDescription(arg),
                });
            }
            sections.Add(new HelpSection { Header = "ARGUMENTS:", Entries = entries });
        }

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

        var globalUsage = new StringBuilder($"    {executableName} [OPTIONS]");
        if (topLevelCommands.Count > 0)
            globalUsage.Append(" <COMMAND> [ARGS...]");
        if (_rootCommand != null)
            foreach (var arg in _rootCommand.Arguments.OrderBy(a => a.Position))
                globalUsage.Append($" {arg.GetSignature()}");

        return new HelpModel
        {
            TitleLine = $"{executableName} v{executableVersion} - {executableTitle}",
            UsageText = globalUsage.ToString(),
            DescriptionText = !string.IsNullOrEmpty(executableDescription) ? $"    {executableDescription}" : null,
            Sections = sections,
            FooterText = $"Run '{executableName} <command> --help' for more information on specific commands.",
        };
    }

    internal HelpModel BuildCommandModel(SubCommandInfo commandInfo)
    {
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

        string? commandSectionName = null;
        string? hierarchySectionName = null;

        if (commandSpecificOptions.Count > 0)
        {
            var isSubCommand = commandInfo.CommandParts.Length > 1;
            commandSectionName = isSubCommand ? "OPTIONS:" : "OPTIONS (command):";
        }

        if (hierarchySpecificOptions.Count > 0)
        {
            var parentCommandName = GetParentCommandName(commandInfo);

            var isImmediateParent = commandInfo.CommandParts.Length == 1;
            hierarchySectionName = isImmediateParent
                ? "OPTIONS (command):"
                : (!string.IsNullOrEmpty(parentCommandName) ? $"OPTIONS ({parentCommandName}):" : "INHERITED OPTIONS:");
        }

        if (commandSectionName is not null
            && hierarchySectionName is not null
            && string.Equals(commandSectionName, hierarchySectionName, StringComparison.Ordinal))
        {
            var entries = new List<HelpEntry>();
            foreach (var opt in commandSpecificOptions)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildOptionSignature(opt),
                    Right = BuildOptionDescription(opt),
                });
            }
            foreach (var opt in hierarchySpecificOptions)
            {
                entries.Add(new HelpEntry
                {
                    Left = BuildOptionSignature(opt),
                    Right = BuildOptionDescription(opt),
                });
            }
            sections.Add(new HelpSection { Header = commandSectionName, Entries = entries });
        }
        else
        {
            if (commandSpecificOptions.Count > 0)
            {
                var entries = new List<HelpEntry>();
                foreach (var opt in commandSpecificOptions)
                {
                    entries.Add(new HelpEntry
                    {
                        Left = BuildOptionSignature(opt),
                        Right = BuildOptionDescription(opt),
                    });
                }
                sections.Add(new HelpSection { Header = commandSectionName!, Entries = entries });
            }

            if (hierarchySpecificOptions.Count > 0)
            {
                var entries = new List<HelpEntry>();
                foreach (var opt in hierarchySpecificOptions)
                {
                    entries.Add(new HelpEntry
                    {
                        Left = BuildOptionSignature(opt),
                        Right = BuildOptionDescription(opt),
                    });
                }
                sections.Add(new HelpSection { Header = hierarchySectionName!, Entries = entries });
            }
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

        foreach (var option in commandInfo.AllOptions)
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

        return string.Join("\n", parts);
    }

    private static string BuildArgumentSignature(SubCommandArgumentInfo argument)
    {
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

        return string.Join("\n", parts);
    }

    private static string? GetParentCommandName(SubCommandInfo commandInfo)
    {
        if (commandInfo.CommandParts.Length > 1)
        {
            return commandInfo.CommandParts[^2];
        }

        var hierarchyOption = commandInfo.AllOptions.FirstOrDefault(IsHierarchySpecificOption);
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
                else
                {
                    return GetUnanimousDerivedDefault(option);
                }
            }

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
    /// first match is the definition. Deliberate divergence from the
    /// promotion lookup (<c>CommandHierarchyBuilder.FindHolder</c>): the
    /// lookup keys by the option property's declaring type and stay local on
    /// ambiguity because it compares initializers across definitions,
    /// while this lookup keys by the holding scope's concrete command type
    /// because it reads one scope's default.
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
    /// Reads the declaring type's initializer default off derived registration
    /// holders when no holder matches the declaring type itself (abstract base
    /// options on a synthesized parent scope). Returns the value only when at
    /// least one derived holder reads and every readable value agrees;
    /// otherwise null, so disagreement stays silent instead of picking one.
    /// </summary>
    private object? GetUnanimousDerivedDefault(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        if (declaringType is null)
            return null;

        var seen = false;
        object? agreed = null;
        foreach (var holder in _commandBuilder.Commands)
        {
            if (!declaringType.IsAssignableFrom(holder.CommandType))
                continue;

            var holderProperty = ResolveHolderProperty(holder, option.Property);
            if (holderProperty is null)
                return null;

            object? candidate;
            if (holder.TryGetInitializerDefault(holderProperty, out var snapshot))
            {
                candidate = snapshot;
            }
            else if (holder.IsInstanceRegistration)
            {
                return null;
            }
            else
            {
                try
                {
                    candidate = holderProperty.GetValue(holder.Command);
                }
                catch
                {
                    return null;
                }
            }

            if (!seen)
            {
                agreed = InitializerValueEquality.CloneIfArray(candidate);
                seen = true;
                continue;
            }

            if (!InitializerValuesEqual(agreed, candidate))
                return null;
        }

        return seen ? agreed : null;
    }

    private static System.Reflection.PropertyInfo? ResolveHolderProperty(Models.TypedCommandHolder holder, System.Reflection.PropertyInfo optionProperty)
    {
        if (holder.CommandType == optionProperty.DeclaringType)
            return optionProperty;

        var resolved = holder.CommandType.GetProperty(
            optionProperty.Name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (resolved is null)
            return null;

        if (!optionProperty.PropertyType.IsAssignableFrom(resolved.PropertyType) && !resolved.PropertyType.IsAssignableFrom(optionProperty.PropertyType))
            return null;

        if (!resolved.IsDefined(typeof(ApplicationBuilderHelpers.Attributes.CommandOptionAttribute), inherit: true))
        {
            var originalDeclaringType = optionProperty.DeclaringType;
            var resolvedDeclaringType = resolved.DeclaringType;
            if (originalDeclaringType is null || resolvedDeclaringType is null || !originalDeclaringType.IsAssignableFrom(resolvedDeclaringType))
                return null;
        }

        return resolved;
    }

    private static bool InitializerValuesEqual(object? first, object? second) =>
        InitializerValueEquality.ValuesEqual(first, second);

    /// <summary>
    /// Gets the default value for a type using the registered type parsers, fallback to trim-safe defaults
    /// </summary>
    private object? GetDefaultValueFromTypeParser(Type type)
    {
        if (_typeParserCollection.TypeParsers.TryGetValue(type, out var parser))
        {
            try
            {
                return parser.GetDefaultValue();
            }
            catch
            {
            }
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return null;
        }

        if (type.IsEnum)
        {
            return null;
        }

        if (type.IsArray)
            return null;

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
