using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>Dependency registration for <see cref="IApplicationDependencyCollection"/>.</summary>
public static class IApplicationDependencyCollectionExtensions
{
    /// <summary>Appends <paramref name="applicationDependency"/> in call order.</summary>
    /// <typeparam name="TApplicationDependencyCollection">The collection type.</typeparam>
    /// <param name="applicationDependencyCollection">The collection.</param>
    /// <param name="applicationDependency">The dependency instance.</param>
    /// <returns>The same collection instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TApplicationDependencyCollection AddApplication<TApplicationDependencyCollection>(this TApplicationDependencyCollection applicationDependencyCollection, IApplicationDependency applicationDependency)
        where TApplicationDependencyCollection : IApplicationDependencyCollection
    {
        ArgumentNullException.ThrowIfNull(applicationDependencyCollection);
        ArgumentNullException.ThrowIfNull(applicationDependency);
        applicationDependencyCollection.ApplicationDependencies.Add(applicationDependency);
        return applicationDependencyCollection;
    }

    /// <summary>Constructs the dependency type and appends it in call order.</summary>
    /// <typeparam name="TApplicationDependency">The dependency type.</typeparam>
    /// <typeparam name="TApplicationDependencyCollection">The collection type.</typeparam>
    /// <param name="applicationDependencyCollection">The collection.</param>
    /// <returns>The same collection instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the collection or created instance is null.</exception>
    /// <exception cref="MissingMemberException">Thrown when <typeparamref name="TApplicationDependency"/> lacks a public parameterless constructor.</exception>
    public static TApplicationDependencyCollection AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency, TApplicationDependencyCollection>(this TApplicationDependencyCollection applicationDependencyCollection)
        where TApplicationDependency : IApplicationDependency
        where TApplicationDependencyCollection : IApplicationDependencyCollection
    {
        ArgumentNullException.ThrowIfNull(applicationDependencyCollection);
        var applicationDependency = Activator.CreateInstance<TApplicationDependency>();
        ArgumentNullException.ThrowIfNull(applicationDependency);
        applicationDependencyCollection.ApplicationDependencies.Add(applicationDependency);
        return applicationDependencyCollection;
    }
}
