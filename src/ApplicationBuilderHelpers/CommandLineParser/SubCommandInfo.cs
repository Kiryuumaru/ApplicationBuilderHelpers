using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Represents a subcommand in the command line parser hierarchy.
/// Supports cascading subcommands like "git submodule add" where submodule has subcommands.
/// </summary>
internal class SubCommandInfo
{
    /// <summary>
    /// The command name parts (e.g., ["git", "submodule", "add"] for "git submodule add")
    /// </summary>
    public string[] CommandParts { get; set; } = [];

    /// <summary>
    /// The full command name (e.g., "git submodule add")
    /// </summary>
    public string FullCommandName => string.Join(" ", CommandParts);

    /// <summary>
    /// Display name for error messages: "<root>" for the root command,
    /// otherwise the full command name. The structured
    /// <c>CommandException.CommandName</c> stays empty for root so the
    /// error footer remains global.
    /// </summary>
    public string DisplayName => IsRoot ? "<root>" : FullCommandName;

    /// <summary>
    /// The last part of the command name (e.g., "add" for "git submodule add")
    /// </summary>
    public string Name => CommandParts.Length > 0 ? CommandParts[^1] : string.Empty;

    /// <summary>
    /// Description of the subcommand
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The actual command implementation
    /// </summary>
    public ICommand? Command { get; set; }

    /// <summary>
    /// Parent subcommand in the hierarchy (null for root commands)
    /// </summary>
    public SubCommandInfo? Parent { get; set; }

    /// <summary>
    /// Child subcommands
    /// </summary>
    public Dictionary<string, SubCommandInfo> Children { get; set; } = [];

    /// <summary>
    /// Options specific to this command level
    /// </summary>
    public List<SubCommandOptionInfo> Options { get; set; } = [];

    /// <summary>
    /// Arguments specific to this command level
    /// </summary>
    public List<SubCommandArgumentInfo> Arguments { get; set; } = [];

    /// <summary>
    /// Gets all options including inherited from parent commands
    /// </summary>
    public List<SubCommandOptionInfo> AllOptions
    {
        get
        {
            var allOptions = new List<SubCommandOptionInfo>();
            var seen = new HashSet<SubCommandOptionInfo>();
            foreach (var option in Options)
            {
                if (seen.Add(option))
                    allOptions.Add(option);
            }
            var current = Parent;
            while (current != null)
            {
                foreach (var option in current.Options.Where(o => o.IsGlobal || o.IsInherited))
                {
                    if (seen.Add(option))
                        allOptions.Add(option);
                }
                current = current.Parent;
            }
            return allOptions;
        }
    }

    /// <summary>
    /// Gets all arguments including inherited from parent commands
    /// </summary>
    public List<SubCommandArgumentInfo> AllArguments
    {
        get
        {
            var allArguments = new List<SubCommandArgumentInfo>(Arguments);
            var current = Parent;
            while (current != null)
            {
                allArguments.AddRange(current.Arguments.Where(a => a.IsGlobal || a.IsInherited));
                current = current.Parent;
            }
            return [.. allArguments.OrderBy(a => a.Position)];
        }
    }

    /// <summary>
    /// Depth in the command hierarchy (0 for root, 1 for first level subcommands, etc.)
    /// </summary>
    public int Depth => CommandParts.Length;

    /// <summary>
    /// Whether this is a leaf command (has no children)
    /// </summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>
    /// Whether this is the root command
    /// </summary>
    public bool IsRoot => Parent == null && CommandParts.Length == 0;

    /// <summary>
    /// Whether this command has an associated implementation
    /// </summary>
    public bool HasImplementation => Command != null;

    /// <summary>
    /// Creates a SubCommandInfo from a command type
    /// </summary>
    public static SubCommandInfo FromCommand(Type commandType, ICommand? commandInstance = null)
    {
        var commandAttr = commandType.GetCustomAttribute<CommandAttribute>();
        var term = commandAttr?.Term;
        if (term is not null && string.IsNullOrWhiteSpace(term))
            throw new InvalidOperationException($"Invalid command term on '{commandType.FullName}': term must not be empty or whitespace.");
        var commandParts = term?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        foreach (var part in commandParts)
        {
            if (part.StartsWith("-", StringComparison.Ordinal))
                throw new InvalidOperationException($"Invalid command term '{term}' on '{commandType.FullName}': command names must not start with '-'.");
        }
        
        return new SubCommandInfo
        {
            CommandParts = commandParts,
            Description = commandAttr?.Description,
            Command = commandInstance
        };
    }

    /// <summary>
    /// Finds a child subcommand by name
    /// </summary>
    public SubCommandInfo? FindChild(string name)
    {
        return Children.TryGetValue(name, out var child) ? child : null;
    }

    /// <summary>
    /// Adds a child subcommand
    /// </summary>
    public void AddChild(SubCommandInfo child)
    {
        if (child.CommandParts.Length == 0)
            throw new ArgumentException("Child command must have a name");

        var childName = child.CommandParts[^1];
        child.Parent = this;
        Children[childName] = child;
    }

    /// <summary>
    /// Finds a descendant command by following the command path
    /// </summary>
    public SubCommandInfo? FindCommand(string[] commandPath)
    {
        if (commandPath.Length == 0)
            return this;

        var nextName = commandPath[0];
        if (!Children.TryGetValue(nextName, out var child))
            return null;

        return child.FindCommand(commandPath[1..]);
    }

    /// <summary>
    /// Gets the command path from root to this command
    /// </summary>
    public string[] GetPathFromRoot()
    {
        var path = new List<string>();
        var current = this;
        
        while (current != null && !current.IsRoot)
        {
            if (current.CommandParts.Length > 0)
                path.Insert(0, current.Name);
            current = current.Parent;
        }
        
        return [.. path];
    }

    /// <summary>
    /// Validates the subcommand hierarchy
    /// </summary>
    public void Validate()
    {
        if (HasImplementation && !IsLeaf)
        {
        }

        foreach (var child in Children.Values)
        {
            child.Validate();
        }

        ValidateOptionInheritance();

        ValidateArgumentInheritance();
    }

    private void ValidateOptionInheritance()
    {
        var inheritedOptions = new HashSet<string>();
        var current = Parent;
        
        while (current != null)
        {
            foreach (var option in current.Options.Where(o => o.IsGlobal || o.IsInherited))
            {
                var optionKey = option.LongName ?? option.ShortName?.ToString();
                if (optionKey != null)
                    inheritedOptions.Add(optionKey);
            }
            current = current.Parent;
        }

        foreach (var option in Options)
        {
            var optionKey = option.LongName ?? option.ShortName?.ToString();
            if (optionKey != null && inheritedOptions.Contains(optionKey))
            {
                if (!option.IsInherited)
                {
                    throw new InvalidOperationException(
                        $"Option conflict: '{optionKey}' is already defined in parent command hierarchy for command '{FullCommandName}'");
                }
            }
        }
    }

    private void ValidateArgumentInheritance()
    {
        var inheritedPositions = new HashSet<int>();
        var current = Parent;
        
        while (current != null)
        {
            foreach (var argument in current.Arguments.Where(a => a.IsGlobal || a.IsInherited))
            {
                inheritedPositions.Add(argument.Position);
            }
            current = current.Parent;
        }

        foreach (var argument in Arguments)
        {
            if (inheritedPositions.Contains(argument.Position))
            {
                throw new InvalidOperationException(
                    $"Argument conflict: Position {argument.Position} is already used in parent command hierarchy for command '{FullCommandName}'");
            }
        }
    }

    /// <summary>
    /// Returns a string representation of the subcommand
    /// </summary>
    public override string ToString()
    {
        return IsRoot ? "<root>" : FullCommandName;
    }
}
