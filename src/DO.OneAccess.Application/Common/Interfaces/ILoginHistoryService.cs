using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// Login history querying. Restricted to System Administrators.
/// </summary>
public interface ILoginHistoryService
{
    Task<PagedResult<LoginHistoryDto>> GetLoginHistoriesAsync(LoginHistoryQueryDto query, CancellationToken cancellationToken = default);
}
