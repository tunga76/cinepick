using CinePick.Domain.ExternalProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class ExternalSyncLogConfiguration : IEntityTypeConfiguration<ExternalSyncLog>
{
    public void Configure(EntityTypeBuilder<ExternalSyncLog> builder)
    {
        builder.ToTable("ExternalSyncLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.ProviderId).HasMaxLength(50).IsRequired();
        builder.Property(log => log.Operation).HasMaxLength(100).IsRequired();
        builder.Property(log => log.Status).HasMaxLength(20).IsRequired();
        builder.Property(log => log.ErrorCode).HasMaxLength(100);
        builder.HasIndex(log => new { log.ProviderId, log.StartedAt });
    }
}
