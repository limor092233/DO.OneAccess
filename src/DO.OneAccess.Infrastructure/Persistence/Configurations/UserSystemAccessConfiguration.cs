using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class UserSystemAccessConfiguration : IEntityTypeConfiguration<UserSystemAccess>
{
    public void Configure(EntityTypeBuilder<UserSystemAccess> builder)
    {
        builder.ToTable("UserSystemAccess");

        builder.HasKey(usa => usa.UserSystemAccessId);
        builder.Property(usa => usa.UserSystemAccessId)
            .UseIdentityColumn();

        builder.Property(usa => usa.UserId)
            .IsRequired();

        builder.Property(usa => usa.SystemId)
            .IsRequired();

        builder.Property(usa => usa.AccessType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(usa => usa.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(usa => usa.CreatedBy);

        builder.Property(usa => usa.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(usa => usa.UpdatedBy);

        builder.HasIndex(usa => new { usa.UserId, usa.SystemId })
            .IsUnique();

        builder.HasIndex(usa => usa.SystemId);

        builder.HasOne(usa => usa.User)
            .WithMany(u => u.UserSystemAccesses)
            .HasForeignKey(usa => usa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(usa => usa.System)
            .WithMany(s => s.UserSystemAccesses)
            .HasForeignKey(usa => usa.SystemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
