using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

public class BaseService<TDbContext>(TDbContext db, IErrorLogger errorLogger) : IBaseService
    where TDbContext : DbContext
{
    protected readonly TDbContext _db = db;

    public async Task<Result<T>> ExecuteAsync<T>(
        string                 operationName,
        Func<Task<Result<T>>>  operation,
        CancellationToken      ct,
        bool                   useTransaction = false)
    {
        IDbContextTransaction? tx = null;
        try
        {
            if (useTransaction)
                tx = await _db.Database.BeginTransactionAsync(ct);

            var result = await operation();

            if (tx is not null)
            {
                if (result.IsSuccess) await tx.CommitAsync(ct);
                else                  await tx.RollbackAsync(ct);
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(tx);
            return Result<T>.Failure("Operation was cancelled.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(tx);
            return Result<T>.Conflict($"Concurrency conflict in '{operationName}'.");
        }
        catch (ValidationException ex)
        {
            await RollbackAsync(tx);
            return Result<T>.Validation([ex.Message]);
        }
        catch (Exception ex)
        {
            await RollbackAsync(tx);
            await errorLogger.LogAsync(operationName, ex, ct);
            return Result<T>.Failure($"Unexpected error in '{operationName}'.");
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    private static async Task RollbackAsync(IDbContextTransaction? tx)
    {
        if (tx is null) return;
        try { await tx.RollbackAsync(); } catch { /* best-effort */ }
    }
}
