using ApplicationBuilderHelpers.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

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
    private readonly ConcurrentDictionary<Type, CommandTypeDescriptor> _cache = new();
    private readonly object _syncRoot = new();
    private int _buildCount;

    /// <summary>
    /// Number of times the reflection factory ran (cache misses). Test hook.
    /// </summary>
    internal int BuildCount => _buildCount;

    /// <summary>
    /// Gets the cached descriptor for the command type, building it once on first use.
    /// The miss counter increments exactly when this cache populates a new entry.
    /// Uses a double-checked lock (instead of a GetOrAdd value-factory
    /// delegate) so the trimmer sees the annotated type flow directly into
    /// <see cref="Build(Type)"/> with no reflection-invoked delegate hop.
    /// </summary>
    internal CommandTypeDescriptor GetOrAdd([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        if (_cache.TryGetValue(commandType, out var cached))
        {
            return cached;
        }

        lock (_syncRoot)
        {
            if (_cache.TryGetValue(commandType, out cached))
            {
                return cached;
            }

            var built = Build(commandType);
            _cache[commandType] = built;
            Interlocked.Increment(ref _buildCount);
            return built;
        }
    }

    /// <summary>
    /// Pure-reflection factory: snapshots options and arguments for the command type.
    /// </summary>
    private static CommandTypeDescriptor Build([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        var properties = GetAllProperties(commandType);

        var options = new List<CommandOptionDescriptor>();
        var arguments = new List<CommandArgumentDescriptor>();

        foreach (var property in properties)
        {
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
        var (enumCandidateType, enumCandidateNames) = GetEnumCandidate(property.PropertyType);

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
            IsPropertyRequired(property),
            enumCandidateType,
            enumCandidateNames);
    }

    private static CommandArgumentDescriptor FromArgumentProperty(PropertyInfo property, CommandArgumentAttribute attribute)
    {
        var (enumCandidateType, enumCandidateNames) = GetEnumCandidate(property.PropertyType);

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
            IsPropertyRequired(property),
            enumCandidateType,
            enumCandidateNames);
    }

    /// <summary>
    /// Checks if a property has the C# required keyword by looking for RequiredMemberAttribute.
    /// Mirrors SubCommandOptionInfo.IsPropertyRequired / SubCommandArgumentInfo.IsPropertyRequired.
    /// </summary>
    private static bool IsPropertyRequired(PropertyInfo property)
    {
        // Check for RequiredMemberAttribute which is added by the compiler when using the required keyword
#if NET7_0_OR_GREATER
        var hasRequiredMemberAttribute = property.IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: false);
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
    private static (Type? CandidateType, string[]? CandidateNames) GetEnumCandidate(Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (!targetType.IsEnum)
        {
            return (null, null);
        }

        return (targetType, [.. Enum.GetNames(targetType)]);
    }

    /// <summary>
    /// Gets all properties including inherited ones from base classes, base-first.
    /// Mirrors SubCommandOptionInfo.GetAllProperties / SubCommandArgumentInfo.GetAllProperties.
    /// </summary>
    private static List<PropertyInfo> GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
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
}
