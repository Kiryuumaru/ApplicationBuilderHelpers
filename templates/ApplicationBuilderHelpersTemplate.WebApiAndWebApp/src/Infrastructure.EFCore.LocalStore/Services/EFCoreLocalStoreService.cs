using Application.LocalStore.Interfaces.Outbound;
using Infrastructure.EFCore.Extensions;
using Infrastructure.EFCore.LocalStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.LocalStore.Services;

internal sealed class EFCoreLocalStoreService(
    IDbContextFactory<EFCoreDbContext> dbContextFactory,
    IDatabaseInitializationState databaseInitializationState) : ILocalStoreService
{
    private readonly IDbContextFactory<EFCoreDbContext> _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    private readonly IDatabaseInitializationState _databaseInitializationState = databaseInitializationState ?? throw new ArgumentNullException(nameof(databaseInitializationState));
    
    private EFCoreDbContext? _dbContext;
    private bool _hasTransaction;

    public async Task Open(CancellationToken cancellationToken)
    {
        if (_dbContext != null)
        {
            return;
        }

        // Wait for database tables to be initialized before opening context
        await _databaseInitializationState.WaitForInitializationAsync(cancellationToken);
        
        _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _hasTransaction = true;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_dbContext?.Database.CurrentTransaction != null)
        {
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
            _hasTransaction = false;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_dbContext?.Database.CurrentTransaction != null)
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            _hasTransaction = false;
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_dbContext == null)
        {
            await Open(cancellationToken);
        }
    }

    public async Task<string?> Get(string group, string id, CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        // Safe: EnsureConnectionAsync guarantees _dbContext is initialized
        var entry = await _dbContext!.Set<LocalStoreEntry>()
            .FirstOrDefaultAsync(e => e.Group == group && e.Id == id, cancellationToken);
        
        return entry?.Data;
    }

    public async Task<string[]> GetIds(string group, CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        // Safe: EnsureConnectionAsync guarantees _dbContext is initialized
        return await _dbContext!.Set<LocalStoreEntry>()
            .Where(e => e.Group == group)
            .Select(e => e.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task Set(string group, string id, string? data, CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        // Safe: EnsureConnectionAsync guarantees _dbContext is initialized
        var existing = await _dbContext!.Set<LocalStoreEntry>()
            .FirstOrDefaultAsync(e => e.Group == group && e.Id == id, cancellationToken);

        if (data == null)
        {
            if (existing != null)
            {
                _dbContext.Set<LocalStoreEntry>().Remove(existing);
                await _dbContext.SaveChangesWithExceptionHandlingAsync(cancellationToken);
            }
        }
        else
        {
            if (existing == null)
            {
                _dbContext.Set<LocalStoreEntry>().Add(new LocalStoreEntry
                {
                    Group = group,
                    Id = id,
                    Data = data
                });
            }
            else
            {
                existing.Data = data;
            }
            await _dbContext.SaveChangesWithExceptionHandlingAsync(cancellationToken);
        }
    }

    public async Task<bool> Contains(string group, string id, CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        // Safe: EnsureConnectionAsync guarantees _dbContext is initialized
        return await _dbContext!.Set<LocalStoreEntry>()
            .AnyAsync(e => e.Group == group && e.Id == id, cancellationToken);
    }

    public void Dispose()
    {
        try
        {
            if (_hasTransaction && _dbContext != null)
            {
                var transaction = _dbContext.Database.CurrentTransaction;
                if (transaction != null)
                {
                    // Auto-commit on dispose - same as the original SqliteLocalStoreService behavior
                    _dbContext.Database.CommitTransaction();
                    _hasTransaction = false;
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Context was already disposed, nothing to do
        }
        finally
        {
            _dbContext?.Dispose();
            _dbContext = null;
        }
    }
}
