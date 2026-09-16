using System.Data;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IUnitOfWorkTransaction : IAsyncDisposable, IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
