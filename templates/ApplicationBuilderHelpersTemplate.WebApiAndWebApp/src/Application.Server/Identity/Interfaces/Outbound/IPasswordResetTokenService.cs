using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Identity.Interfaces.Outbound;

public interface IPasswordResetTokenService
{
    Task<string> GenerateResetTokenAsync(User user, CancellationToken cancellationToken);

    Task<bool> ResetPasswordWithTokenAsync(User user, string token, string newPassword, CancellationToken cancellationToken);
}
