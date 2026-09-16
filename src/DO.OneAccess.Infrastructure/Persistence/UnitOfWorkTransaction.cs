using Microsoft.EntityFrameworkCore.Storage;
using DO.OneAccess.Application.Common.Interfaces.Persistence;

namespace DO.OneAccess.Infrastructure.Persistence;

public sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;
    private bool _committed;
    private bool _disposed;

    public UnitOfWorkTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (!_committed)
            {
                await _transaction.RollbackAsync();
            }
        }
        catch
        {
            // Suppress exception during rollback on dispose to prevent masking primary errors
        }
        finally
        {
            await _transaction.DisposeAsync();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (!_committed)
            {
                _transaction.Rollback();
            }
        }
        catch
        {
            // Suppress exception during rollback on dispose to prevent masking primary errors
        }
        finally
        {
            _transaction.Dispose();
            _disposed = true;
        }
    }
}
