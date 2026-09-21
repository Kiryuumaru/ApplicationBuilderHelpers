using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Narrow leaf for pure-reflection descriptor predicates shared by
/// <see cref="SubCommandOptionInfo"/>, <see cref="SubCommandArgumentInfo"/>
/// and <see cref="CommandReflectionCache"/>: the C# <c>required</c>-keyword
/// query and the nullable-unwrap/enum-candidate
/// query. Owns no walk, no freeze, no ordering, and no parser-derived state.
/// </summary>
internal static class CommandDescriptorReflection
{
    /// <summary>
    /// Checks if a property has the C# required keyword by looking for RequiredMemberAttribute.
    /// </summary>
    internal static bool IsPropertyRequired(PropertyInfo property)
    {
        // Check for RequiredMemberAttribute which is added by the compiler when using the required keyword
#if NET7_0_OR_GREATER
        var hasRequiredMemberAttribute = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
#else
        var hasRequiredMemberAttribute = property.GetCustomAttributes()
            .Any(attr => attr.GetType().Name == "RequiredMemberAttribute");
#endif

        return hasRequiredMemberAttribute;
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
