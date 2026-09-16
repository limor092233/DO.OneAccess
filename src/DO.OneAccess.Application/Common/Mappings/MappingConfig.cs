namespace DO.OneAccess.Application.Common.Mappings;

using Mapster;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Domain.Entities;

public static class MappingConfig
{
    public static readonly TypeAdapterConfig Config;

    static MappingConfig()
    {
        Config = new TypeAdapterConfig();

        Config.NewConfig<Division, DivisionDto>()
            .Map(dest => dest.DivisionId, src => src.DivisionId)
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .IgnoreNonMapped(true);
    }
}
