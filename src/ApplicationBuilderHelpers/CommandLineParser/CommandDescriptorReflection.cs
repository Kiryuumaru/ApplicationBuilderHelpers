using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Descriptor predicates shared by
/// <see cref="SubCommandOptionInfo"/>, <see cref="SubCommandArgumentInfo"/>
/// and <see cref="CommandReflectionCache"/>: the C# <c>required</c>-keyword
/// query and the nullable-unwrap/enum-candidate
/// query, plus the single-letter-long short-resolution rule. Performs no walk, no ordering, and keeps no parser-derived state.
/// </summary>
internal static class CommandDescriptorReflection
{
    /// <summary>
    /// Checks if a property has the C# required keyword by looking for RequiredMemberAttribute.
    /// </summary>
    internal static bool IsPropertyRequired(PropertyInfo property)
    {
#if NET7_0_OR_GREATER
        var hasRequiredMemberAttribute = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
#else
        var hasRequiredMemberAttribute = property.GetCustomAttributes()
            .Any(attr => attr.GetType().Name == "RequiredMemberAttribute");
#endif

        return hasRequiredMemberAttribute;
    }

    /// <summary>
    /// Resolves the effective short flag: an explicit short wins; otherwise a
    /// single-character long name doubles as the short (case preserved).
    /// </summary>
    internal static char? ResolveShortName(string? longName, char? shortTerm)
    {
        if (shortTerm.HasValue)
            return shortTerm;
        if (longName?.Length == 1)
            return longName[0];
        return null;
    }

    /// <summary>
    /// Unwraps Nullable&lt;T&gt; and snapshots Enum.GetNames for enum types.
    /// No type-parser check here: the parser layer decides auto-populate.
    /// </summary>
    internal static (Type? CandidateType, string[]? CandidateNames) GetEnumCandidate(Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (!targetType.IsEnum)
        {
            return (null, null);
        }

        return (targetType, [.. Enum.GetNames(targetType)]);
    }
}
