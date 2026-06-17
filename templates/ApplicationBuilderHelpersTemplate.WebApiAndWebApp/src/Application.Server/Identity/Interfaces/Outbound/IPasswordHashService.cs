using Domain.Identity.Entities;
using Domain.Identity.Models;

namespace Application.Server.Identity.Interfaces.Outbound;

public interface IPasswordHashService
{
    string Hash(User user, string password);
}
