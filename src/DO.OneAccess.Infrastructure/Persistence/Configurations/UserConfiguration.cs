using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.EmployeeNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(u => u.EmployeeNumber)
            .IsUnique();

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.MiddleName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Position)
            .HasMaxLength(100);

        builder.Property(u => u.SectionId)
            .IsRequired(false);

        builder.Property(u => u.RoleId)
            .IsRequired();

        builder.Property(u => u.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("datetime2");

        builder.Property(u => u.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(u => u.CreatedBy);

        builder.Property(u => u.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(u => u.UpdatedBy);

        builder.HasIndex(u => u.RoleId);
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.SectionId);

        builder.HasOne(u => u.Section)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SectionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.UserSystemAccesses)
            .WithOne(usa => usa.User)
            .HasForeignKey(usa => usa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.AdministratorScope)
            .WithOne(s => s.AdministratorUser)
            .HasForeignKey<AdministratorScope>(s => s.AdministratorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AdministratorSystemAccesses)
            .WithOne(asa => asa.AdministratorUser)
            .HasForeignKey(asa => asa.AdministratorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.LoginHistories)
            .WithOne(lh => lh.User)
            .HasForeignKey(lh => lh.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AuditLogs)
            .WithOne(al => al.User)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
