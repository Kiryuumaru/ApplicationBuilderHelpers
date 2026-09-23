using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Represents a collection of application dependencies configured on the application builder.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IApplicationDependencyCollection
{
    internal List<IApplicationDependency> ApplicationDependencies { get; }
}
