using Domain.Identity.Interfaces;
using Infrastructure.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Identity.Services;

internal sealed class EFCoreIdentityUnitOfWork(EFCoreDbContext context) : IIdentityUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesWithExceptionHandlingAsync(cancellationToken);
    }
}
