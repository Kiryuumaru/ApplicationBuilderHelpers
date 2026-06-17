#if DEBUG
namespace Presentation.WebApi.Controllers.V1.DevTools.AdminController.Responses;

/// <summary>
/// Response model for create-admin (DEBUG only).
/// </summary>
public sealed class CreateAdminResponse
{
    /// <summary>
    /// The created user's ID.
    /// </summary>
    public required Guid UserId { get; init; }
}
#endif
