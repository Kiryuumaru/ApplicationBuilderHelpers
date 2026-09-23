using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IApplicationDependencyCollection"/> to add application dependencies.
/// </summary>
/// <remarks>
/// Extension methods for adding application dependencies.
/// </remarks>
public static class IApplicationDependencyCollectionExtensions
{
    /// <summary>
    /// Adds an application dependency instance to the collection.
    /// </summary>
    /// <typeparam name="TApplicationDependencyCollection">The type of the application dependency collection that implements <see cref="IApplicationDependencyCollection"/>.</typeparam>
    /// <param name="applicationDependencyCollection">The application dependency collection to add the dependency to.</param>
    /// <param name="applicationDependency">The application dependency instance to add to the collection.</param>
    /// <returns>The same application dependency collection instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="applicationDependencyCollection"/> or <paramref name="applicationDependency"/> is null.</exception>
    public static TApplicationDependencyCollection AddApplication<TApplicationDependencyCollection>(this TApplicationDependencyCollection applicationDependencyCollection, IApplicationDependency applicationDependency)
        where TApplicationDependencyCollection : IApplicationDependencyCollection
    {
        ArgumentNullException.ThrowIfNull(applicationDependencyCollection);
        ArgumentNullException.ThrowIfNull(applicationDependency);
        applicationDependencyCollection.ApplicationDependencies.Add(applicationDependency);
        return applicationDependencyCollection;
    }

    /// <summary>
    /// Adds an application dependency of the specified type to the collection by creating a new instance using the parameterless constructor.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The type of application dependency that implements <see cref="IApplicationDependency"/> and has a public parameterless constructor.</typeparam>
    /// <typeparam name="TApplicationDependencyCollection">The type of the application dependency collection that implements <see cref="IApplicationDependencyCollection"/>.</typeparam>
    /// <param name="applicationDependencyCollection">The application dependency collection to add the dependency to.</param>
    /// <returns>The same application dependency collection instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="applicationDependencyCollection"/> is null or when the created application dependency instance is null.</exception>
    /// <exception cref="MissingMemberException">Thrown when <typeparamref name="TApplicationDependency"/> does not have a public parameterless constructor.</exception>
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
