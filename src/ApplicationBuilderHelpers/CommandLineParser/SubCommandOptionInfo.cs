using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal class SubCommandOptionInfo
{
    public PropertyInfo Property { get; set; } = null!;

    public Type PropertyType { get; set; } = null!;

    public char? ShortName { get; set; }

    public string? LongName { get; set; }

    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    public string? EnvironmentVariable { get; set; }

    public object[]? ValidValues { get; set; }

    public bool IsCaseSensitive { get; set; }

    /// <summary>Secret value: never echoed in help, errors, or completion.</summary>
    public bool IsSecret { get; set; }

    public bool IsGlobal { get; set; }

    public bool IsInherited { get; set; }

    public bool IsFlag => PropertyType == typeof(bool) || PropertyType == typeof(bool?);

    public bool SupportsNegation => IsFlag && LongName != null;

    // SupportsNegation stays true for help/version; the parser misuse gate rejects their
    // negated forms before matching, so display paths exclude them here instead.
    internal bool ShouldShowNegation => SupportsNegation
        && !string.Equals(LongName, "help", StringComparison.Ordinal)
        && !string.Equals(LongName, "version", StringComparison.Ordinal);

    internal string? NegatedLongName => LongName == null ? null : $"--no-{LongName}";

    internal string? NegatedBareName => LongName == null ? null : $"no-{LongName}";

    public bool IsCollection => CollectionShape.IsCollection(PropertyType);

    public Type? ElementType => CollectionShape.TryGetElementType(PropertyType, out var elementType) ? elementType : null;

    public SubCommandInfo? OwnerCommand { get; set; }

    public SubCommandInfo? BindTarget { get; set; }

    public static SubCommandOptionInfo FromProperty(PropertyInfo property, CommandOptionAttribute attribute, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        var isRequiredByKeyword = CommandDescriptorReflection.IsPropertyRequired(property);
        var longName = attribute.Term ?? property.Name.ToLowerInvariant();

        var optionInfo = new SubCommandOptionInfo
        {
            Property = property,
            PropertyType = property.PropertyType,
            ShortName = CommandDescriptorReflection.ResolveShortName(longName, attribute.ShortTerm),
            LongName = longName,
            Description = attribute.Description,
            IsRequired = attribute.Required || isRequiredByKeyword,
            EnvironmentVariable = attribute.EnvironmentVariable,
            IsCaseSensitive = attribute.CaseSensitive,
            IsSecret = attribute.Secret,
            OwnerCommand = ownerCommand,
            BindTarget = ownerCommand
        };

        optionInfo.ValidValues = ResolveEnumValues(property.PropertyType, attribute.FromAmong, typeParserCollection);

        optionInfo.ApplyInheritanceScope(property.DeclaringType, ownerCommand);

        return optionInfo;
    }

    public static SubCommandOptionInfo FromDescriptor(CommandOptionDescriptor descriptor, SubCommandInfo? ownerCommand, object[]? resolvedValidValues)
    {
        var optionInfo = new SubCommandOptionInfo
        {
            Property = descriptor.Property,
            PropertyType = descriptor.PropertyType,
            ShortName = descriptor.ShortName,
            LongName = descriptor.LongName,
            Description = descriptor.Description,
            IsRequired = descriptor.Required || descriptor.IsRequiredByKeyword,
            EnvironmentVariable = descriptor.EnvironmentVariable,
            ValidValues = resolvedValidValues,
            IsCaseSensitive = descriptor.IsCaseSensitive,
            IsSecret = descriptor.IsSecret,
            OwnerCommand = ownerCommand,
            BindTarget = ownerCommand
        };

        optionInfo.ApplyInheritanceScope(descriptor.DeclaringType, ownerCommand);

        return optionInfo;
    }

    private static object[]? ResolveEnumValues(Type propertyType, object[]? fromAmong, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(propertyType, fromAmong, typeParserCollection);
    }

    private static object[]? ResolveEnumValues(Type? enumCandidateType, string[]? enumCandidateNames, object[]? fromAmong, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(enumCandidateType, enumCandidateNames, fromAmong, typeParserCollection);
    }

    internal static object[]? ResolveValidValues(CommandOptionDescriptor descriptor, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(descriptor.EnumCandidateType, descriptor.EnumCandidateNames, descriptor.FromAmong, typeParserCollection);
    }

    public string GetTypeName()
    {
        var targetType = IsCollection ? ElementType! : PropertyType;

        return HelpTypeDisplay.GetPlaceholderToken(targetType);
    }

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

    [Obsolete("Use CommandReflectionCache for cached descriptors or the per-run FromDescriptor path instead.")]
    public static List<SubCommandOptionInfo> FromCommandType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        return FromProperties(CommandReflectionCache.Walk(commandType), ownerCommand, typeParserCollection);
    }

    [Obsolete("Use CommandReflectionCache for cached descriptors or the per-run FromDescriptor path instead.")]
    public static List<SubCommandOptionInfo> FromDeclaredType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        return FromProperties(CommandReflectionCache.WalkDeclaredOnly(commandType), ownerCommand, typeParserCollection);
    }

    private void ApplyInheritanceScope(Type? declaringType, SubCommandInfo? ownerCommand)
    {
        var targetType = ownerCommand?.Command?.GetType();

        if (targetType != null && declaringType != targetType && declaringType != null && declaringType.IsAssignableFrom(targetType))
        {
            IsInherited = true;
            DetermineInheritanceScope();
        }
        else if (targetType == null && declaringType != null)
        {
            IsInherited = false;
        }
    }

    private void DetermineInheritanceScope()
    {
        IsGlobal = false;
        IsInherited = true;
    }

    public void ValidateValue(object? value)
    {
        if (IsRequired && value == null)
        {
            throw new CommandException($"Required option '--{LongName ?? ShortName?.ToString()}' is missing", 2, CommandErrorKind.MissingRequired);
        }

    }

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

    public string GetSignature()
    {
        var name = GetDisplayName();
        if (ShouldShowNegation && NegatedLongName != null)
            name += $", {NegatedLongName}";

        var placeholder = HelpTypeDisplay.GetParameterPlaceholder(this);
        if (string.IsNullOrEmpty(placeholder))
            return name;

        return $"{name} {placeholder}";
    }

    internal static SubCommandOptionInfo? FindNoValueBase(IEnumerable<SubCommandOptionInfo> allOptions, string baseName)
    {
        if (string.IsNullOrEmpty(baseName))
            return null;

        return allOptions.FirstOrDefault(o =>
            o.LongName != null && string.Equals(o.LongName, baseName, StringComparison.Ordinal));
    }

    public bool MatchesArgument(string argument)
    {
        if (LongName != null && (argument == $"--{LongName}" || argument.StartsWith($"--{LongName}=", StringComparison.Ordinal)))
            return true;

        if (SupportsNegation && NegatedLongName != null && (argument == NegatedLongName || argument.StartsWith($"{NegatedLongName}=", StringComparison.Ordinal)))
            return true;

        if (ShortName.HasValue)
        {
            if (argument == $"-{ShortName}" || argument.StartsWith($"-{ShortName}=", StringComparison.Ordinal))
                return true;

            if (!IsFlag && argument.StartsWith($"-{ShortName}", StringComparison.Ordinal) && argument.Length > 2)
                return true;
        }

        return false;
    }

    public string? ExtractValue(string argument, string? nextArgument = null)
    {
        if (LongName != null && argument.StartsWith($"--{LongName}=", StringComparison.Ordinal))
        {
            var literal = argument[$"--{LongName}=".Length..];
            if (IsFlag)
                throw new CommandException(SecretRedaction.NoValueAcceptedMessage($"--{LongName}", literal, IsSecret, isFlag: true, positiveLongName: LongName, isNegated: false), 2, CommandErrorKind.InvalidValue);
            ThrowOnEmptyEqualsLiteral(literal);
            return literal;
        }

        if (ShortName.HasValue && argument.StartsWith($"-{ShortName}=", StringComparison.Ordinal))
        {
            var literal = argument[$"-{ShortName}=".Length..];
            if (IsFlag)
            {
                var display = LongName != null ? $"--{LongName}" : $"-{ShortName}";
                throw new CommandException(SecretRedaction.NoValueAcceptedMessage(display, literal, IsSecret, isFlag: true, positiveLongName: LongName, isNegated: false), 2, CommandErrorKind.InvalidValue);
            }
            ThrowOnEmptyEqualsLiteral(literal);
            return literal;
        }

        if (SupportsNegation && NegatedLongName != null && (argument == NegatedLongName || argument.StartsWith($"{NegatedLongName}=", StringComparison.Ordinal)))
        {
            if (argument.StartsWith($"{NegatedLongName}=", StringComparison.Ordinal))
            {
                var rejected = argument[$"{NegatedLongName}=".Length..];
                throw new CommandException(SecretRedaction.NoValueAcceptedMessage(NegatedLongName, rejected, IsSecret, isFlag: true, positiveLongName: LongName, isNegated: true), 2, CommandErrorKind.InvalidValue);
            }

            return "false";
        }

        if (ShortName.HasValue && !IsFlag && argument.StartsWith($"-{ShortName}", StringComparison.Ordinal) && argument.Length > 2)
        {
            return argument[2..];
        }

        if ((LongName != null && argument == $"--{LongName}") ||
            (ShortName.HasValue && argument == $"-{ShortName}"))
        {
            if (IsFlag)
            {
                return "true";
            }
            else
            {
                return nextArgument;
            }
        }

        return null;
    }

    internal static bool IsBooleanValue(string value)
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

    private void ThrowOnEmptyEqualsLiteral(string literal)
    {
        if (literal.Length != 0 || PropertyType == typeof(string))
            return;
        if (IsCollection && ElementType == typeof(string))
            return;
        Type effectiveType = IsCollection && ElementType is not null ? ElementType : PropertyType;
        effectiveType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
        string? reason = effectiveType.IsEnum ? ParserErrorHints.EnumAllowedValues(effectiveType) : ParserErrorHints.HintFor(effectiveType);
        throw ConversionErrors.InvalidValue(literal, $"option '--{LongName ?? ShortName?.ToString()}'", reason, IsSecret, effectiveType.Name);
    }

    public override string ToString()
    {
        return GetDisplayName();
    }
}
