using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Extensions;

public static class IApplicationDependencyCollectionExtensions
{
    public static TApplicationDependencyCollection AddApplication<TApplicationDependencyCollection>(this TApplicationDependencyCollection applicationDependencyCollection, IApplicationDependency applicationDependency)
        where TApplicationDependencyCollection : IApplicationDependencyCollection
    {
        ArgumentNullException.ThrowIfNull(applicationDependencyCollection);
        ArgumentNullException.ThrowIfNull(applicationDependency);
        applicationDependencyCollection.ApplicationDependencies.Add(applicationDependency);
        return applicationDependencyCollection;
    }

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
