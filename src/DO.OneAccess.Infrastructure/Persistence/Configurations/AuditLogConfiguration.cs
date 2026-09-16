using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.AuditLogId);
        builder.Property(al => al.AuditLogId)
            .UseIdentityColumn();

        builder.Property(al => al.UserId);

        builder.Property(al => al.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityName)
            .HasMaxLength(100);

        builder.Property(al => al.EntityId)
            .HasMaxLength(100);

        builder.Property(al => al.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.IpAddress)
            .HasMaxLength(50);

        builder.Property(al => al.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => al.CreatedAt);
        builder.HasIndex(al => new { al.EntityName, al.EntityId });

        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
