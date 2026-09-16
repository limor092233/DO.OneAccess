using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.RefreshTokenId);
        builder.Property(rt => rt.RefreshTokenId)
            .UseIdentityColumn();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .HasColumnType("datetime2");

        builder.Property(rt => rt.ReplacedByTokenId);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(50);

        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.TokenHash);
        builder.HasIndex(rt => new { rt.RevokedAt, rt.ExpiresAt });

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rt => rt.ReplacedByToken)
            .WithMany(rt => rt.ReplacedTokens)
            .HasForeignKey(rt => rt.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
