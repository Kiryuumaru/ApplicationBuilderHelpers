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
}
