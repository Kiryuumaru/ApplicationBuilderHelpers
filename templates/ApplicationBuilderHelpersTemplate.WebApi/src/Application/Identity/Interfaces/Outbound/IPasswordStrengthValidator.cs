using Domain.Identity.Models;
using Domain.Identity.Entities;

namespace Application.Identity.Interfaces.Outbound;

/// <summary>
/// Service for validating password strength.
/// Implemented by Infrastructure layer.
/// </summary>
public interface IPasswordStrengthValidator
{
    Task<IReadOnlyCollection<string>> ValidateAsync(User user, string password, CancellationToken cancellationToken);
}
