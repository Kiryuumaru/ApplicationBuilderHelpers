using System.Security.Claims;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Presentation.WebApp.Client.Services;

internal class BlazorAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthStateProvider _authStateProvider;

    public BlazorAuthStateProvider(IAuthStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
        _authStateProvider.OnStateChanged += NotifyAuthenticationStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Initialization is guaranteed by RunPreparationAsync before app serves requests
        var state = _authStateProvider.CurrentState;

        if (!state.IsAuthenticated)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, state.UserId.ToString()),
            new(ClaimTypes.Name, state.Username ?? string.Empty),
            new(ClaimTypes.Email, state.Email ?? string.Empty)
        };

        foreach (var role in state.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in state.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(principal));
    }

    private void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
