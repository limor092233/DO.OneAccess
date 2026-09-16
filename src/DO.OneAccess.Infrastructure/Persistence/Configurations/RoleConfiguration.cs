using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.RoleId);
        builder.Property(r => r.RoleId)
            .ValueGeneratedNever();

        builder.Property(r => r.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Code)
            .IsUnique();

        builder.Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(200);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasData(
            new Role
            {
                RoleId = 1,
                Code = "SYSTEM_ADMINISTRATOR",
                Name = "System Administrator",
                Description = "Global authority over the entire DO.OneAccess platform",
                IsActive = true
            },
            new Role
            {
                RoleId = 2,
                Code = "ADMINISTRATOR",
                Name = "Administrator",
                Description = "Scoped authority over one Division and its Sections",
                IsActive = true
            },
            new Role
            {
                RoleId = 3,
                Code = "USER",
                Name = "User",
                Description = "Regular employee account; accesses permitted systems",
                IsActive = true
            }
        );
    }
}
