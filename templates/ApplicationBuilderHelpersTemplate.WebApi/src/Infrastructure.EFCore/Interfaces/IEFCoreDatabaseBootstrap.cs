namespace Infrastructure.EFCore.Interfaces;

internal interface IEFCoreDatabaseBootstrap
{
    Task SetupAsync(CancellationToken cancellationToken = default);
}
