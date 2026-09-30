using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view dependency list backing the host pipeline; entries run in registration order.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IApplicationDependencyCollection
{
    /// <summary>
    /// Gets the registered dependencies in registration order.
    /// </summary>
    internal List<IApplicationDependency> ApplicationDependencies { get; }
}
