using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Builds and validates the command hierarchy from registered commands.
/// Each build resolves a per-run instance: type registrations get a fresh
/// instance so bound values cannot leak across runs, while caller-supplied
/// instance registrations keep their identity.
/// </summary>
internal sealed class CommandHierarchyBuilder(
    ICommandBuilder commandBuilder,
    ICommandTypeParserCollection typeParserCollection,
    CommandReflectionCache reflectionCache)
{
    public SubCommandInfo RootCommand { get; private set; } = null!;

    private readonly Dictionary<string, SubCommandInfo> _allCommands = [];

    public IReadOnlyDictionary<string, SubCommandInfo> AllCommands => _allCommands;

    /// <summary>
    /// Builds the command hierarchy from registered commands
    /// </summary>
    public void BuildCommandHierarchy()
    {
        RootCommand = new SubCommandInfo
        {
            CommandParts = [],
            Description = commandBuilder.ExecutableDescription ?? AssemblyHelpers.GetAutoDetectedExecutableDescription()
        };
        _allCommands.Clear();

        foreach (var typedCommandHolder in commandBuilder.Commands)
        {
            _ = typedCommandHolder.CommandType.GetCustomAttribute<CommandAttribute>();
            var runCommand = typedCommandHolder.CreateRunInstance();
            var typeDescriptor = reflectionCache.GetOrAdd(typedCommandHolder.CommandType);

            var subCommandInfo = SubCommandInfo.FromCommand(typedCommandHolder.CommandType, runCommand);

            subCommandInfo.Options = typeDescriptor.Options
                .Select(descriptor => SubCommandOptionInfo.FromDescriptor(
                    descriptor,
                    subCommandInfo,
                    SubCommandOptionInfo.ResolveValidValues(descriptor, typeParserCollection)))
                .ToList();
            subCommandInfo.Arguments = typeDescriptor.Arguments
                .Select(descriptor => SubCommandArgumentInfo.FromDescriptor(
                    descriptor,
                    subCommandInfo,
                    SubCommandArgumentInfo.ResolveValidValues(descriptor, typeParserCollection)))
                .ToList();

            InsertCommandIntoHierarchy(subCommandInfo);
        }

        DetermineGlobalOptions();
    }

    /// <summary>
    /// Inserts a command into the appropriate place in the hierarchy
    /// </summary>
    private void InsertCommandIntoHierarchy(SubCommandInfo commandInfo)
    {
        foreach (var part in commandInfo.CommandParts)
        {
            if (string.IsNullOrWhiteSpace(part))
                throw new InvalidOperationException($"Invalid command term '{commandInfo.FullCommandName}': command names must not be empty or whitespace.");
            if (part.StartsWith("-", StringComparison.Ordinal))
                throw new InvalidOperationException($"Invalid command term '{commandInfo.FullCommandName}': command names must not start with '-'.");
        }

        if (commandInfo.CommandParts.Length == 0)
        {
            if (RootCommand!.HasImplementation)
                throw new InvalidOperationException("Cannot have more than one root command");

            RootCommand.Command = commandInfo.Command;

            RootCommand.Options.AddRange(commandInfo.Options);
            RootCommand.Arguments.AddRange(commandInfo.Arguments);
            return;
        }

        var current = RootCommand!;

        for (int i = 0; i < commandInfo.CommandParts.Length; i++)
        {
            var part = commandInfo.CommandParts[i];

            if (i == commandInfo.CommandParts.Length - 1)
            {
                current.AddChild(commandInfo);
                _allCommands[commandInfo.FullCommandName] = commandInfo;
            }
            else
            {
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
        var leafDescriptor = reflectionCache.GetOrAdd(leafCommandType);
        var intermediateCommandInfo = FindAbstractBaseCommandInfo(commandParts, leafCommandType, leafDescriptor);

        SubCommandInfo result;
        if (intermediateCommandInfo != null)
        {
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
            result = new SubCommandInfo
            {
                CommandParts = commandParts,
                Description = $"Commands for {commandParts[^1]}"
            };
        }

        if (RootCommand != null)
        {
            foreach (var globalOption in RootCommand.Options.Where(o => o.IsGlobal))
            {
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
    private SubCommandInfo? FindAbstractBaseCommandInfo(string[] commandParts, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type leafCommandType, CommandTypeDescriptor leafDescriptor)
    {
        var currentType = leafCommandType.BaseType;
        var targetCommandName = string.Join(" ", commandParts);

        while (currentType != null && currentType != typeof(object))
        {
            var commandAttr = currentType.GetCustomAttribute<CommandAttribute>();
            var baseTerm = commandAttr?.Term;
            string? normalizedBaseTerm = null;
            if (baseTerm is not null)
            {
                if (string.IsNullOrWhiteSpace(baseTerm))
                    throw new InvalidOperationException($"Invalid command term on '{currentType.FullName}': term must not be empty or whitespace.");
                var baseParts = baseTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var basePart in baseParts)
                {
                    if (basePart.StartsWith("-", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Invalid command term '{baseTerm}' on '{currentType.FullName}': command names must not start with '-'.");
                }
                normalizedBaseTerm = string.Join(" ", baseParts);
            }
            if (commandAttr != null &&
                currentType.IsAbstract &&
                normalizedBaseTerm == targetCommandName)
            {
                var baseCommandInfo = new SubCommandInfo
                {
                    CommandParts = commandParts,
                    Description = commandAttr.Description
                };

                var matchedBaseType = currentType;
                baseCommandInfo.Options = leafDescriptor.Options
                    .Where(d => d.DeclaringType == matchedBaseType)
                    .Select(descriptor => SubCommandOptionInfo.FromDescriptor(
                        descriptor,
                        baseCommandInfo,
                        SubCommandOptionInfo.ResolveValidValues(descriptor, typeParserCollection)))
                    .ToList();
                baseCommandInfo.Arguments = leafDescriptor.Arguments
                    .Where(d => d.DeclaringType == matchedBaseType)
                    .Select(descriptor => SubCommandArgumentInfo.FromDescriptor(
                        descriptor,
                        baseCommandInfo,
                        SubCommandArgumentInfo.ResolveValidValues(descriptor, typeParserCollection)))
                    .ToList();

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
        var concreteCommands = _allCommands.Values.Where(c => c.HasImplementation).ToList();

        if (concreteCommands.Count == 0)
            return;

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

        foreach (var (signature, optionInfos) in optionsBySignature)
        {
            if (optionInfos.Count == concreteCommands.Count)
            {
                var firstOption = optionInfos[0].option;
                var allIdentical = optionInfos.All(oi =>
                    oi.option.PropertyType == firstOption.PropertyType &&
                    oi.option.IsRequired == firstOption.IsRequired &&
                    oi.option.IsSecret == firstOption.IsSecret &&
                    oi.option.ShortName == firstOption.ShortName &&
                    oi.option.LongName == firstOption.LongName &&
                    string.Equals(oi.option.EnvironmentVariable, firstOption.EnvironmentVariable, StringComparison.Ordinal) &&
                    oi.option.IsCaseSensitive == firstOption.IsCaseSensitive &&
                    string.Equals(oi.option.Description, firstOption.Description, StringComparison.Ordinal) &&
                    InitializerValuesEqual(oi.option, firstOption) &&
                    ArraysEqual(oi.option.ValidValues, firstOption.ValidValues));

                if (allIdentical)
                {
                    foreach (var (option, _) in optionInfos)
                    {
                        option.IsGlobal = true;
                        option.IsInherited = true;
                    }

                    if (!RootCommand!.Options.Any(o => o.GetDisplayName() == signature))
                    {
                        var globalOption = CreateGlobalOptionCopy(firstOption);
                        globalOption.BindTarget = RootCommand;
                        RootCommand.Options.Add(globalOption);
                    }
                }
            }
        }

        AddBuiltInGlobalOptions();
    }

    /// <summary>
    /// Compares initializer defaults across definitions. No
    /// <c>DefaultValue</c> snapshot exists on the option node, so divergence
    /// is read from the registration holders keyed by the option property's
    /// declaring type (see
    /// <c>HelpContentProvider.GetOptionDefaultValue</c>). For type registrations the
    /// holder's registration instance is pristine (binding mutates per-run
    /// copies, never the registration). For caller-supplied instance
    /// registrations the registration instance is the shared mutable
    /// registration: a prior run's binding may already have replaced the
    /// initializer, so the comparison uses the holder's registration-time
    /// snapshot (captured on the first read, before binding can mutate
    /// the instance). Any unknown or ambiguous holder, or any read failure,
    /// blocks promotion (stays local) rather than risking a wrong global merge.
    /// </summary>
    private bool InitializerValuesEqual(SubCommandOptionInfo option, SubCommandOptionInfo firstOption)
    {
        try
        {
            var current = ReadInitializerValue(option);
            var first = ReadInitializerValue(firstOption);
            if (current is InitializerValuesUnreadable || first is InitializerValuesUnreadable)
                return false;
            if (current is Array currentArray && first is Array firstArray)
            {
                if (currentArray.Length != firstArray.Length)
                    return false;
                for (var i = 0; i < currentArray.Length; i++)
                {
                    if (!Equals(currentArray.GetValue(i), firstArray.GetValue(i)))
                        return false;
                }

                return true;
            }

            return Equals(current, first);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads one option's initializer default for the promotion lookup off the
    /// registration holder keyed by the option property's declaring type:
    /// the holder's registration-time snapshot for caller-supplied
    /// instance registrations, otherwise the holder's registration instance
    /// value. Returns the sentinel when the holder is unknown, ambiguous
    /// (more than one holder for the declaring type), missing, or unreadable.
    /// </summary>
    private object? ReadInitializerValue(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        if (declaringType is null)
            return InitializerValuesUnreadable.Value;

        var holder = FindHolder(declaringType);
        if (holder is null)
            return InitializerValuesUnreadable.Value;

        if (holder.TryGetInitializerDefault(option.Property, out var snapshot))
            return snapshot;

        if (holder.IsInstanceRegistration)
            return InitializerValuesUnreadable.Value;

        try
        {
            return option.Property.GetValue(holder.Command);
        }
        catch
        {
            return InitializerValuesUnreadable.Value;
        }
    }

    /// <summary>
    /// Sentinel that never equals a real initializer default, so an unknown or
    /// ambiguous holder, or a missing/unreadable instance-registration snapshot,
    /// blocks promotion (stays local).
    /// </summary>
    private sealed class InitializerValuesUnreadable
    {
        public static readonly InitializerValuesUnreadable Value = new();
        private InitializerValuesUnreadable() { }
    }

    /// <summary>
    /// Finds the single registration holder whose command type can supply the
    /// declaring type's initializer default: a holder whose
    /// <c>CommandType</c> equals the declaring type, or whose type derives from
    /// it (inherited option reports the base declaring type). Returns null when
    /// no holder matches or more than one matches (ambiguous), both are
    /// rejected. Multiple same-type registrations are rejected
    /// elsewhere (duplicate-command validation), so a single exact-type match
    /// remains the common definition-site case.
    /// </summary>
    private Models.TypedCommandHolder? FindHolder(Type declaringType)
    {
        Models.TypedCommandHolder? match = null;
        foreach (var holder in commandBuilder.Commands)
        {
            if (holder.CommandType == declaringType || declaringType.IsAssignableFrom(holder.CommandType))
            {
                if (match is not null)
                    return null;
                match = holder;
            }
        }

        return match;
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
        var dummyProperty = typeof(SubCommandOptionInfo).GetProperty(nameof(SubCommandOptionInfo.Property))!;

        var helpOption = new SubCommandOptionInfo
        {
            ShortName = 'h',
            LongName = "help",
            Description = "Show help information",
            IsGlobal = true,
            IsInherited = true,
            OwnerCommand = RootCommand,
            BindTarget = RootCommand,
            Property = dummyProperty,
            PropertyType = typeof(bool)
        };

        if (!RootCommand!.Options.Any(o => o.GetDisplayName() == helpOption.GetDisplayName()))
        {
            RootCommand.Options.Add(helpOption);
        }

        foreach (var command in _allCommands.Values)
        {
            var existingHelpOption = command.Options.FirstOrDefault(o => o.LongName == "help");
            if (existingHelpOption == null)
            {
                var globalHelpOption = CreateGlobalOptionCopy(helpOption);
                globalHelpOption.BindTarget = command;
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
    public void ValidateCommandHierarchy()
    {
        RootCommand?.Validate();

        foreach (var command in _allCommands.Values)
        {
            if (!command.HasImplementation && command.IsLeaf)
            {
                throw new InvalidOperationException(
                    $"Command '{command.FullCommandName}' has no implementation and no subcommands");
            }
        }

        ValidateReservedShortNames();
    }

    /// <summary>
    /// Rejects registration-time shadowing of the reserved help/version shorts:
    /// <c>-h</c> (help) and <c>-V</c> (version) win inside combined short
    /// clusters even mid-cluster (<c>ArgumentParser</c>), so a declared option
    /// reusing either short (e.g. <c>-h/--host</c> on <c>serve</c>) would never
    /// bind, <c>serve -hw</c> routes to help instead of <c>Host=w</c>. Only the
    /// built-in <c>--help</c> owner may hold <c>-h</c>; <c>-V</c> is forbidden
    /// for all local options because no built-in version node exists (version
    /// is handled in pre-parse), so any local <c>-V</c> would silently never bind.
    /// </summary>
    private void ValidateReservedShortNames()
    {
        if (RootCommand != null)
            ValidateReservedShortNames(RootCommand);

        foreach (var command in _allCommands.Values)
            ValidateReservedShortNames(command);
    }

    private static void ValidateReservedShortNames(SubCommandInfo command)
    {
        var commandName = string.IsNullOrEmpty(command.FullCommandName) ? "<root>" : command.FullCommandName;
        foreach (var option in command.Options)
        {
            if (option.ShortName == 'h' && option.LongName != "help")
            {
                throw new InvalidOperationException(
                    $"Reserved short name conflict: '-h' on option '{option.GetDisplayName()}' in command '{commandName}' is reserved for help. Rename or remove the short name.");
            }

            if (option.ShortName == 'V')
            {
                throw new InvalidOperationException(
                    $"Reserved short name conflict: '-V' on option '{option.GetDisplayName()}' in command '{commandName}' is reserved for version. Rename or remove the short name.");
            }
        }
    }

    /// <summary>
    /// Creates an independent copy of an option for global use.
    /// <c>OwnerCommand</c> keeps the definition site, <c>ValidValues</c> is
    /// copied, and the caller assigns <c>BindTarget</c>.
    /// </summary>
    internal static SubCommandOptionInfo CreateGlobalOptionCopy(SubCommandOptionInfo original)
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
            ValidValues = original.ValidValues is null ? null : [.. original.ValidValues],
            IsCaseSensitive = original.IsCaseSensitive,
            IsSecret = original.IsSecret,
            IsGlobal = true,
            IsInherited = true,
            OwnerCommand = original.OwnerCommand,
            BindTarget = original.BindTarget
        };
    }
}
