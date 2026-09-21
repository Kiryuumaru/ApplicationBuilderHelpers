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
            // Use auto-detection for null ExecutableDescription
            Description = commandBuilder.ExecutableDescription ?? AssemblyHelpers.GetAutoDetectedExecutableDescription()
        };
        _allCommands.Clear();

        // Process all commands and build hierarchy. Each Build resolves a
        // per-run instance so type-registered commands cannot leak bound values
        // across repeated RunAsync calls on one builder; caller-supplied
        // instance registrations keep identity.
        foreach (var typedCommandHolder in commandBuilder.Commands)
        {
            _ = typedCommandHolder.CommandType.GetCustomAttribute<CommandAttribute>();
            var runCommand = typedCommandHolder.CreateRunInstance();
            var typeDescriptor = reflectionCache.GetOrAdd(typedCommandHolder.CommandType);

            // Create SubCommandInfo for this command
            var subCommandInfo = SubCommandInfo.FromCommand(typedCommandHolder.CommandType, runCommand);

            // Extract fresh per-run options and arguments from cached descriptors
            subCommandInfo.Options = typeDescriptor.Options
                .Select(descriptor => SubCommandOptionInfo.FromDescriptor(
                    descriptor,
                    subCommandInfo,
                    SubCommandOptionInfo.ResolveValidValues(descriptor, typeParserCollection)))
                .ToList();
            subCommandInfo.Arguments = typeDescriptor.Arguments
                .Select(descriptor => SubCommandArgumentInfo.FromDescriptor(descriptor, subCommandInfo))
                .ToList();

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
            if (RootCommand!.HasImplementation)
                throw new InvalidOperationException("Cannot have more than one root command");

            RootCommand.Command = commandInfo.Command;

            // Add ALL options from the root command (both BaseCommand and MainCommand options)
            RootCommand.Options.AddRange(commandInfo.Options);
            RootCommand.Arguments.AddRange(commandInfo.Arguments);
            return;
        }

        // Navigate to the correct parent and create intermediate commands if needed
        var current = RootCommand!;

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
        var leafDescriptor = reflectionCache.GetOrAdd(leafCommandType);
        var intermediateCommandInfo = FindAbstractBaseCommandInfo(commandParts, leafCommandType, leafDescriptor);

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
        if (RootCommand != null)
        {
            foreach (var globalOption in RootCommand.Options.Where(o => o.IsGlobal))
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
    private SubCommandInfo? FindAbstractBaseCommandInfo(string[] commandParts, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type leafCommandType, CommandTypeDescriptor leafDescriptor)
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

                // Materialize only the members declared directly on the matched
                // abstract base: filter the leaf descriptor by DeclaringType
                // instead of re-walking properties via reflection.
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
                    .Select(descriptor => SubCommandArgumentInfo.FromDescriptor(descriptor, baseCommandInfo))
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
                    // These options appear in ALL commands with identical signatures
                    foreach (var (option, _) in optionInfos)
                    {
                        option.IsGlobal = true;
                        option.IsInherited = true;
                    }

                    // Add one instance to root command
                    if (!RootCommand!.Options.Any(o => o.GetDisplayName() == signature))
                    {
                        var globalOption = CreateGlobalOptionCopy(firstOption);
                        // OwnerCommand stays at the definition site (firstOption's
                        // owning command) so default-value reads resolve the
                        // declaring command instance; BindTarget records the
                        // scope holding this copy (root). Step 1: no behavior change.
                        globalOption.BindTarget = RootCommand;
                        RootCommand.Options.Add(globalOption);
                    }
                }
            }
        }

        // Add built-in global options (help only - version is not global)
        AddBuiltInGlobalOptions();
    }

    /// <summary>
    /// Compares initializer defaults across definition sites. The
    /// <see cref="SubCommandOptionInfo.DefaultValue"/> snapshot is not populated
    /// at build time, so divergence is read from the per-run command instances
    /// (which carry the C# initializer defaults), mirroring
    /// <c>HelpFormatter.GetOptionDefaultValue</c>. For caller-supplied instance
    /// registrations the per-run instance is the shared mutable registration:
    /// a prior run's binding may already have replaced the initializer, so the
    /// comparison uses the holder's registration-time snapshot (captured on the
    /// first gate read, before binding can mutate the instance). Any read
    /// failure blocks promotion (stays local) rather than risking a wrong
    /// global merge.
    /// </summary>
    private bool InitializerValuesEqual(SubCommandOptionInfo option, SubCommandOptionInfo firstOption)
    {
        try
        {
            var current = ReadInitializerValue(option);
            var first = ReadInitializerValue(firstOption);
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
    /// Reads one option's initializer default for the promotion gate: the
    /// holder's registration-time snapshot for caller-supplied instance
    /// registrations (fail-closed when the snapshot is missing or unreadable),
    /// otherwise the live per-run instance value.
    /// </summary>
    private object? ReadInitializerValue(SubCommandOptionInfo option)
    {
        if (option.OwnerCommand?.Command is null)
            return null;

        var holder = FindHolder(option.OwnerCommand.Command.GetType());
        if (holder?.TryGetInitializerDefault(option.Property, out var snapshot) == true)
            return snapshot;

        if (holder is { IsInstanceRegistration: true })
            return InitializerValuesUnreadable.Value;

        return option.Property.GetValue(option.OwnerCommand.Command);
    }

    /// <summary>
    /// Sentinel that never equals a real initializer default, so a missing or
    /// unreadable instance-registration snapshot blocks promotion (stays local).
    /// </summary>
    private sealed class InitializerValuesUnreadable
    {
        public static readonly InitializerValuesUnreadable Value = new();
        private InitializerValuesUnreadable() { }
    }

    /// <summary>
    /// Finds the registration holder for a command type. Multiple registrations
    /// of one type are rejected elsewhere (duplicate-command validation), so
    /// first match is the definition site.
    /// </summary>
    private Models.TypedCommandHolder? FindHolder(Type commandType)
    {
        foreach (var holder in commandBuilder.Commands)
        {
            if (holder.CommandType == commandType)
                return holder;
        }

        return null;
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
            OwnerCommand = RootCommand,
            BindTarget = RootCommand,
            Property = dummyProperty,
            // Set PropertyType directly to avoid AOT warnings
            PropertyType = typeof(bool)
        };

        if (!RootCommand!.Options.Any(o => o.GetDisplayName() == helpOption.GetDisplayName()))
        {
            RootCommand.Options.Add(helpOption);
        }

        // Mark help as global in all commands
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
    /// Creates a copy of an option for global use. Step 1: the copy is a
    /// frozen snapshot — OwnerCommand stays at the definition site (copied
    /// from the original), ValidValues is defensively copied so later
    /// mutation cannot flow between scopes, and the caller sets BindTarget
    /// to the scope holding the copy. No behavior change.
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
