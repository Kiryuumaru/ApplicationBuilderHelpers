namespace Infrastructure.EFCore.Interfaces;

public interface IEFCoreDatabaseBootstrap
{
    Task SetupAsync(CancellationToken cancellationToken = default);
}
