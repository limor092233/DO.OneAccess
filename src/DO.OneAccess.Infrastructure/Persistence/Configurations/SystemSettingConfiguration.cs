using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(ss => ss.SystemSettingId);
        builder.Property(ss => ss.SystemSettingId)
            .UseIdentityColumn();

        builder.Property(ss => ss.SystemId)
            .IsRequired();

        builder.Property(ss => ss.SettingKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ss => ss.SettingValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(ss => ss.IsSecret)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ss => ss.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(ss => ss.CreatedBy);

        builder.Property(ss => ss.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(ss => ss.UpdatedBy);

        builder.HasIndex(ss => new { ss.SystemId, ss.SettingKey })
            .IsUnique();

        builder.HasOne(ss => ss.System)
            .WithMany(s => s.SystemSettings)
            .HasForeignKey(ss => ss.SystemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
