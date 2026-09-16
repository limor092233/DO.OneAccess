using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class AdministratorScopeConfiguration : IEntityTypeConfiguration<AdministratorScope>
{
    public void Configure(EntityTypeBuilder<AdministratorScope> builder)
    {
        builder.ToTable("AdministratorScopes");

        builder.HasKey(a => a.AdministratorScopeId);
        builder.Property(a => a.AdministratorScopeId)
            .UseIdentityColumn();

        builder.Property(a => a.AdministratorUserId)
            .IsRequired();

        builder.HasIndex(a => a.AdministratorUserId)
            .IsUnique();

        builder.Property(a => a.DivisionId)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(a => a.CreatedBy);

        builder.Property(a => a.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(a => a.UpdatedBy);

        builder.HasIndex(a => a.DivisionId);

        builder.HasOne(a => a.AdministratorUser)
            .WithOne(u => u.AdministratorScope)
            .HasForeignKey<AdministratorScope>(a => a.AdministratorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Division)
            .WithMany(d => d.AdministratorScopes)
            .HasForeignKey(a => a.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
