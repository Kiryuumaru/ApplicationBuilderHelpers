using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Interfaces.Outbound;
using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Authorization.Services;

internal sealed class UserRoleResolver(IRoleRepository roleRepository) : IUserRoleResolver
{
    public async Task<IReadOnlyCollection<UserRoleResolution>> ResolveRolesAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();
        if (user.RoleAssignments.Count == 0)
        {
            return Array.Empty<UserRoleResolution>();
        }

        var distinctIds = user.RoleAssignments
            .Select(static assignment => assignment.RoleId)
            .Distinct()
            .ToArray();

        var roles = await roleRepository.GetByIdsAsync(distinctIds, cancellationToken).ConfigureAwait(false);
        var roleIndex = roles.ToDictionary(static role => role.Id);
        var resolutions = new List<UserRoleResolution>(user.RoleAssignments.Count);
        foreach (var assignment in user.RoleAssignments)
        {
            if (!roleIndex.TryGetValue(assignment.RoleId, out var role))
            {
                continue;
            }

            resolutions.Add(new UserRoleResolution(role, assignment.ParameterValues));
        }

        return resolutions;
    }
}
