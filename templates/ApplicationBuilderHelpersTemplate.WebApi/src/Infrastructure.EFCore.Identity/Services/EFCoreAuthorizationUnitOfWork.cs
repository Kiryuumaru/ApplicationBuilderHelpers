using Domain.Authorization.Interfaces;
using Infrastructure.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Services;

internal sealed class EFCoreAuthorizationUnitOfWork(EFCoreDbContext context) : IAuthorizationUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }
}
