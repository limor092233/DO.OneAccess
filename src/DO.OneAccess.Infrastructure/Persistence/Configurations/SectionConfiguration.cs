using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Sections");

        builder.HasKey(s => s.SectionId);
        builder.Property(s => s.SectionId)
            .UseIdentityColumn();

        builder.Property(s => s.DivisionId)
            .IsRequired();

        builder.Property(s => s.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

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

        builder.HasIndex(s => new { s.DivisionId, s.Code })
            .IsUnique();

        builder.HasOne(s => s.Division)
            .WithMany(d => d.Sections)
            .HasForeignKey(s => s.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Users)
            .WithOne(u => u.Section)
            .HasForeignKey(u => u.SectionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
