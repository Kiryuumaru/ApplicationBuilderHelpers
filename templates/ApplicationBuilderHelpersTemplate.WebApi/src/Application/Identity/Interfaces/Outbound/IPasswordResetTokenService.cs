using Domain.Identity.Models;
using Domain.Identity.Entities;

namespace Application.Identity.Interfaces.Outbound;

/// <summary>
/// Service for generating and validating password reset tokens.
/// Implemented by Infrastructure layer.
/// </summary>
public interface IPasswordResetTokenService
{
    Task<string> GenerateResetTokenAsync(User user, CancellationToken cancellationToken);

    Task<bool> ResetPasswordWithTokenAsync(User user, string token, string newPassword, CancellationToken cancellationToken);
}
