using Microsoft.AspNetCore.Mvc;
using Presentation.WebApp.Server.Controllers.V1.Auth.MeController;
using Presentation.WebApp.Server.Controllers.V1.Auth.Shared.Responses;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.Shared;

/// <summary>
/// Shared helper methods for Auth controllers.
/// </summary>
public static class AuthHelpers
{
    /// <summary>
    /// Creates a 201 Created response pointing to the GET /auth/me endpoint.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="response">The authentication response.</param>
    /// <returns>A CreatedAtAction result.</returns>
    public static CreatedAtActionResult CreatedAtMe(this ControllerBase controller, AuthResponse response)
    {
        _ = controller.RouteData.Values.TryGetValue("v", out var apiVersion);

        return controller.CreatedAtAction(
            actionName: nameof(AuthMeController.GetMe),
            controllerName: "AuthMe",
            routeValues: apiVersion is null ? null : new { v = apiVersion },
            value: response);
    }

    /// <summary>
    /// Returns either a 201 Created (pointing to GET /auth/me) or 200 OK based on whether this is a new user.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="isNewUser">Whether this is a newly registered user.</param>
    /// <param name="response">The authentication response.</param>
    /// <returns>CreatedAtAction for new users, Ok for existing users.</returns>
    public static IActionResult CreatedAtMeOrOk(this ControllerBase controller, bool isNewUser, AuthResponse response)
    {
        return isNewUser ? controller.CreatedAtMe(response) : controller.Ok(response);
    }
}
