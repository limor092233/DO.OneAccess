using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class SystemConfiguration : IEntityTypeConfiguration<Domain.Entities.System>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.System> builder)
    {
        builder.ToTable("Systems");

        builder.HasKey(s => s.SystemId);

        builder.Property(s => s.SystemCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.SystemCode)
            .IsUnique();

        builder.Property(s => s.SystemName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.BaseUrl)
            .HasMaxLength(500);

        builder.Property(s => s.IconUrl)
            .HasMaxLength(500);

        builder.Property(s => s.DefaultAccess)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(s => s.CreatedBy);

        builder.Property(s => s.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(s => s.UpdatedBy);

        builder.HasIndex(s => s.IsActive);

        builder.HasMany(s => s.SystemSettings)
            .WithOne(ss => ss.System)
            .HasForeignKey(ss => ss.SystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.UserSystemAccesses)
            .WithOne(usa => usa.System)
            .HasForeignKey(usa => usa.SystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.AdministratorSystemAccesses)
            .WithOne(asa => asa.System)
            .HasForeignKey(asa => asa.SystemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
