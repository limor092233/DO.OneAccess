using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class AdministratorSystemAccessConfiguration : IEntityTypeConfiguration<AdministratorSystemAccess>
{
    public void Configure(EntityTypeBuilder<AdministratorSystemAccess> builder)
    {
        builder.ToTable("AdministratorSystemAccess");

        builder.HasKey(asa => asa.AdministratorSystemAccessId);
        builder.Property(asa => asa.AdministratorSystemAccessId)
            .UseIdentityColumn();

        builder.Property(asa => asa.AdministratorUserId)
            .IsRequired();

        builder.Property(asa => asa.SystemId)
            .IsRequired();

        builder.Property(asa => asa.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(asa => asa.CreatedBy);

        builder.Property(asa => asa.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(asa => asa.UpdatedBy);

        builder.HasIndex(asa => new { asa.AdministratorUserId, asa.SystemId })
            .IsUnique();

        builder.HasIndex(asa => asa.SystemId);

        builder.HasOne(asa => asa.AdministratorUser)
            .WithMany(u => u.AdministratorSystemAccesses)
            .HasForeignKey(asa => asa.AdministratorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(asa => asa.System)
            .WithMany(s => s.AdministratorSystemAccesses)
            .HasForeignKey(asa => asa.SystemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
