namespace Application.Identity.Models;

public sealed record UserRegistrationRequest(
    string Username,
    string Password,
    string? ConfirmPassword = null,
    string? Email = null,
    IReadOnlyCollection<string>? PermissionIdentifiers = null,
    IReadOnlyCollection<RoleAssignmentRequest>? RoleAssignments = null,
    bool AutoActivate = true)
{
    /// <summary>
    /// Validates that the password and confirm password match.
    /// </summary>
    /// <returns>True if passwords match or confirm password is not provided.</returns>
    public bool PasswordsMatch()
    {
        if (ConfirmPassword is null) return true;
        return string.Equals(Password, ConfirmPassword, StringComparison.Ordinal);
    }
}
