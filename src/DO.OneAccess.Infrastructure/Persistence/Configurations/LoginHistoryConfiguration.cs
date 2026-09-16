using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistories");

        builder.HasKey(lh => lh.LoginHistoryId);
        builder.Property(lh => lh.LoginHistoryId)
            .UseIdentityColumn();

        builder.Property(lh => lh.UserId);

        builder.Property(lh => lh.UsernameAttempted)
            .HasMaxLength(100);

        builder.Property(lh => lh.Success)
            .IsRequired();

        builder.Property(lh => lh.IpAddress)
            .HasMaxLength(50);

        builder.Property(lh => lh.UserAgent)
            .HasMaxLength(500);

        builder.Property(lh => lh.FailureReason)
            .HasMaxLength(200);

        builder.Property(lh => lh.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(lh => lh.UserId);
        builder.HasIndex(lh => lh.CreatedAt);
        builder.HasIndex(lh => lh.UsernameAttempted);

        builder.HasOne(lh => lh.User)
            .WithMany(u => u.LoginHistories)
            .HasForeignKey(lh => lh.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
