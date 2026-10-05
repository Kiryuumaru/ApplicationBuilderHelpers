using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Walk-position scope queries: which node owns an option spelling.</summary>
/// <remarks>Answers scope questions with existing nodes only; the parser keeps consume/extract/break decisions.</remarks>
internal static class OptionScopeAuthority
{
    /// <summary>Non-global options on the current path node matching the token.</summary>
    internal static List<SubCommandOptionInfo> FindOwnedMatches(SubCommandInfo target, string token)
    {
        var owned = new List<SubCommandOptionInfo>();
        foreach (var option in target.AllOptions)
        {
            if (option.IsGlobal || !option.MatchesArgument(token))
                continue;
            if (!owned.Any(o => ReferenceEquals(o, option)))
                owned.Add(option);
        }

        return owned;
    }

    /// <summary>Leaf-owned option reachable below the current path; the single compatible owner.</summary>
    internal static SubCommandOptionInfo? FindLeafOwnedOption(SubCommandInfo target, string token)
    {
        SubCommandOptionInfo? owner = null;
        var ownerCommandName = string.Empty;
        var ambiguous = false;
        var stack = new Stack<SubCommandInfo>(target.Children.Values);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var option in current.Options)
            {
                if (option.IsGlobal || option.IsInherited)
                    continue;
                if (!option.MatchesArgument(token))
                    continue;
                if (owner == null)
                {
                    owner = option;
                    ownerCommandName = OwnerName(current);
                    continue;
                }
                if (string.Equals(OwnerName(current), ownerCommandName, StringComparison.Ordinal))
                    continue;
                if (!AreOptionsShapeCompatible(owner, option))
                {
                    ambiguous = true;
                    break;
                }
            }
            if (ambiguous)
                break;
            foreach (var child in current.Children.Values)
                stack.Push(child);
        }

        if (owner == null || ambiguous)
            return null;
        return owner;
    }

    /// <summary>Whether the child (or its subtree) binds the leaf-owned probe by canonical key.</summary>
    internal static bool OwnsOption(SubCommandInfo child, SubCommandOptionInfo probe)
    {
        var key = ParseResult.GetCanonicalOptionKey(probe);
        var stack = new Stack<SubCommandInfo>();
        stack.Push(child);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current.AllOptions.Any(o => string.Equals(ParseResult.GetCanonicalOptionKey(o), key, StringComparison.Ordinal)))
                return true;
            foreach (var next in current.Children.Values)
                stack.Push(next);
        }

        return false;
    }

    /// <summary>Canonical owner identity for a match: prefer the deepest leaf holding the option.</summary>
    private static string OwnerName(SubCommandInfo command) =>
        string.IsNullOrEmpty(command.FullCommandName) ? "<root>" : command.FullCommandName;

    /// <summary>Whether two options share one bindable shape; mismatched shapes stay ambiguous.</summary>
    private static bool AreOptionsShapeCompatible(SubCommandOptionInfo first, SubCommandOptionInfo second)
    {
        if (!string.Equals(ParseResult.GetCanonicalOptionKey(first), ParseResult.GetCanonicalOptionKey(second), StringComparison.Ordinal))
            return false;
        if (first.IsFlag != second.IsFlag)
            return false;
        if (first.IsCollection != second.IsCollection)
            return false;
        if (!Equals(first.PropertyType, second.PropertyType))
            return false;
        return true;
    }
}
