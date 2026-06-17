using System.Security.Claims;
using Domain.Authorization.Models;

namespace Application.Authorization.Interfaces.Inbound;

/// <summary>
/// Coordinates permission metadata and token issuance.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets the root permission tree defined in <see cref="Domain.Authorization.Constants.Permissions"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A hierarchical collection of permissions rooted at the cloud scope.</returns>
    Task<IReadOnlyCollection<Permission>> GetPermissionTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the flat list of permission identifiers (paths) available for assignment.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The complete set of permission identifiers.</returns>
    Task<IReadOnlyCollection<string>> GetAllPermissionIdentifiersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the provided permission identifiers exist in the canonical permission set.
    /// </summary>
    /// <param name="permissionIdentifiers">Identifiers to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns><c>true</c> when the identifiers are all recognized; otherwise <c>false</c>.</returns>
    Task<bool> ValidatePermissionsAsync(IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a JWT token containing the supplied permission claims.
    /// </summary>
    /// <param name="userId">The unique identifier for the principal.</param>
    /// <param name="username">The display name for the principal (null for anonymous users).</param>
    /// <param name="permissionIdentifiers">Permission identifiers to embed as claims.</param>
    /// <param name="additionalClaims">Additional claims to include in the token beyond permissions.</param>
    /// <param name="expiration">Optional expiration override for the delegated token.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A signed JWT token string.</returns>
    Task<string> GenerateTokenWithPermissionsAsync(
        string userId,
        string? username,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues an API key token containing the supplied permission claims.
    /// </summary>
    /// <param name="apiKeyName">The friendly name for the API key principal.</param>
    /// <param name="permissionIdentifiers">Permission identifiers to embed as claims.</param>
    /// <param name="additionalClaims">Additional claims to include alongside the API key metadata.</param>
    /// <param name="expiration">Optional expiration override for the delegated token.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A signed JWT token string suitable for API key authentication.</returns>
    Task<string> GenerateApiKeyTokenWithPermissionsAsync(
        string apiKeyName,
        IEnumerable<string> permissionIdentifiers,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a JWT token containing scope directives.
    /// This method uses the new directive-based scope system (RBAC v2).
    /// </summary>
    /// <param name="userId">The unique identifier for the principal.</param>
    /// <param name="username">The display name for the principal (null for anonymous users).</param>
    /// <param name="scopeDirectives">Scope directives to embed as claims.</param>
    /// <param name="additionalClaims">Additional claims to include in the token beyond scopes.</param>
    /// <param name="expiration">Optional expiration override for the delegated token.</param>
    /// <param name="tokenType">The type of token to generate (Access, Refresh, or ApiKey).</param>
    /// <param name="tokenId">Optional custom token ID (jti claim). If null, a random GUID is used.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A signed JWT token string.</returns>
    Task<string> GenerateTokenWithScopeAsync(
        string userId,
        string? username,
        IEnumerable<Domain.Authorization.ValueObjects.ScopeDirective> scopeDirectives,
        IEnumerable<Claim>? additionalClaims = null,
        DateTimeOffset? expiration = null,
        Domain.Identity.Enums.TokenType tokenType = Domain.Identity.Enums.TokenType.Access,
        string? tokenId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the canonical <see cref="Permission"/> objects for the provided identifiers.
    /// </summary>
    /// <param name="permissionIdentifiers">Identifiers to resolve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The matching permission objects.</returns>
    Task<IReadOnlyCollection<Permission>> ResolvePermissionsAsync(
        IEnumerable<string> permissionIdentifiers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified principal holds the provided permission.
    /// Resolves role permissions at runtime from the database.
    /// </summary>
    /// <param name="principal">The principal to evaluate.</param>
    /// <param name="permissionIdentifier">The permission identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the principal has the permission; otherwise <c>false</c>.</returns>
    Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the principal holds any of the supplied permissions.
    /// Resolves role permissions at runtime from the database.
    /// </summary>
    /// <param name="principal">The principal to evaluate.</param>
    /// <param name="permissionIdentifiers">The permission identifiers to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when any permission is present; otherwise <c>false</c>.</returns>
    Task<bool> HasAnyPermissionAsync(ClaimsPrincipal principal, IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the principal holds all of the supplied permissions.
    /// Resolves role permissions at runtime from the database.
    /// </summary>
    /// <param name="principal">The principal to evaluate.</param>
    /// <param name="permissionIdentifiers">The permission identifiers to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when all permissions are present; otherwise <c>false</c>.</returns>
    Task<bool> HasAllPermissionsAsync(ClaimsPrincipal principal, IEnumerable<string> permissionIdentifiers, CancellationToken cancellationToken = default);
}
