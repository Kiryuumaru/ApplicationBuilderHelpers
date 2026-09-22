using ApplicationBuilderHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Immutable snapshot of a single <see cref="CommandOptionAttribute"/>-backed property.
/// Stores only pure-reflection data: the <see cref="PropertyInfo"/> plus copied
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
/// Stores only pure-reflection data: the <see cref="PropertyInfo"/> plus copied
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
/// Arguments are ordered by <c>Position</c>, mirroring
/// <c>SubCommandArgumentInfo.FromCommandType</c>. Options preserve base-first
/// property order, mirroring <c>SubCommandOptionInfo.FromCommandType</c>.
/// </summary>
/// <param name="CommandType">The reflected command type.</param>
/// <param name="Options">Frozen option snapshots in base-first property order.</param>
/// <param name="Arguments">Frozen argument snapshots ordered by position.</param>
internal sealed record CommandTypeDescriptor(
    Type CommandType,
    CommandOptionDescriptor[] Options,
    CommandArgumentDescriptor[] Arguments);

/// <summary>
/// S9 cache core: pure-reflection snapshot cache keyed by command <see cref="Type"/>.
/// The factory performs reflection only and never consults type parsers, command
/// instances, or hierarchy state. Effective required is
/// <c>Required || IsRequiredByKeyword</c> (mirroring
/// <c>SubCommandOptionInfo.FromProperty</c> / <c>SubCommandArgumentInfo.FromProperty</c>);
/// enum auto-populate decisions stay with the parser layer, which reads
/// <c>EnumCandidateType</c> / <c>EnumCandidateNames</c>.
/// Never stored here: ICommand, SubCommandInfo, IsGlobal/IsInherited, OwnerCommand,
/// or parser-derived ValidValues.
/// </summary>
internal sealed class CommandReflectionCache
{
    private readonly TypePlanCache<CommandTypeDescriptor> _plans = new();

    /// <summary>
    /// Number of times the reflection factory ran (cache misses). Test hook.
    /// </summary>
    internal int BuildCount => _plans.BuildCount;

    /// <summary>
    /// Gets the cached descriptor for the command type, building it once on first use.
    /// The miss counter increments exactly when this cache populates a new entry.
    /// Shares the double-checked-lock core in <see cref="TypePlanCache{TValue}"/>
    /// via the annotated <see cref="PlanFactory{TValue}"/> delegate hop
    /// (method-group delegate creation reports IL2111, suppressed explicitly
    /// below: the target is statically referenced, never reflection-invoked
    /// by name).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Method-group Build is statically referenced, never reflection-invoked by name; the All-annotated type flows via the annotated PlanFactory delegate.")]
    internal CommandTypeDescriptor GetOrAdd([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        return _plans.GetOrAdd(commandType, Build);
    }

    /// <summary>
    /// Pure-reflection factory: snapshots options and arguments for the command type.
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
    /// Single owned property walk: full BaseType chain base-first. Carries
    /// <c>All</c> so the full BaseType loop (which reflects off
    /// <c>BaseType</c> hops) and every full-walk caller flow without trim
    /// warnings. The declared-only walk lives in
    /// <see cref="WalkDeclaredOnly(Type)"/> with its own narrow annotation
    /// so neither path needs a suppression; filtering by descriptor is
    /// deliberately not offered (override/hide members must keep their
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
    /// <c>FromDeclaredType</c> shims' own annotations, so those callers
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
