using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Injects per-command services into properties marked with framework service attributes.</summary>
internal static class ServiceInjectionGate
{
    private sealed record ServiceInjectionTarget(PropertyInfo Property, object? Key, bool Keyed);

    private static readonly TypePlanCache<ServiceInjectionTarget[]> InjectionPlanCache = new();

    /// <summary>Returns the cached injection plan for the command type.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Method-group BuildInjectionPlan is statically referenced, never reflection-invoked by name; the All-annotated type flows through the annotated PlanFactory delegate.")]
    private static ServiceInjectionTarget[] GetInjectionPlan([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        return InjectionPlanCache.GetOrAdd(commandType, BuildInjectionPlan);
    }

    /// <summary>Builds the injection plan; dual-marked properties throw once at plan build.</summary>
    private static ServiceInjectionTarget[] BuildInjectionPlan([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        var walk = CommandReflectionCache.Walk(commandType);

        var cliBoundNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in walk)
        {
            if (CommandReflectionCache.IsCliBound(property))
            {
                cliBoundNames.Add(property.Name);
            }
        }

        var targets = new List<ServiceInjectionTarget>();
        foreach (var property in walk)
        {
            var attributes = property.GetCustomAttributes(inherit: true);
            bool hasFromServices = attributes.Any(a => a.GetType().Name == "FromServicesAttribute");
            var fromKeyed = attributes.FirstOrDefault(a => a.GetType().Name == "FromKeyedServicesAttribute");
            if (!hasFromServices && fromKeyed is null)
            {
                continue;
            }

            if (CommandReflectionCache.IsCliBound(property) || cliBoundNames.Contains(property.Name))
            {
                ThrowForDualMarkedProperty(commandType, property);
            }

            if (!property.CanWrite || property.SetMethod is null || property.SetMethod.IsStatic)
            {
                throw new InvalidOperationException(
                    $"Property '{commandType.FullName}.{property.Name}' is marked for service injection but has no writable instance setter.");
            }

            object? key = null;
            bool keyed = fromKeyed is not null;
            if (keyed)
            {
                key = fromKeyed is FromKeyedServicesAttribute typed ? typed.Key : ReadKeyedServiceKey(property, commandType);
            }

            targets.Add(new ServiceInjectionTarget(property, key, keyed));
        }

        return [.. targets];
    }

    /// <summary>Injects cached-plan services into the per-command scope instance.</summary>
    internal static void Inject(SubCommandInfo commandInfo, IServiceProvider scopedProvider)
    {
        var command = commandInfo.Command!;
        var commandType = command.GetType();

        foreach (var target in GetInjectionPlan(commandType))
        {
            var property = target.Property;

            object? value = target.Keyed
                ? scopedProvider.GetRequiredKeyedService(property.PropertyType, target.Key)
                : scopedProvider.GetRequiredService(property.PropertyType);

            property.SetValue(command, value);
        }
    }

    /// <summary>Throws for a property marked both CLI-bound and service-injected.</summary>
    private static void ThrowForDualMarkedProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType,
        PropertyInfo property)
    {
        throw new InvalidOperationException(
            $"Property '{commandType.FullName}.{property.Name}' is marked with both a command-line attribute and a service attribute. A property is either CLI-bound or service-injected, never both.");
    }

    /// <summary>Reads the key of a same-named FromKeyedServices attribute from metadata.</summary>
    private static object? ReadKeyedServiceKey(PropertyInfo property, Type commandType)
    {
        foreach (var data in property.GetCustomAttributesData())
        {
            if (!string.Equals(data.AttributeType.Name, "FromKeyedServicesAttribute", StringComparison.Ordinal))
            {
                continue;
            }

            if (data.ConstructorArguments.Count > 0)
            {
                return data.ConstructorArguments[0].Value;
            }

            foreach (var named in data.NamedArguments)
            {
                if (string.Equals(named.MemberName, "Key", StringComparison.Ordinal))
                {
                    return named.TypedValue.Value;
                }
            }

            throw new InvalidOperationException(
                $"Property '{commandType.FullName}.{property.Name}' is marked with 'FromKeyedServicesAttribute' but carries no key.");
        }

        throw new InvalidOperationException(
            $"Property '{commandType.FullName}.{property.Name}' is marked with 'FromKeyedServicesAttribute' but carries no key.");
    }
}
