using DO.OneAccess.Application.DTOs.Sections;

namespace DO.OneAccess.Client.Services.Api;

public interface ISectionApiClient
{
    Task<IReadOnlyList<SectionDto>> GetSectionsAsync(SectionQueryDto query, CancellationToken ct = default);
    Task<SectionDto> GetSectionByIdAsync(int sectionId, CancellationToken ct = default);
    Task<SectionDto> CreateSectionAsync(CreateSectionDto dto, CancellationToken ct = default);
    Task<SectionDto> UpdateSectionAsync(int sectionId, UpdateSectionDto dto, CancellationToken ct = default);
    Task DeactivateSectionAsync(int sectionId, CancellationToken ct = default);
}
