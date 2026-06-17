namespace Application.Server.Identity.Interfaces.Outbound;

/// <summary>
/// Verifies password credentials against stored hashes.
/// </summary>
public interface IPasswordVerifier
{
    /// <summary>
    /// Verifies that the provided password matches the stored password hash.
    /// </summary>
    /// <param name="passwordHash">The stored password hash.</param>
    /// <param name="providedPassword">The password to verify.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    bool Verify(string passwordHash, string providedPassword);
}
