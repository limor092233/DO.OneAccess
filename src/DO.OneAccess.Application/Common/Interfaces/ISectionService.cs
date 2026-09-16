using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Sections;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// Section management. Administrator scope is enforced: only sections within the actor's division.
/// </summary>
public interface ISectionService
{
    Task<IReadOnlyList<SectionDto>> GetSectionsAsync(SectionQueryDto query, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<SectionDto> GetSectionByIdAsync(int sectionId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<SectionDto> CreateSectionAsync(CreateSectionDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<SectionDto> UpdateSectionAsync(int sectionId, UpdateSectionDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeactivateSectionAsync(int sectionId, Guid actorUserId, CancellationToken cancellationToken = default);
}
