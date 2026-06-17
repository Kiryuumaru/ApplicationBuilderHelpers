using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Authorization.Interfaces.Inbound;

/// <summary>
/// Resolves user role assignments to their full role definitions with parameter values.
/// </summary>
/// <remarks>
/// Use the returned <see cref="UserRoleResolution"/> collection with:
/// <list type="bullet">
///   <item><c>resolutions.Select(r => r.Code)</c> - Get role codes</item>
///   <item><c>resolutions.Select(r => r.ToFormattedClaim())</c> - Get formatted role claims with parameters</item>
/// </list>
/// </remarks>
public interface IUserRoleResolver
{
    /// <summary>
    /// Resolves all role assignments for a user to their full role definitions with parameter values.
    /// </summary>
    Task<IReadOnlyCollection<UserRoleResolution>> ResolveRolesAsync(User user, CancellationToken cancellationToken);
}
