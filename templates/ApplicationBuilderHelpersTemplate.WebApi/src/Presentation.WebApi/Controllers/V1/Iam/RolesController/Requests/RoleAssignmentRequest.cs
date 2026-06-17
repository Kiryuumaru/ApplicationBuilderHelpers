using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApi.Controllers.V1.Iam.RolesController.Requests;

/// <summary>
/// Request to assign a role to a user.
/// </summary>
public sealed class RoleAssignmentRequest
{
    /// <summary>
    /// Gets or sets the user ID to assign the role to.
    /// </summary>
    [Required]
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the role code to assign.
    /// </summary>
    [Required]
    public required string RoleCode { get; init; }

    /// <summary>
    /// Gets or sets optional parameter values for the role assignment.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? ParameterValues { get; init; }
}
