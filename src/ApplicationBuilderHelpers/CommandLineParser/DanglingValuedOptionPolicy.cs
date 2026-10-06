using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>One dangling-valued rule, two hooks: eager empty-=-form vs late bare occurrence.</summary>
internal static class DanglingValuedOptionPolicy
{
    /// <summary>Eager hook: whether the token is an empty =-form of the valued option; always beats help. Late twin: <c>HasBareValuedOccurrence</c>.</summary>
    internal static bool IsEmptyEqualsForm(SubCommandOptionInfo option, string token)
    {
        if (option.IsFlag || IsBareForm(option, token))
            return false;
        if (option.LongName != null && token.StartsWith($"--{option.LongName}=", StringComparison.Ordinal))
            return token.Length == $"--{option.LongName}=".Length;
        if (option.ShortName.HasValue && token.StartsWith($"-{option.ShortName}=", StringComparison.Ordinal))
            return token.Length == $"-{option.ShortName}=".Length;
        return false;
    }

    /// <summary>Whether the token is the exact bare form of the valued option.</summary>
    internal static bool IsBareForm(SubCommandOptionInfo option, string token)
    {
        if (option.LongName != null && token == $"--{option.LongName}")
            return true;
        if (option.ShortName.HasValue && token == $"-{option.ShortName}")
            return true;
        return false;
    }

    /// <summary>Late hook: parse carries a dangling valued option only an error path can settle; help must not forgive it. Eager twin: <c>IsEmptyEqualsForm</c>. Abstract paths return false: abstract validates nothing late. Version is never cleared here, so version beats dangling by construction.</summary>
    internal static bool HasBareValuedOccurrence(ParseResult result)
    {
        if (!result.ShowHelp || !result.TargetCommand.HasImplementation)
            return false;
        foreach (var key in result.BareOptionOccurrences)
        {
            if (!result.TryGetCanonicalIdentityOption(key, out var option) || option.IsFlag)
                continue;
            if (option.IsCollection && result.TryGetMergedOptionValues(option, out _))
                continue;
            return true;
        }

        return false;
    }
}
