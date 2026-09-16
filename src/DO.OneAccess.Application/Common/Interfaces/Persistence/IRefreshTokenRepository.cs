using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(RefreshToken token);
    void Update(RefreshToken token);
    Task RevokeAllActiveByUserIdAsync(Guid userId, string revokedByIp, string reason, CancellationToken cancellationToken = default);
}
