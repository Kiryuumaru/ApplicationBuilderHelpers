using Domain.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Extensions;

public static class DbContextExtensions
{
    public static async Task<int> SaveChangesWithExceptionHandlingAsync(
        this DbContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            var entityInfo = ExtractEntityInfoFromException(ex);
            throw new DuplicateEntityException(entityInfo.EntityType, entityInfo.Identifier);
        }
    }

    public static async Task<int> SaveChangesWithExceptionHandlingAsync(
        this DbContext context,
        string entityType,
        string entityIdentifier,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateEntityException(entityType, entityIdentifier);
        }
    }

    public static async Task<int> SaveChangesWithExceptionHandlingAsync(
        this DbContext context,
        Func<DbUpdateException, DuplicateEntityException?> duplicateExceptionFactory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            var customException = duplicateExceptionFactory(ex);
            if (customException is not null)
            {
                throw customException;
            }
            
            var entityInfo = ExtractEntityInfoFromException(ex);
            throw new DuplicateEntityException(entityInfo.EntityType, entityInfo.Identifier);
        }
    }

    public static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        
        if (message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
            return true;
        
        if (message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            return true;
        
        if (message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
            return true;
        
        if (message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            return true;
        
        return false;
    }

    public static string? ExtractConstraintName(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        
        if (message.Contains("UNIQUE constraint failed:", StringComparison.OrdinalIgnoreCase))
        {
            var startIndex = message.IndexOf("UNIQUE constraint failed:", StringComparison.OrdinalIgnoreCase) + 25;
            var endIndex = message.IndexOf('\'', startIndex);
            if (endIndex == -1) endIndex = message.Length;
            return message[startIndex..endIndex].Trim();
        }
        
        return null;
    }

    public static bool IsConstraintViolationForColumn(DbUpdateException ex, string columnName)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains(columnName, StringComparison.OrdinalIgnoreCase);
    }

    private static (string EntityType, string Identifier) ExtractEntityInfoFromException(DbUpdateException ex)
    {
        var constraintInfo = ExtractConstraintName(ex);
        
        if (constraintInfo is not null)
        {
            // Try to extract table and column from "TableName.ColumnName" format
            var parts = constraintInfo.Split('.');
            if (parts.Length >= 2)
            {
                return (parts[0], parts[1]);
            }
            return ("Entity", constraintInfo);
        }
        
        return ("Entity", "unknown");
    }
}
