using Domain.Authorization.Interfaces;

namespace Application.UnitTests.Authorization.Fakes;

internal sealed class InMemoryAuthorizationUnitOfWork : IAuthorizationUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
