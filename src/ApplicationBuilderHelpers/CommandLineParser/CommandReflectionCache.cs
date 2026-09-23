using ApplicationBuilderHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Immutable snapshot of a single <see cref="CommandOptionAttribute"/>-backed property.
/// Stores only reflection data: the <see cref="PropertyInfo"/> plus copied
/// attribute primitives, the declaring type, the C# <c>required</c>-keyword result,
/// and the enum-candidate snapshot. Never stores parser-derived state.
/// </summary>
/// <param name="Property">The bound property.</param>
/// <param name="PropertyType">The property type (<c>Property.PropertyType</c> copy).</param>
/// <param name="DeclaringType">The property declaring type.</param>
/// <param name="ShortName">Copy of <c>CommandOptionAttribute.ShortTerm</c>.</param>
/// <param name="LongName">Resolved copy: <c>attribute.Term ?? property.Name.ToLowerInvariant()</c>.</param>
/// <param name="Description">Copy of <c>CommandOptionAttribute.Description</c>.</param>
/// <param name="Required">Copy of <c>CommandOptionAttribute.Required</c> (attribute only).</param>
/// <param name="EnvironmentVariable">Copy of <c>CommandOptionAttribute.EnvironmentVariable</c>.</param>
/// <param name="FromAmong">Frozen copy of <c>CommandOptionAttribute.FromAmong</c> (null when empty).</param>
/// <param name="IsCaseSensitive">Copy of <c>CommandOptionAttribute.CaseSensitive</c>.</param>
/// <param name="IsSecret">Copy of <c>CommandOptionAttribute.Secret</c>.</param>
/// <param name="IsRequiredByKeyword">C# <c>required</c>-keyword result (RequiredMember semantics).</param>
/// <param name="EnumCandidateType">Unwrapped enum type (nullable unwrapped) or null.</param>
/// <param name="EnumCandidateNames">Frozen <c>Enum.GetNames</c> copy or null.</param>
internal sealed record CommandOptionDescriptor(
    PropertyInfo Property,
    Type PropertyType,
    Type? DeclaringType,
    char? ShortName,
    string? LongName,
    string? Description,
    bool Required,
    string? EnvironmentVariable,
    object[]? FromAmong,
    bool IsCaseSensitive,
    bool IsSecret,
    bool IsRequiredByKeyword,
    Type? EnumCandidateType,
    string[]? EnumCandidateNames);

/// <summary>
/// Immutable snapshot of a single <see cref="CommandArgumentAttribute"/>-backed property.
/// Stores only reflection data: the <see cref="PropertyInfo"/> plus copied
/// attribute primitives, the declaring type, the C# <c>required</c>-keyword result,
/// and the enum-candidate snapshot. Never stores parser-derived state.
/// </summary>
/// <param name="Property">The bound property.</param>
/// <param name="PropertyType">The property type (<c>Property.PropertyType</c> copy).</param>
/// <param name="DeclaringType">The property declaring type.</param>
/// <param name="Name">Resolved copy: <c>attribute.Name ?? property.Name.ToLowerInvariant()</c>.</param>
/// <param name="Description">Copy of <c>CommandArgumentAttribute.Description</c>.</param>
/// <param name="Position">Copy of <c>CommandArgumentAttribute.Position</c>.</param>
/// <param name="Required">Copy of <c>CommandArgumentAttribute.Required</c> (attribute only).</param>
/// <param name="FromAmong">Frozen copy of <c>CommandArgumentAttribute.FromAmong</c> (null when empty).</param>
/// <param name="IsCaseSensitive">Copy of <c>CommandArgumentAttribute.CaseSensitive</c>.</param>
/// <param name="IsSecret">Copy of <c>CommandArgumentAttribute.Secret</c>.</param>
/// <param name="IsRequiredByKeyword">C# <c>required</c>-keyword result (RequiredMember semantics).</param>
/// <param name="EnumCandidateType">Unwrapped enum type (nullable unwrapped) or null.</param>
/// <param name="EnumCandidateNames">Frozen <c>Enum.GetNames</c> copy or null.</param>
internal sealed record CommandArgumentDescriptor(
    PropertyInfo Property,
    Type PropertyType,
    Type? DeclaringType,
    string? Name,
    string? Description,
    int Position,
    bool Required,
    object[]? FromAmong,
    bool IsCaseSensitive,
    bool IsSecret,
    bool IsRequiredByKeyword,
    Type? EnumCandidateType,
    string[]? EnumCandidateNames);

/// <summary>
/// Immutable snapshot of one command type: its option and argument descriptors.
/// Arguments are ordered by <c>Position</c> (see
/// <c>SubCommandArgumentInfo.FromCommandType</c>). Options preserve base-first
/// property order (see <c>SubCommandOptionInfo.FromCommandType</c>).
/// </summary>
/// <param name="CommandType">The reflected command type.</param>
/// <param name="Options">Frozen option snapshots in base-first property order.</param>
/// <param name="Arguments">Frozen argument snapshots ordered by position.</param>
internal sealed record CommandTypeDescriptor(
    Type CommandType,
    CommandOptionDescriptor[] Options,
    CommandArgumentDescriptor[] Arguments);

/// <summary>
/// Reflection snapshot cache keyed by command <see cref="Type"/>.
/// The factory performs reflection only. Effective required is
/// <c>Required || IsRequiredByKeyword</c> (see
/// <c>SubCommandOptionInfo.FromProperty</c> / <c>SubCommandArgumentInfo.FromProperty</c>);
/// enum auto-population stays in the parser layer with
/// <c>EnumCandidateType</c> / <c>EnumCandidateNames</c>.
/// Excluded: ICommand, SubCommandInfo, IsGlobal/IsInherited, OwnerCommand/BindTarget,
/// parser-derived ValidValues.
/// </summary>
internal sealed class CommandReflectionCache
{
    private readonly TypePlanCache<CommandTypeDescriptor> _plans = new();

    /// <summary>
    /// Number of times the reflection factory ran (cache misses).
    /// </summary>
    internal int BuildCount => _plans.BuildCount;

    /// <summary>
    /// Gets the cached descriptor for the command type, building it once on first use.
    /// Shares the core in <see cref="TypePlanCache{TValue}"/>
    /// with the annotated <see cref="PlanFactory{TValue}"/> delegate.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Method-group Build is statically referenced, never reflection-invoked by name; the All-annotated type flows through the annotated PlanFactory delegate.")]
    internal CommandTypeDescriptor GetOrAdd([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        return _plans.GetOrAdd(commandType, Build);
    }

    /// <summary>
    /// Reflection factory: snapshots options and arguments for the command type.
    /// Bound detection uses the canonical <see cref="IsCliBound"/> predicate.
    /// </summary>
    private static CommandTypeDescriptor Build([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        var properties = Walk(commandType);

        var options = new List<CommandOptionDescriptor>();
        var arguments = new List<CommandArgumentDescriptor>();

        foreach (var property in properties)
        {
            if (!IsCliBound(property))
            {
                continue;
            }

            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>();
            if (optionAttr != null)
            {
                options.Add(FromOptionProperty(property, optionAttr));
            }

            var argumentAttr = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (argumentAttr != null)
            {
                arguments.Add(FromArgumentProperty(property, argumentAttr));
            }
        }

        return new CommandTypeDescriptor(
            commandType,
            [.. options],
            [.. arguments.OrderBy(a => a.Position)]);
    }

    private static CommandOptionDescriptor FromOptionProperty(PropertyInfo property, CommandOptionAttribute attribute)
    {
        var (enumCandidateType, enumCandidateNames) = CommandDescriptorReflection.GetEnumCandidate(property.PropertyType);

        return new CommandOptionDescriptor(
            property,
            property.PropertyType,
            property.DeclaringType,
            attribute.ShortTerm,
            attribute.Term ?? property.Name.ToLowerInvariant(),
            attribute.Description,
            attribute.Required,
            attribute.EnvironmentVariable,
            attribute.FromAmong?.Length > 0 ? [.. attribute.FromAmong] : null,
            attribute.CaseSensitive,
            attribute.Secret,
            CommandDescriptorReflection.IsPropertyRequired(property),
            enumCandidateType,
            enumCandidateNames);
    }

    private static CommandArgumentDescriptor FromArgumentProperty(PropertyInfo property, CommandArgumentAttribute attribute)
    {
        var (enumCandidateType, enumCandidateNames) = CommandDescriptorReflection.GetEnumCandidate(property.PropertyType);

        return new CommandArgumentDescriptor(
            property,
            property.PropertyType,
            property.DeclaringType,
            attribute.Name ?? property.Name.ToLowerInvariant(),
            attribute.Description,
            attribute.Position,
            attribute.Required,
            attribute.FromAmong?.Length > 0 ? [.. attribute.FromAmong] : null,
            attribute.CaseSensitive,
            attribute.Secret,
            CommandDescriptorReflection.IsPropertyRequired(property),
            enumCandidateType,
            enumCandidateNames);
    }

    /// <summary>
    /// Canonical CLI-bound identity: a property is CLI-bound iff it carries
    /// a <see cref="CommandOptionAttribute"/> or a
    /// <see cref="CommandArgumentAttribute"/>. Owned next to
    /// <see cref="Walk(Type)"/> so the reflection cache and the injection
    /// plan share one predicate; no NAME-string comparison.
    /// </summary>
    internal static bool IsCliBound(PropertyInfo property)
    {
        return property.IsDefined(typeof(CommandOptionAttribute), inherit: true)
            || property.IsDefined(typeof(CommandArgumentAttribute), inherit: true);
    }

    /// <summary>
    /// Single property walk: full BaseType chain base-first. Holds
    /// <c>All</c> so the full BaseType loop (which reflects off
    /// <c>BaseType</c> hops) and every full-walk caller flow without trim
    /// warnings. The declared-only walk is in
    /// <see cref="WalkDeclaredOnly(Type)"/> with its own narrow annotation
    /// so neither path needs a suppression; filtering by descriptor is
    /// not offered (override/hide members must keep their
    /// duplicate walk entries). Shared by SubCommandOptionInfo /
    /// SubCommandArgumentInfo so the walk exists once.
    /// </summary>
    internal static List<PropertyInfo> Walk([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        var properties = new List<PropertyInfo>();
        var currentType = type;

        while (currentType != null)
        {
            var declaredProperties = currentType.GetProperties(
                BindingFlags.DeclaredOnly |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            properties.AddRange((declaredProperties as IEnumerable<PropertyInfo>).Reverse());
            currentType = currentType.BaseType;
        }

        properties.Reverse();
        return properties;
    }

    /// <summary>
    /// Declared-only walk: the properties declared directly on
    /// <paramref name="type"/>. Carries only
    /// <c>PublicProperties | NonPublicProperties</c>, matching the leaf
    /// <c>FromDeclaredType</c> overloads' own annotations, so those callers
    /// flow without a trim suppression.
    /// </summary>
    internal static List<PropertyInfo> WalkDeclaredOnly([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
    {
        return [.. type.GetProperties(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance)];
    }
}
