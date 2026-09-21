using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Represents a command line option that corresponds to CommandOptionAttribute.
/// Supports option inheritance in subcommand hierarchies.
/// </summary>
internal class SubCommandOptionInfo
{
    /// <summary>
    /// The property this option is bound to
    /// </summary>
    public PropertyInfo Property { get; set; } = null!;

    /// <summary>
    /// The type of the property
    /// </summary>
    public Type PropertyType { get; set; } = null!;

    /// <summary>
    /// Short option name (e.g., 'l' for -l)
    /// </summary>
    public char? ShortName { get; set; }

    /// <summary>
    /// Long option name (e.g., "log-level" for --log-level)
    /// </summary>
    public string? LongName { get; set; }

    /// <summary>
    /// Description of the option
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this option is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Environment variable to fall back to if option is not provided
    /// </summary>
    public string? EnvironmentVariable { get; set; }

    /// <summary>
    /// Valid values for this option (for validation)
    /// </summary>
    public object[]? ValidValues { get; set; }

    /// <summary>
    /// Whether validation should be case sensitive
    /// </summary>
    public bool IsCaseSensitive { get; set; }

    /// <summary>
    /// Whether this option value is a secret (redacted in help and errors)
    /// </summary>
    public bool IsSecret { get; set; }

    /// <summary>
    /// Whether this option is global (available to all subcommands)
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Whether this option is inherited by child commands
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// Whether this is a boolean flag option
    /// </summary>
    public bool IsFlag => PropertyType == typeof(bool) || PropertyType == typeof(bool?);

    /// <summary>
    /// Whether this option accepts multiple values (collection type)
    /// </summary>
    public bool IsCollection => CollectionShape.IsCollection(PropertyType);

    /// <summary>
    /// The element type if this is a collection option
    /// </summary>
    public Type? ElementType => CollectionShape.TryGetElementType(PropertyType, out var elementType) ? elementType : null;

    /// <summary>
    /// The command this option belongs to
    /// </summary>
    public SubCommandInfo? OwnerCommand { get; set; }

    /// <summary>
    /// Explicit bind-target scope for this option copy: the command whose
    /// <see cref="SubCommandInfo.Options"/> list holds this node. For a
    /// definition-site node this equals <see cref="OwnerCommand"/>; for a
    /// global copy <see cref="OwnerCommand"/> stays at the definition site
    /// while this points at the scope holding the copy. Step 1 only: recorded
    /// at every wiring point, read only by help default-value lookup.
    /// </summary>
    public SubCommandInfo? BindTarget { get; set; }

    /// <summary>
    /// Creates a SubCommandOptionInfo from a property and its CommandOptionAttribute
    /// </summary>
    public static SubCommandOptionInfo FromProperty(PropertyInfo property, CommandOptionAttribute attribute, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        // Check if the property has the C# required keyword (auto-detection)
        var isRequiredByKeyword = CommandDescriptorReflection.IsPropertyRequired(property);
        
        var optionInfo = new SubCommandOptionInfo
        {
            Property = property,
            PropertyType = property.PropertyType,
            ShortName = attribute.ShortTerm,
            LongName = attribute.Term ?? property.Name.ToLowerInvariant(),
            Description = attribute.Description,
            // Required if explicitly set in attribute OR if property has required keyword
            IsRequired = attribute.Required || isRequiredByKeyword,
            EnvironmentVariable = attribute.EnvironmentVariable,
            IsCaseSensitive = attribute.CaseSensitive,
            IsSecret = attribute.Secret,
            OwnerCommand = ownerCommand,
            BindTarget = ownerCommand
        };

        // Explicit FromAmong wins; else frozen/live enum names unless suppressed.
        optionInfo.ValidValues = ResolveEnumValues(property.PropertyType, attribute.FromAmong, typeParserCollection);

        // Determine if this option should be inherited by checking if it comes from a base class
        optionInfo.ApplyInheritanceScope(property.DeclaringType, ownerCommand);

        return optionInfo;
    }

    /// <summary>
    /// Creates a per-run copy from a cached descriptor with parser-derived
    /// <paramref name="resolvedValidValues"/>. Descriptor primitives are copied
    /// onto a fresh node; inheritance uses the same declaringType-vs-targetType
    /// check as <see cref="FromProperty"/>, applied to the per-run copy only.
    /// </summary>
    public static SubCommandOptionInfo FromDescriptor(CommandOptionDescriptor descriptor, SubCommandInfo? ownerCommand, object[]? resolvedValidValues)
    {
        var optionInfo = new SubCommandOptionInfo
        {
            Property = descriptor.Property,
            PropertyType = descriptor.PropertyType,
            ShortName = descriptor.ShortName,
            LongName = descriptor.LongName,
            Description = descriptor.Description,
            // Required if explicitly set in attribute OR if property has required keyword
            IsRequired = descriptor.Required || descriptor.IsRequiredByKeyword,
            EnvironmentVariable = descriptor.EnvironmentVariable,
            ValidValues = resolvedValidValues,
            IsCaseSensitive = descriptor.IsCaseSensitive,
            IsSecret = descriptor.IsSecret,
            OwnerCommand = ownerCommand,
            BindTarget = ownerCommand
        };

        // Determine if this option should be inherited by checking if it comes from a base class
        optionInfo.ApplyInheritanceScope(descriptor.DeclaringType, ownerCommand);

        return optionInfo;
    }

    /// <summary>
    /// Single enum predicate (live overload): delegates to the shared
    /// <see cref="EnumValidValues"/> predicate (behavior-neutral).
    /// </summary>
    private static object[]? ResolveEnumValues(Type propertyType, object[]? fromAmong, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(propertyType, fromAmong, typeParserCollection);
    }

    /// <summary>
    /// Single enum predicate (frozen overload): delegates to the shared
    /// <see cref="EnumValidValues"/> predicate (behavior-neutral).
    /// </summary>
    private static object[]? ResolveEnumValues(Type? enumCandidateType, string[]? enumCandidateNames, object[]? fromAmong, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(enumCandidateType, enumCandidateNames, fromAmong, typeParserCollection);
    }

    /// <summary>
    /// Resolves per-run valid values for a cached descriptor: delegates to
    /// the shared <see cref="EnumValidValues"/> predicate (behavior-neutral).
    /// </summary>
    internal static object[]? ResolveValidValues(CommandOptionDescriptor descriptor, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(descriptor.EnumCandidateType, descriptor.EnumCandidateNames, descriptor.FromAmong, typeParserCollection);
    }

    /// <summary>
    /// Gets the type name for display
    /// </summary>
    public string GetTypeName()
    {
        var targetType = IsCollection ? ElementType! : PropertyType;

        return HelpTypeDisplay.GetPlaceholderToken(targetType);
    }

    /// <summary>
    /// Per-kind core: single attribute-read loop for options (base-first,
    /// no sort) shared by the <c>FromCommandType</c>/<c>FromDeclaredType</c>
    /// shims. Inheritance keeps the declaringType-vs-targetType check.
    /// </summary>
    private static List<SubCommandOptionInfo> FromProperties(IEnumerable<PropertyInfo> properties, SubCommandInfo? ownerCommand, ICommandTypeParserCollection? typeParserCollection)
    {
        var options = new List<SubCommandOptionInfo>();

        foreach (var property in properties)
        {
            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>();
            if (optionAttr != null)
            {
                var optionInfo = FromProperty(property, optionAttr, ownerCommand, typeParserCollection);
                options.Add(optionInfo);
            }
        }

        return options;
    }

    /// <summary>
    /// Creates a list of SubCommandOptionInfo objects from a command type.
    /// Shim over the per-kind core (full walk).
    /// </summary>
    [Obsolete("Use CommandReflectionCache for cached descriptors or the per-run FromDescriptor path instead. This member will be removed in a future major version.")]
    public static List<SubCommandOptionInfo> FromCommandType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        return FromProperties(CommandReflectionCache.Walk(commandType), ownerCommand, typeParserCollection);
    }

    /// <summary>
    /// Creates a list of SubCommandOptionInfo objects from properties declared directly in the specified type
    /// (excludes inherited properties to avoid conflicts).
    /// Shim over the per-kind core (declared-only walk).
    /// </summary>
    [Obsolete("Use CommandReflectionCache for cached descriptors or the per-run FromDescriptor path instead. This member will be removed in a future major version.")]
    public static List<SubCommandOptionInfo> FromDeclaredType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        return FromProperties(CommandReflectionCache.WalkDeclaredOnly(commandType), ownerCommand, typeParserCollection);
    }

    /// <summary>
    /// Applies the declaringType-vs-targetType inheritance-scope decision shared by
    /// <see cref="FromProperty"/> and <see cref="FromDescriptor"/>.
    /// </summary>
    private void ApplyInheritanceScope(Type? declaringType, SubCommandInfo? ownerCommand)
    {
        var targetType = ownerCommand?.Command?.GetType();

        // If we have a concrete command instance, check if the property comes from a base class
        if (targetType != null && declaringType != targetType && declaringType != null && declaringType.IsAssignableFrom(targetType))
        {
            IsInherited = true;
            DetermineInheritanceScope();
        }
        // For abstract command processing (when we don't have a concrete command instance),
        // we'll rely on the global option detection logic to determine inheritance patterns
        else if (targetType == null && declaringType != null)
        {
            // This handles cases where we're processing abstract command hierarchies
            // The inheritance will be determined later by the global option detection logic
            IsInherited = false;
        }
    }

    /// <summary>
    /// Determines whether this option should be inherited by child commands
    /// </summary>
    private void DetermineInheritanceScope()
    {
        // For options that come from base classes, they should be inherited but not automatically global
        // Let the global option detection logic determine what's truly global based on actual usage patterns
        IsGlobal = false;  // Don't automatically promote to global based on hardcoded names
        IsInherited = true; // But do mark as inherited since this method is only called for base class options
    }

    /// <summary>
    /// Validates the option value against constraints (only required field validation now).
    /// ValidValues validation is applied inside TypeConversion.Convert (convert-then-compare).
    /// </summary>
    public void ValidateValue(object? value)
    {
        if (IsRequired && value == null)
        {
            throw new CommandException($"Required option '--{LongName ?? ShortName?.ToString()}' is missing", 2, CommandErrorKind.MissingRequired);
        }

        // Note: ValidValues validation is applied inside TypeConversion.Convert
        // (convert-then-compare) before type conversion completes, to keep error messages consistent.
    }

    /// <summary>
    /// Gets the option name for display purposes
    /// </summary>
    public string GetDisplayName()
    {
        if (ShortName.HasValue && !string.IsNullOrEmpty(LongName))
            return $"-{ShortName}, --{LongName}";
        else if (ShortName.HasValue)
            return $"-{ShortName}";
        else if (!string.IsNullOrEmpty(LongName))
            return $"--{LongName}";
        else
            return Property.Name;
    }

    /// <summary>
    /// Gets the option signature for help text
    /// </summary>
    public string GetSignature()
    {
        var name = GetDisplayName();

        var placeholder = HelpTypeDisplay.GetParameterPlaceholder(this);
        if (string.IsNullOrEmpty(placeholder))
            return name;

        return $"{name} {placeholder}";
    }

    /// <summary>
    /// Resolves a <c>--no-&lt;name&gt;=value</c> base name against the caller's
    /// <see cref="SubCommandInfo.AllOptions"/> scope: Ordinal long-name match
    /// across every option kind (flag, valued, collection). The returned node
    /// is the scope's own copy, so <see cref="IsSecret"/> is preserved.
    /// Returns null when the base is unknown or empty.
    /// </summary>
    internal static SubCommandOptionInfo? FindNoValueBase(IEnumerable<SubCommandOptionInfo> allOptions, string baseName)
    {
        if (string.IsNullOrEmpty(baseName))
            return null;

        return allOptions.FirstOrDefault(o =>
            o.LongName != null && string.Equals(o.LongName, baseName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if this option matches the given argument
    /// </summary>
    public bool MatchesArgument(string argument)
    {
        // Long option format: --option or --option=value (Ordinal kind matching)
        if (LongName != null && (argument == $"--{LongName}" || argument.StartsWith($"--{LongName}=", StringComparison.Ordinal)))
            return true;

        // Negated flag format: --no-<name> or --no-<name>=value, flags only.
        // Bare binds false; =-form is rejected in ExtractValue.
        if (IsFlag && LongName != null && (argument == $"--no-{LongName}" || argument.StartsWith($"--no-{LongName}=", StringComparison.Ordinal)))
            return true;

        // Short option format: -o or -o=value or -ovalue (compact)
        if (ShortName.HasValue)
        {
            if (argument == $"-{ShortName}" || argument.StartsWith($"-{ShortName}=", StringComparison.Ordinal))
                return true;

            // Compact format for non-boolean options: -ovalue
            if (!IsFlag && argument.StartsWith($"-{ShortName}", StringComparison.Ordinal) && argument.Length > 2)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the value from a command line argument
    /// </summary>
    public string? ExtractValue(string argument, string? nextArgument = null)
    {
        // Handle --option=value format (Ordinal kind matching)
        if (LongName != null && argument.StartsWith($"--{LongName}=", StringComparison.Ordinal))
        {
            var literal = argument[$"--{LongName}=".Length..];
            if (IsFlag)
                ValidateFlagLiteral(literal);
            return literal;
        }

        // Handle -o=value format
        if (ShortName.HasValue && argument.StartsWith($"-{ShortName}=", StringComparison.Ordinal))
        {
            var literal = argument[$"-{ShortName}=".Length..];
            if (IsFlag)
                ValidateFlagLiteral(literal);
            return literal;
        }

        // Handle --no-<name> negation for boolean flags: bare binds false, =-form is rejected
        if (IsFlag && LongName != null && (argument == $"--no-{LongName}" || argument.StartsWith($"--no-{LongName}=", StringComparison.Ordinal)))
        {
            if (argument.StartsWith($"--no-{LongName}=", StringComparison.Ordinal))
            {
                var rejected = argument[$"--no-{LongName}=".Length..];
                throw new CommandException(SecretRedaction.NoValueAcceptedMessage($"--no-{LongName}", rejected, IsSecret), 2, CommandErrorKind.InvalidValue);
            }

            return "false";
        }

        // Handle compact format -ovalue
        if (ShortName.HasValue && !IsFlag && argument.StartsWith($"-{ShortName}", StringComparison.Ordinal) && argument.Length > 2)
        {
            return argument[2..];
        }

        // Handle --option value or -o value format
        if ((LongName != null && argument == $"--{LongName}") ||
            (ShortName.HasValue && argument == $"-{ShortName}"))
        {
            if (IsFlag)
            {
                return "true"; // Flag without value means true; never consume next token
            }
            else
            {
                return nextArgument; // Use next argument as value
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a string represents a boolean value
    /// </summary>
    private static bool IsBooleanValue(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("off", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("0", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates a flag =-form literal; invalid throws a value error naming the option
    /// </summary>
    private void ValidateFlagLiteral(string literal)
    {
        if (!IsBooleanValue(literal))
            throw new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, $"--{LongName ?? ShortName?.ToString()}", IsSecret), 2, CommandErrorKind.InvalidValue);
    }

    /// <summary>
    /// Returns a string representation of the option
    /// </summary>
    public override string ToString()
    {
        return GetDisplayName();
    }
}
