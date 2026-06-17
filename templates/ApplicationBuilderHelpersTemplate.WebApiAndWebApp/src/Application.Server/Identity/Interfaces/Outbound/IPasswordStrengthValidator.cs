using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Identity.Interfaces.Outbound;

public interface IPasswordStrengthValidator
{
    Task<IReadOnlyCollection<string>> ValidateAsync(User user, string password, CancellationToken cancellationToken);
}
