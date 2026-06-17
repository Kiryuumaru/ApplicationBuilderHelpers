namespace Domain.Authorization.Constants;

/// <summary>
/// Constants for Role-Based Access Control (RBAC) versioning and claim types.
/// </summary>
public static class RbacConstants
{
    /// <summary>
    /// The current RBAC version. Used to invalidate tokens when RBAC schema changes.
    /// Tokens with a different or missing version are rejected.
    /// </summary>
    public const string CurrentVersion = "2";

    /// <summary>
    /// The claim type for the RBAC version in JWT tokens.
    /// </summary>
    public const string VersionClaimType = "rbac_version";
}
