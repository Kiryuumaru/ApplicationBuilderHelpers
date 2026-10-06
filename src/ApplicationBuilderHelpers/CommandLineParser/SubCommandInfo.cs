using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal class SubCommandInfo
{
    public string[] CommandParts { get; set; } = [];

    public string FullCommandName => string.Join(" ", CommandParts);

    public string DisplayName => IsRoot ? "<root>" : FullCommandName;

    public string Name => CommandParts.Length > 0 ? CommandParts[^1] : string.Empty;

    public string? Description { get; set; }

    public ICommand? Command { get; set; }

    public SubCommandInfo? Parent { get; set; }

    public Dictionary<string, SubCommandInfo> Children { get; set; } = [];

    public List<SubCommandOptionInfo> Options { get; set; } = [];

    public List<SubCommandArgumentInfo> Arguments { get; set; } = [];

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
            var leafType = Command?.GetType();
            while (current != null)
            {
                foreach (var option in current.Options.Where(o => o.IsGlobal || o.IsInherited))
                {
                    if (!option.IsGlobal && !IsBindableTo(option, leafType))
                        continue;
                    if (seen.Add(option))
                        allOptions.Add(option);
                }
                current = current.Parent;
            }
            return allOptions;
        }
    }

    private static bool IsBindableTo(SubCommandOptionInfo option, Type? leafType)
    {
        // Unknown scope is permissive; otherwise the declaring type must fit the leaf.
        if (leafType == null)
            return true;
        var declaringType = option.Property.DeclaringType;
        if (declaringType == null)
            return true;
        return declaringType.IsAssignableFrom(leafType);
    }

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

    public int Depth => CommandParts.Length;

    public bool IsLeaf => Children.Count == 0;

    public bool IsRoot => Parent == null && CommandParts.Length == 0;

    public bool HasImplementation => Command != null;

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

    public SubCommandInfo? FindChild(string name)
    {
        return Children.TryGetValue(name, out var child) ? child : null;
    }

    public void AddChild(SubCommandInfo child)
    {
        if (child.CommandParts.Length == 0)
            throw new ArgumentException("Child command must have a name");

        var childName = child.CommandParts[^1];
        child.Parent = this;
        Children[childName] = child;
    }

    public SubCommandInfo? FindCommand(string[] commandPath)
    {
        if (commandPath.Length == 0)
            return this;

        var nextName = commandPath[0];
        if (!Children.TryGetValue(nextName, out var child))
            return null;

        return child.FindCommand(commandPath[1..]);
    }

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

    public override string ToString()
    {
        return IsRoot ? "<root>" : FullCommandName;
    }
}
