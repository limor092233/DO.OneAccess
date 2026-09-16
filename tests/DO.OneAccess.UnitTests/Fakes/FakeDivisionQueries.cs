namespace DO.OneAccess.UnitTests.Fakes;

using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Application.DTOs.Divisions;

public class FakeDivisionQueries : IDivisionQueries
{
    private readonly List<DivisionDto> _divisions = new();

    public FakeDivisionQueries(IEnumerable<DivisionDto>? seed = null)
    {
        if (seed != null)
        {
            _divisions.AddRange(seed);
        }
    }

    public void AddDivision(DivisionDto dto)
    {
        _divisions.Add(dto);
    }

    public Task<IReadOnlyList<DivisionDto>> GetAllAsync(int? scopedDivisionId = null, CancellationToken cancellationToken = default)
    {
        var result = _divisions.AsEnumerable();
        if (scopedDivisionId.HasValue)
        {
            result = result.Where(d => d.DivisionId == scopedDivisionId.Value);
        }
        return Task.FromResult<IReadOnlyList<DivisionDto>>(result.OrderBy(d => d.Name).ToList());
    }

    public Task<DivisionDto?> GetByIdAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        var result = _divisions.FirstOrDefault(d => d.DivisionId == divisionId);
        return Task.FromResult(result);
    }
}
