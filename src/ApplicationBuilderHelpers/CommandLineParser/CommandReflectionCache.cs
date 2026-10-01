using ApplicationBuilderHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

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

internal sealed record CommandTypeDescriptor(
    Type CommandType,
    CommandOptionDescriptor[] Options,
    CommandArgumentDescriptor[] Arguments);

internal sealed class CommandReflectionCache
{
    private readonly TypePlanCache<CommandTypeDescriptor> _plans = new();

    internal int BuildCount => _plans.BuildCount;

    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Method-group Build is statically referenced, never reflection-invoked by name; the All-annotated type flows through the annotated PlanFactory delegate.")]
    internal CommandTypeDescriptor GetOrAdd([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        return _plans.GetOrAdd(commandType, Build);
    }

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
        var longName = attribute.Term ?? property.Name.ToLowerInvariant();

        return new CommandOptionDescriptor(
            property,
            property.PropertyType,
            property.DeclaringType,
            CommandDescriptorReflection.ResolveShortName(longName, attribute.ShortTerm),
            longName,
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

    internal static bool IsCliBound(PropertyInfo property)
    {
        return property.IsDefined(typeof(CommandOptionAttribute), inherit: true)
            || property.IsDefined(typeof(CommandArgumentAttribute), inherit: true);
    }

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

    internal static List<PropertyInfo> WalkDeclaredOnly([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
    {
        return [.. type.GetProperties(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance)];
    }
}
