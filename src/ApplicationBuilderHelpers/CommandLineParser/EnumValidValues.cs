using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Single shared enum auto-population predicate for options and arguments
/// (Option B, issue #478). Explicit <c>FromAmong</c> wins; else the
/// frozen/live enum names iff a candidate exists and no live parser is
/// registered for that enum type; else null. Parser policy stays here in
/// the parser layer (per ADR-0003); the leaf
/// (<see cref="CommandDescriptorReflection"/>) only supplies the candidate.
/// Large enums show the full list; suppression is symmetric across kinds.
/// </summary>
internal static class EnumValidValues
{
    /// <summary>
    /// Single enum predicate (live overload): unwraps via the leaf's
    /// <c>GetEnumCandidate</c> then delegates to the frozen overload.
    /// </summary>
    internal static object[]? Resolve(Type propertyType, object[]? fromAmong, ICommandTypeParserCollection? typeParserCollection)
    {
        var (candidateType, candidateNames) = CommandDescriptorReflection.GetEnumCandidate(propertyType);
        return Resolve(candidateType, candidateNames, fromAmong, typeParserCollection);
    }

    /// <summary>
    /// Single enum predicate (frozen overload): explicit <c>FromAmong</c> wins;
    /// else the frozen enum names iff a candidate exists and no live parser
    /// exists for that enum type in the live collection; else null.
    /// </summary>
    internal static object[]? Resolve(Type? enumCandidateType, string[]? enumCandidateNames, object[]? fromAmong, ICommandTypeParserCollection? typeParserCollection)
    {
        if (fromAmong is { Length: > 0 })
        {
            return [.. fromAmong];
        }

        if (enumCandidateType is null || enumCandidateNames is null)
        {
            return null;
        }

        if (typeParserCollection?.TypeParsers.ContainsKey(enumCandidateType) == true)
        {
            return null;
        }

        return [.. enumCandidateNames];
    }
}
