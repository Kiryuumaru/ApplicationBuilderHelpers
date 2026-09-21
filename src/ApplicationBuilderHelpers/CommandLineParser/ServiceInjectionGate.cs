using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Injects per-command services into <see cref="SubCommandInfo.Command"/>
/// properties marked with the framework service attributes.
/// CLI-bound properties are never touched: any property carrying both a
/// CLI marker (<see cref="CommandOptionAttribute"/> /
/// <see cref="CommandArgumentAttribute"/>) and a service marker is a
/// configuration error. Values resolve from the per-command scope so
/// scoped lifetimes stay isolated to one command run.
/// Moved verbatim from the executor (mechanical split, no behavior change)
/// with a single <see cref="Inject"/> entry point; the two former dual-marked
/// fail-fast checks funnel into one throw helper preserving the exact message.
/// <para>
/// Reuse-only seam: no new attribute types. Both markers bind by
/// attribute simple name so this library gains no new package dependency:
/// <c>FromServicesAttribute</c> (ASP.NET Core, property-targeted and
/// directly usable) and <c>FromKeyedServicesAttribute</c> (already
/// referenced via <c>Microsoft.Extensions.DependencyInjection.Abstractions</c>
/// for the <c>Key</c> read and the keyed resolution call).
/// </para>
/// <para>
/// Framework limitation: the upstream <c>FromKeyedServicesAttribute</c>
/// declares <c>AttributeTargets.Parameter</c> only, so the C# compiler
/// rejects direct property use (CS0592). The keyed path below still
/// resolves any property attribute named <c>FromKeyedServicesAttribute</c>
/// that exposes a <c>Key</c> property (same-named shim, emitted metadata,
/// or a future framework retargeting to properties).
/// </para>
/// </summary>
internal static class ServiceInjectionGate
{
    private sealed record ServiceInjectionTarget(PropertyInfo Property, object? Key, bool Keyed);

    private static readonly ConcurrentDictionary<Type, ServiceInjectionTarget[]> InjectionPlanCache = new();
    private static readonly object InjectionPlanSyncRoot = new();

    private static ServiceInjectionTarget[] GetInjectionPlan([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
    {
        if (InjectionPlanCache.TryGetValue(commandType, out var cached))
        {
            return cached;
        }

        lock (InjectionPlanSyncRoot)
        {
            if (InjectionPlanCache.TryGetValue(commandType, out cached))
            {
                return cached;
            }

            var targets = new List<ServiceInjectionTarget>();
            foreach (var property in CommandReflectionCache.GetAllProperties(commandType))
            {
                var attributes = property.GetCustomAttributes(inherit: true);
                bool hasFromServices = attributes.Any(a => a.GetType().Name == "FromServicesAttribute");
                var fromKeyed = attributes.FirstOrDefault(a => a.GetType().Name == "FromKeyedServicesAttribute");
                if (!hasFromServices && fromKeyed is null)
                {
                    continue;
                }

                if (property.IsDefined(typeof(CommandOptionAttribute), inherit: true)
                    || property.IsDefined(typeof(CommandArgumentAttribute), inherit: true))
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

            var plan = targets.ToArray();
            InjectionPlanCache[commandType] = plan;
            return plan;
        }
    }

    internal static void Inject(SubCommandInfo commandInfo, IServiceProvider scopedProvider)
    {
        var command = commandInfo.Command!;
        var commandType = command.GetType();
        var cliBoundNames = commandInfo.AllOptions.Select(o => o.Property.Name).Concat(commandInfo.AllArguments.Select(a => a.Property.Name)).ToHashSet(StringComparer.Ordinal);

        foreach (var target in GetInjectionPlan(commandType))
        {
            var property = target.Property;
            if (cliBoundNames.Contains(property.Name))
            {
                ThrowForDualMarkedProperty(commandType, property);
            }

            object? value = target.Keyed
                ? scopedProvider.GetRequiredKeyedService(property.PropertyType, target.Key)
                : scopedProvider.GetRequiredService(property.PropertyType);

            property.SetValue(command, value);
        }
    }

    /// <summary>
    /// Single fail-fast point for the disjoint-sets invariant: a property is
    /// either CLI-bound or service-injected, never both. Preserves the exact
    /// historical message from both former check sites.
    /// </summary>
    private static void ThrowForDualMarkedProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType,
        PropertyInfo property)
    {
        throw new InvalidOperationException(
            $"Property '{commandType.FullName}.{property.Name}' is marked with both a command-line attribute and a service attribute. A property is either CLI-bound or service-injected, never both.");
    }

    /// <summary>
    /// Reads the <c>Key</c> of a same-named <c>FromKeyedServicesAttribute</c>
    /// shim from attribute metadata (constructor argument or named argument),
    /// without reflecting over the shim type itself (trim-safe).
    /// </summary>
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
