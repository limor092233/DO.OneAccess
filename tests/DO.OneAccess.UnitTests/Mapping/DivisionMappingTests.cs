using DO.OneAccess.Application.Common.Mappings;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Domain.Entities;
using Mapster;
using Xunit;

namespace DO.OneAccess.UnitTests.Mapping;

public class DivisionMappingTests
{
    [Fact]
    public void Map_DivisionToDivisionDto_MapsAllExpectedFieldsCorrectly()
    {
        // Arrange
        var createdAt = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
        var division = new Division
        {
            DivisionId = 42,
            Code = "ENG",
            Name = "Engineering",
            Description = "Engineering Department",
            IsActive = true,
            CreatedAt = createdAt
        };

        // Act
        var dto = division.Adapt<DivisionDto>(MappingConfig.Config);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(42, dto.DivisionId);
        Assert.Equal("ENG", dto.Code);
        Assert.Equal("Engineering", dto.Name);
        Assert.Equal("Engineering Department", dto.Description);
        Assert.True(dto.IsActive);
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    [Fact]
    public void Map_DivisionToDivisionDto_WithNullDescription_MapsNullGracefully()
    {
        // Arrange
        var division = new Division
        {
            DivisionId = 10,
            Code = "HR",
            Name = "Human Resources",
            Description = null,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = division.Adapt<DivisionDto>(MappingConfig.Config);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(10, dto.DivisionId);
        Assert.Equal("HR", dto.Code);
        Assert.Equal("Human Resources", dto.Name);
        Assert.Null(dto.Description);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public void Map_DivisionToDivisionDto_DoesNotLeakNavigationOrAuditProperties()
    {
        // Arrange
        var createdByUserId = Guid.NewGuid();
        var updatedByUserId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;

        var division = new Division
        {
            DivisionId = 99,
            Code = "FIN",
            Name = "Finance",
            Description = "Finance Department",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId,
            UpdatedBy = updatedByUserId,
            UpdatedAt = updatedAt,
            Sections = new List<Section>
            {
                new Section { SectionId = 1, DivisionId = 99, Code = "PAY", Name = "Payroll" }
            },
            AdministratorScopes = new List<AdministratorScope>
            {
                new AdministratorScope { AdministratorScopeId = 1, DivisionId = 99 }
            }
        };

        // Act
        var dto = division.Adapt<DivisionDto>(MappingConfig.Config);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(99, dto.DivisionId);
        Assert.Equal("FIN", dto.Code);
        Assert.Equal("Finance", dto.Name);
        Assert.Equal("Finance Department", dto.Description);
        Assert.True(dto.IsActive);

        // Verify that DivisionDto contract contains only the 6 expected public properties
        var properties = typeof(DivisionDto).GetProperties();
        Assert.Equal(6, properties.Length);
        Assert.Contains(properties, p => p.Name == nameof(DivisionDto.DivisionId));
        Assert.Contains(properties, p => p.Name == nameof(DivisionDto.Code));
        Assert.Contains(properties, p => p.Name == nameof(DivisionDto.Name));
        Assert.Contains(properties, p => p.Name == nameof(DivisionDto.Description));
        Assert.Contains(properties, p => p.Name == nameof(DivisionDto.IsActive));
        Assert.Contains(properties, p => p.Name == nameof(DivisionDto.CreatedAt));
    }

    [Fact]
    public void Map_DivisionList_MapsCollectionCorrectly()
    {
        // Arrange
        var divisions = new List<Division>
        {
            new Division { DivisionId = 1, Code = "D1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Division { DivisionId = 2, Code = "D2", Name = "Division 2", IsActive = false, CreatedAt = DateTime.UtcNow },
            new Division { DivisionId = 3, Code = "D3", Name = "Division 3", Description = "Desc 3", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var dtos = divisions.Adapt<List<DivisionDto>>(MappingConfig.Config);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("D1", dtos[0].Code);
        Assert.Equal("D2", dtos[1].Code);
        Assert.Equal("D3", dtos[2].Code);
        Assert.Equal("Desc 3", dtos[2].Description);
    }
}
