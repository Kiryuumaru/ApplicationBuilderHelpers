namespace Domain.Identity.Interfaces;

/// <summary>
/// Verifies user passwords against stored hashes.
/// </summary>
public interface IPasswordVerifier
{
    bool Verify(string passwordHash, string providedPassword);
}
