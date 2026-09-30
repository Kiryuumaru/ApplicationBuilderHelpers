using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal class SubCommandArgumentInfo
{
    public PropertyInfo Property { get; set; } = null!;

    public Type PropertyType { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int Position { get; set; }

    public bool IsRequired { get; set; }

    public object[]? ValidValues { get; set; }

    public bool IsCaseSensitive { get; set; }

    /// <summary>Secret value: never echoed in errors or completion.</summary>
    public bool IsSecret { get; set; }

    public bool IsGlobal { get; set; }

    public bool IsInherited { get; set; }

    public bool IsCollection => CollectionShape.IsCollection(PropertyType);

    public Type? ElementType => CollectionShape.TryGetElementType(PropertyType, out var elementType) ? elementType : null;

    public SubCommandInfo? OwnerCommand { get; set; }

    public string DisplayName => Name ?? Property.Name.ToLowerInvariant();

    public static SubCommandArgumentInfo FromProperty(PropertyInfo property, CommandArgumentAttribute attribute, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        var isRequiredByKeyword = CommandDescriptorReflection.IsPropertyRequired(property);

        var argumentInfo = new SubCommandArgumentInfo
        {
            Property = property,
            PropertyType = property.PropertyType,
            Name = attribute.Name ?? property.Name.ToLowerInvariant(),
            Description = attribute.Description,
            Position = attribute.Position,
            IsRequired = attribute.Required || isRequiredByKeyword,
            ValidValues = EnumValidValues.Resolve(property.PropertyType, attribute.FromAmong, typeParserCollection),
            IsCaseSensitive = attribute.CaseSensitive,
            IsSecret = attribute.Secret,
            OwnerCommand = ownerCommand
        };

        argumentInfo.DetermineInheritanceScope();

        return argumentInfo;
    }

    public static SubCommandArgumentInfo FromDescriptor(CommandArgumentDescriptor descriptor, SubCommandInfo? ownerCommand, object[]? resolvedValidValues)
    {
        var argumentInfo = new SubCommandArgumentInfo
        {
            Property = descriptor.Property,
            PropertyType = descriptor.PropertyType,
            Name = descriptor.Name,
            Description = descriptor.Description,
            Position = descriptor.Position,
            IsRequired = descriptor.Required || descriptor.IsRequiredByKeyword,
            ValidValues = resolvedValidValues,
            IsCaseSensitive = descriptor.IsCaseSensitive,
            IsSecret = descriptor.IsSecret,
            OwnerCommand = ownerCommand
        };

        argumentInfo.DetermineInheritanceScope();

        return argumentInfo;
    }

    public string GetTypeName()
    {
        var targetType = IsCollection ? ElementType! : PropertyType;

        return HelpTypeDisplay.GetPlaceholderToken(targetType);
    }

    internal static object[]? ResolveValidValues(CommandArgumentDescriptor descriptor, ICommandTypeParserCollection? typeParserCollection)
    {
        return EnumValidValues.Resolve(descriptor.EnumCandidateType, descriptor.EnumCandidateNames, descriptor.FromAmong, typeParserCollection);
    }

    private static List<SubCommandArgumentInfo> FromProperties(IEnumerable<PropertyInfo> properties, SubCommandInfo? ownerCommand, ICommandTypeParserCollection? typeParserCollection)
    {
        var arguments = new List<SubCommandArgumentInfo>();

        foreach (var property in properties)
        {
            var argumentAttr = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (argumentAttr != null)
            {
                var argumentInfo = FromProperty(property, argumentAttr, ownerCommand, typeParserCollection);
                arguments.Add(argumentInfo);
            }
        }

        return [.. arguments.OrderBy(a => a.Position)];
    }

    [Obsolete("Use CommandReflectionCache for cached descriptors or the per-run FromDescriptor path instead.")]
    public static List<SubCommandArgumentInfo> FromCommandType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        return FromProperties(CommandReflectionCache.Walk(commandType), ownerCommand, typeParserCollection);
    }

    [Obsolete("Use CommandReflectionCache for cached descriptors or the per-run FromDescriptor path instead.")]
    public static List<SubCommandArgumentInfo> FromDeclaredType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType, SubCommandInfo? ownerCommand = null, ICommandTypeParserCollection? typeParserCollection = null)
    {
        return FromProperties(CommandReflectionCache.WalkDeclaredOnly(commandType), ownerCommand, typeParserCollection);
    }

    private void DetermineInheritanceScope()
    {
        if (!IsGlobal && !IsInherited)
        {
            IsGlobal = false;
            IsInherited = false;
        }
    }

    public string GetSignature()
    {
        var name = DisplayName.ToUpperInvariant();
        
        if (IsCollection)
            name += "...";
            
        if (IsRequired)
            return $"<{name}>";
        else
            return $"[{name}]";
    }

    public bool CanAcceptValueAtPosition(int position)
    {
        if (IsCollection)
        {
            return position >= Position;
        }
        else
        {
            return position == Position;
        }
    }

    public override string ToString()
    {
        return $"{DisplayName} (position {Position})";
    }
}
