using System.IdentityModel.Tokens.Jwt;
using Domain.Identity.Enums;
using Application.Identity.Interfaces.Inbound;
using Infrastructure.Identity.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Infrastructure.Identity.ConfigureOptions;

internal sealed class ConfigureJwtBearerOptions(IServiceProvider serviceProvider) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options)
    {
        Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        options.Events ??= new JwtBearerEvents();

        var originalOnMessageReceived = options.Events.OnMessageReceived;
        options.Events.OnMessageReceived = async context =>
        {
            if (originalOnMessageReceived is not null)
            {
                await originalOnMessageReceived(context);
            }

            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
        };

        // Unified token validation on token validated
        var originalOnTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            if (originalOnTokenValidated is not null)
            {
                await originalOnTokenValidated(context);
            }

            // Skip if authentication already failed
            if (context.Result?.Failure is not null)
            {
                return;
            }

            // Extract typ header from security token
            // Handle both JwtSecurityToken (older handler) and JsonWebToken (newer handler)
            var typHeader = context.SecurityToken switch
            {
                JsonWebToken jsonWebToken => jsonWebToken.Typ,
                JwtSecurityToken jwtSecurityToken => jwtSecurityToken.Header?.Typ,
                _ => null
            };

            // Determine allowed token types based on endpoint
            var path = context.HttpContext.Request.Path;
            var allowedTypes = GetAllowedTokenTypes(path);

            // Use unified validation service
            using var scope = context.HttpContext.RequestServices.CreateScope();
            var tokenValidation = scope.ServiceProvider.GetRequiredService<ITokenValidationService>();

            var result = await tokenValidation.ValidatePostSignatureAsync(
                context.Principal!,
                typHeader,
                allowedTypes,
                context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                context.Fail(result.Error ?? "Token validation failed");
            }
        };

        // Create a scope to resolve scoped services (e.g., CredentialsService used by the JWT config factory)
        using var scope = serviceProvider.CreateScope();
        options.TokenValidationParameters = scope.ServiceProvider
            .GetRequiredService<IJwtTokenService>()
            .GetTokenValidationParameters(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        // Disable claim type mapping - keep JWT claim types as-is ("sub" stays "sub", not mapped to ClaimTypes.NameIdentifier)
        options.MapInboundClaims = false;
    }

    private static TokenType[] GetAllowedTokenTypes(PathString path)
    {
        // Refresh endpoint only accepts refresh tokens
        if (path.StartsWithSegments("/api/v1/auth/refresh"))
        {
            return [TokenType.Refresh];
        }

        // All endpoints accept access tokens, API keys, and refresh tokens.
        // Refresh tokens pass authentication but will fail authorization checks
        // (403 Forbidden) because they lack the necessary permission claims.
        return [TokenType.Access, TokenType.ApiKey, TokenType.Refresh];
    }
}
