using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal sealed class CommandHierarchyBuilder(
    ICommandBuilder commandBuilder,
    ICommandTypeParserCollection typeParserCollection,
    CommandReflectionCache reflectionCache)
{
    public SubCommandInfo RootCommand { get; private set; } = null!;

    private readonly Dictionary<string, SubCommandInfo> _allCommands = [];

    private readonly Dictionary<string, SubCommandOptionInfo> _globalRegistry = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, SubCommandOptionInfo> GlobalRegistry => _globalRegistry;

    public IReadOnlyDictionary<string, SubCommandInfo> AllCommands => _allCommands;

    public void BuildCommandHierarchy()
    {
        RootCommand = new SubCommandInfo
        {
            CommandParts = [],
            Description = commandBuilder.ExecutableDescription ?? AssemblyHelpers.GetAutoDetectedExecutableDescription()
        };
        _allCommands.Clear();
        _globalRegistry.Clear();

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

        BuildGlobalOptionRegistry();
        DetermineGlobalOptions();
    }

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
                var canonicalKey = ParseResult.GetCanonicalOptionKey(globalOption);
                if (!result.Options.Any(o => ReferenceEquals(o, globalOption) || string.Equals(ParseResult.GetCanonicalOptionKey(o), canonicalKey, StringComparison.Ordinal)))
                {
                    result.Options.Add(globalOption);
                }
            }
        }

        return result;
    }

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

    private void BuildGlobalOptionRegistry()
    {
        foreach (var option in RootCommand.Options)
        {
            RegisterGlobalOption(option);
        }

        foreach (var command in _allCommands.Values)
        {
            foreach (var option in command.Options)
            {
                RegisterGlobalOption(option);
            }
        }
    }

    private void RegisterGlobalOption(SubCommandOptionInfo option)
    {
        if (!option.IsGlobal)
            return;
        if (string.Equals(option.LongName, "help", StringComparison.Ordinal)
            || string.Equals(option.LongName, "version", StringComparison.Ordinal))
            return;
        _globalRegistry.TryAdd(ParseResult.GetCanonicalOptionKey(option), option);
    }

    private void DetermineGlobalOptions()
    {
        if (_globalRegistry.Count > 0)
        {
            AddBuiltInGlobalOptions();
            return;
        }

        var promotedKeys = new HashSet<string>(StringComparer.Ordinal);
        var concreteCommands = _allCommands.Values.Where(c => c.HasImplementation).ToList();

        if (concreteCommands.Count == 0)
        {
            AddBuiltInGlobalOptions();
            return;
        }

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

                    _ = promotedKeys.Add(ParseResult.GetCanonicalOptionKey(firstOption));

                    if (!RootCommand!.Options.Any(o => o.GetDisplayName() == signature))
                    {
                        var globalOption = CreateGlobalOptionCopy(firstOption);
                        globalOption.BindTarget = RootCommand;
                        RootCommand.Options.Add(globalOption);
                    }
                    var promotedCopy = RootCommand.Options.First(o => o.GetDisplayName() == signature);
                    _ = _globalRegistry.TryAdd(ParseResult.GetCanonicalOptionKey(promotedCopy), promotedCopy);
                }
            }
        }

        if (promotedKeys.Count > 0)
            Debug.WriteLine($"[ABH] Global options resolved via legacy promotion fallback (no explicit [CommandOption(IsGlobal=true)] declaration found); promoted {promotedKeys.Count} option(s): {string.Join(",", promotedKeys.OrderBy(k => k, StringComparer.Ordinal))}. Declare the global on a common ancestor for single truth.");

        AddBuiltInGlobalOptions();
    }

    private bool InitializerValuesEqual(SubCommandOptionInfo option, SubCommandOptionInfo firstOption)
    {
        try
        {
            var current = ReadInitializerValue(option);
            var first = ReadInitializerValue(firstOption);
            if (current is InitializerValuesUnreadable || first is InitializerValuesUnreadable)
                return false;

            return Models.InitializerValueEquality.ValuesEqual(current, first);
        }
        catch
        {
            // Unreadable initializer blocks promotion (stays local) rather than a wrong global merge.
            return false;
        }
    }

    private object? ReadInitializerValue(SubCommandOptionInfo option)
    {
        var declaringType = option.Property.DeclaringType;
        if (declaringType is null)
            return InitializerValuesUnreadable.Value;

        var holder = FindHolder(declaringType);
        if (holder is null)
            return ReadSharedBaseValue(declaringType, option.Property);

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
            // Getter may throw on uninitialized registration instances: block promotion, stay local.
            return InitializerValuesUnreadable.Value;
        }
    }

    private sealed class InitializerValuesUnreadable
    {
        public static readonly InitializerValuesUnreadable Value = new();
        private InitializerValuesUnreadable() { }
    }

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

    private object? ReadSharedBaseValue(Type declaringType, System.Reflection.PropertyInfo optionProperty)
    {
        // A base that is itself a command owns the option on its node, so only pure shared bases promote.
        if (declaringType.IsAbstract && declaringType.GetCustomAttribute<Attributes.CommandAttribute>() is not null)
            return InitializerValuesUnreadable.Value;

        var seen = false;
        object? agreed = null;
        foreach (var holder in commandBuilder.Commands)
        {
            if (!declaringType.IsAssignableFrom(holder.CommandType))
                continue;

            var holderProperty = ResolveHolderProperty(holder, optionProperty);
            if (holderProperty is null)
                return InitializerValuesUnreadable.Value;

            object? candidate;
            if (holder.TryGetInitializerDefault(holderProperty, out var snapshot))
            {
                candidate = snapshot;
            }
            else if (holder.IsInstanceRegistration)
            {
                return InitializerValuesUnreadable.Value;
            }
            else
            {
                try
                {
                    candidate = holderProperty.GetValue(holder.Command);
                }
                catch
                {
                    return InitializerValuesUnreadable.Value;
                }
            }

            if (!seen)
            {
                agreed = Models.InitializerValueEquality.CloneIfArray(candidate);
                seen = true;
                continue;
            }

            if (!Models.InitializerValueEquality.ValuesEqual(agreed, candidate))
                return InitializerValuesUnreadable.Value;
        }

        return seen ? agreed : InitializerValuesUnreadable.Value;
    }

    private static System.Reflection.PropertyInfo? ResolveHolderProperty(Models.TypedCommandHolder holder, System.Reflection.PropertyInfo optionProperty)
    {
        if (holder.CommandType == optionProperty.DeclaringType)
            return optionProperty;

        var resolved = holder.CommandType.GetProperty(
            optionProperty.Name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (resolved is null || resolved.DeclaringType != optionProperty.DeclaringType)
            return null;

        return resolved;
    }

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

        ValidateDuplicateShortNames();
    }

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

    private void ValidateDuplicateShortNames()
    {
        if (RootCommand != null)
            ValidateDuplicateShortNames(RootCommand);

        foreach (var command in _allCommands.Values)
            ValidateDuplicateShortNames(command);
    }

    private static void ValidateDuplicateShortNames(SubCommandInfo command)
    {
        var commandName = string.IsNullOrEmpty(command.FullCommandName) ? "<root>" : command.FullCommandName;
        foreach (var group in command.AllOptions.Where(o => o.ShortName.HasValue).GroupBy(o => o.ShortName!.Value))
        {
            var distinct = group
                .GroupBy(ParseResult.GetCanonicalOptionKey, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(o => o.GetDisplayName(), StringComparer.Ordinal)
                .ToList();
            if (distinct.Count >= 2)
            {
                var names = string.Join("' and '", distinct.Select(o => o.GetDisplayName()));
                throw new InvalidOperationException(
                    $"Duplicate short name conflict: '-{group.Key}' on options '{names}' in command '{commandName}'. Rename or remove one of the short names.");
            }
        }
    }

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
