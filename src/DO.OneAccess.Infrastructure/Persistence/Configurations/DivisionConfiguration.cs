using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class DivisionConfiguration : IEntityTypeConfiguration<Division>
{
    public void Configure(EntityTypeBuilder<Division> builder)
    {
        builder.ToTable("Divisions");

        builder.HasKey(d => d.DivisionId);
        builder.Property(d => d.DivisionId)
            .UseIdentityColumn();

        builder.Property(d => d.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.Property(d => d.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(d => d.CreatedBy);

        builder.Property(d => d.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(d => d.UpdatedBy);

        builder.HasMany(d => d.Sections)
            .WithOne(s => s.Division)
            .HasForeignKey(s => s.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.AdministratorScopes)
            .WithOne(a => a.Division)
            .HasForeignKey(a => a.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
