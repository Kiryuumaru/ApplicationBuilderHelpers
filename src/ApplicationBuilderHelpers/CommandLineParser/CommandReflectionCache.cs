using ApplicationBuilderHelpers.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    /// Command-type DAM is intentionally properties-only (no PublicFields);
    /// enum member fields are preserved separately at the BuildEnumConverter boundary.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection to discover command options and arguments.")]
    internal CommandTypeDescriptor GetOrAdd([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType)
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
    /// Trim-safe enum parse delegate: name, numeric, and flags-comma forms with
    /// caller-chosen case handling. Built once per enum type by the RUC-isolated
    /// factory below; the hot path invokes this delegate with no reflection.
    /// </summary>
    internal delegate bool TryParseEnumDelegate(string? value, bool ignoreCase, out object? result);

    private static readonly ConcurrentDictionary<Type, TryParseEnumDelegate> s_enumConverters = new();
    private static readonly object s_enumSyncRoot = new();

    /// <summary>
    /// Hot path: cached per-enum-type parse with no reflection. The converter is
    /// built once by <see cref="BuildEnumConverter(Type)"/>; every later call for
    /// the same type is a plain delegate invocation over frozen snapshot state
    /// (the only runtime-type operation is the numeric-path box via
    /// <c>Enum.ToObject</c>, never member lookup).
    /// Returns false (never throws) when the text is not a valid enum value.
    /// Uses a double-checked lock (instead of a GetOrAdd value-factory
    /// delegate) so the trimmer sees the annotated type flow directly into the
    /// RUC factory with no reflection-invoked delegate hop.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Single designed trim boundary: the RUC-annotated BuildEnumConverter snapshots one enum type; the cached delegate performs no reflection.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Enum snapshot hop: BuildEnumConverter is RUC-annotated and preserves the enum type's public fields itself; the unannotated enumType here flows only into that single factory call.")]
    internal static bool TryParseEnum(Type enumType, string? value, bool ignoreCase, out object? result)
    {
        if (s_enumConverters.TryGetValue(enumType, out var cached))
        {
            return cached(value, ignoreCase, out result);
        }

        lock (s_enumSyncRoot)
        {
            if (s_enumConverters.TryGetValue(enumType, out cached))
            {
                return cached(value, ignoreCase, out result);
            }

            var built = BuildEnumConverter(enumType);
            s_enumConverters[enumType] = built;
            return built(value, ignoreCase, out result);
        }
    }

    /// <summary>
    /// Hot path throwing parse mirroring <c>Enum.Parse(Type, string, bool)</c>:
    /// returns the boxed value or throws <see cref="ArgumentException"/> with the
    /// same messages as <c>Enum.Parse</c>, so the existing
    /// catch-to-<c>CommandException</c> mapping is unchanged.
    /// </summary>
    internal static object ParseEnum(Type enumType, string value, bool ignoreCase)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (TryParseEnum(enumType, value, ignoreCase, out var result) && result is not null)
        {
            return result;
        }

        if (value.Trim().Length == 0)
        {
            throw new ArgumentException("Must specify valid information for parsing in the string.", nameof(value));
        }

        throw new ArgumentException($"Requested value '{value}' was not found.", nameof(value));
    }

    /// <summary>
    /// RUC-isolated factory: the single snapshot hop (<c>Enum.GetNames</c> /
    /// <c>Enum.GetValues</c> / underlying-type read) that freezes one enum type
    /// into name/value pairs plus its underlying type code and flags-ness. Runs
    /// once per enum type; never on the hot path. The returned delegate closes
    /// over frozen arrays only and performs no reflection.
    /// Enum members are static public fields, so this factory — not the
    /// properties-only command-type DAM on GetOrAdd/Build — is the trim
    /// boundary that preserves them; RUC keeps the snapshot reachable.
    /// The parameter carries PublicFields so the snapshot's field dependency
    /// is explicit; the single TryParseEnum call site suppresses IL2067 with
    /// justification instead of cascading DAM onto every binder caller.
    /// </summary>
    [RequiresUnreferencedCode("Snapshots enum names and values via reflection; the returned delegate performs no reflection.")]
    private static TryParseEnumDelegate BuildEnumConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType)
    {
        var names = Enum.GetNames(enumType);
#pragma warning disable IL3050 // Single RUC-isolated snapshot hop: boxes one enum type's values once; the cached delegate performs no reflection.
        var boxed = Enum.GetValues(enumType);
#pragma warning restore IL3050
        var frozen = new KeyValuePair<string, object?>[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            frozen[i] = new KeyValuePair<string, object?>(names[i], boxed.GetValue(i));
        }

        var underlyingCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
        var isFlags = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

        return (string? value, bool ignoreCase, out object? result) =>
        {
            result = null;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Mirrors Enum.TryParse trimming: leading/trailing whitespace ignored.
            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // Mirrors Enum.TryParse: comma forms only for flags enums; each
            // segment is trimmed and may be a name or a numeric, OR-ed together.
            if (trimmed.IndexOf(',') >= 0)
            {
                if (!isFlags)
                {
                    return false;
                }

                ulong accumulator = 0;
                foreach (var segment in trimmed.Split(','))
                {
                    if (!TryMatchSingleEnumValue(segment.Trim(), comparison, frozen, underlyingCode, enumType, out var part) || part is null)
                    {
                        return false;
                    }

                    // Signed-safe bits: Convert.ToUInt64 throws OverflowException on
                    // negative signed members; Int64 always fits signed values and
                    // UInt64 always fits unsigned values, with unchecked preserving bits.
                    ulong bits = underlyingCode is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64
                        ? unchecked((ulong)Convert.ToInt64(part))
                        : Convert.ToUInt64(part);
                    accumulator |= bits;
                }

                result = Enum.ToObject(enumType, accumulator);
                return true;
            }

            if (TryMatchSingleEnumValue(trimmed, comparison, frozen, underlyingCode, enumType, out var single) && single is not null)
            {
                result = single;
                return true;
            }

            return false;
        };
    }

    /// <summary>
    /// Matches one enum token: frozen name lookup first (case per caller flag),
    /// then an underlying-type numeric parse (mirrors <c>Enum.TryParse</c>,
    /// which accepts any in-range numeric even when unnamed).
    /// Pure string/integer work; no reflection.
    /// </summary>
    private static bool TryMatchSingleEnumValue(string candidate, StringComparison comparison, KeyValuePair<string, object?>[] frozen, TypeCode underlyingCode, Type enumType, out object? matched)
    {
        for (var i = 0; i < frozen.Length; i++)
        {
            if (string.Equals(frozen[i].Key, candidate, comparison))
            {
                matched = frozen[i].Value;
                return true;
            }
        }

        matched = TryParseEnumNumeric(candidate, underlyingCode, enumType);
        return matched is not null;
    }

    /// <summary>
    /// Parses a numeric token in the enum's underlying type
    /// (<see cref="NumberStyles.Integer"/>, invariant culture, no hex —
    /// mirroring <c>Enum.TryParse</c>) and boxes it via
    /// <c>Enum.ToObject</c>. Returns null when the token is not a number or is
    /// out of range for the underlying type.
    /// </summary>
    private static object? TryParseEnumNumeric(string candidate, TypeCode underlyingCode, Type enumType)
    {
        var styles = NumberStyles.Integer;
        var culture = CultureInfo.InvariantCulture;
        bool ok;
        object raw;

        switch (underlyingCode)
        {
            case TypeCode.SByte:
                sbyte signedByte;
                ok = sbyte.TryParse(candidate, styles, culture, out signedByte);
                raw = signedByte;
                break;
            case TypeCode.Byte:
                byte unsignedByte;
                ok = byte.TryParse(candidate, styles, culture, out unsignedByte);
                raw = unsignedByte;
                break;
            case TypeCode.Int16:
                short int16;
                ok = short.TryParse(candidate, styles, culture, out int16);
                raw = int16;
                break;
            case TypeCode.UInt16:
                ushort uint16;
                ok = ushort.TryParse(candidate, styles, culture, out uint16);
                raw = uint16;
                break;
            case TypeCode.Int32:
                int int32;
                ok = int.TryParse(candidate, styles, culture, out int32);
                raw = int32;
                break;
            case TypeCode.UInt32:
                uint uint32;
                ok = uint.TryParse(candidate, styles, culture, out uint32);
                raw = uint32;
                break;
            case TypeCode.Int64:
                long int64;
                ok = long.TryParse(candidate, styles, culture, out int64);
                raw = int64;
                break;
            case TypeCode.UInt64:
                ulong uint64;
                ok = ulong.TryParse(candidate, styles, culture, out uint64);
                raw = uint64;
                break;
            default:
                return null;
        }

        if (!ok)
        {
            return null;
        }

        return Enum.ToObject(enumType, raw);
    }

    /// <summary>
    /// Trim-boundary array factory for the binder fallback path: constructs an
    /// array of the real element type (never <c>object[]</c>). Throws plain on
    /// failure; the caller wraps with a styled error carrying real value context.
    /// </summary>
    internal static Array CreateTypedArray(Type elementType, int length)
    {
        return Array.CreateInstance(elementType, length);
    }

    /// <summary>
    /// Pure-reflection factory: snapshots options and arguments for the command type.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection to discover command options and arguments.")]
    private static CommandTypeDescriptor Build([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType)
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

    /// <summary>
    /// Snapshot hop for one option property: routes the property type through
    /// <see cref="GetEnumCandidate(Type)"/> (Enum.GetNames boundary) under the
    /// RUC of <see cref="Build(Type)"/>; performs no reflection itself.
    /// </summary>
    [RequiresUnreferencedCode("Snapshots the property's enum candidates via the GetEnumCandidate boundary.")]
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

    /// <summary>
    /// Snapshot hop for one argument property: routes the property type through
    /// <see cref="GetEnumCandidate(Type)"/> (Enum.GetNames boundary) under the
    /// RUC of <see cref="Build(Type)"/>; performs no reflection itself.
    /// </summary>
    [RequiresUnreferencedCode("Snapshots the property's enum candidates via the GetEnumCandidate boundary.")]
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
    /// Enum members are static public fields, so the candidate type depends on
    /// PublicFields preservation; the RUC boundary keeps the snapshot reachable
    /// while the command-type DAM above stays properties-only. The parameter
    /// intentionally carries no DAM annotation: most property types are not
    /// enums, so annotating it would cascade field requirements onto every
    /// command property without adding preservation beyond this RUC boundary.
    /// No type-parser check here: the parser layer decides auto-populate.
    /// </summary>
    [RequiresUnreferencedCode("Snapshots enum names via reflection for the candidate enum type.")]
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
    [RequiresUnreferencedCode("Uses reflection to discover command options and arguments.")]
    private static List<PropertyInfo> GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
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
