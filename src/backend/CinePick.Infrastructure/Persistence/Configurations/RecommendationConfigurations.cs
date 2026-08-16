using CinePick.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinePick.Infrastructure.Identity;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class RecommendationSessionConfiguration : IEntityTypeConfiguration<RecommendationSession>
{
    public void Configure(EntityTypeBuilder<RecommendationSession> builder)
    {
        builder.ToTable("RecommendationSessions"); builder.HasKey(item => item.Id);
        builder.Property(item => item.Method).HasMaxLength(50).IsRequired();
        builder.Property(item => item.GenreSlug).HasMaxLength(100);
        builder.Property(item => item.CitySlug).HasMaxLength(100);
        builder.Property(item => item.DistrictSlug).HasMaxLength(100);
        builder.Property(item => item.MaximumPrice).HasPrecision(10, 2);
        builder.Property(item => item.Language).HasMaxLength(10);
        builder.Property(item => item.Format).HasMaxLength(20);
        builder.HasIndex(item => item.CreatedAt);
        builder.HasIndex(item => new { item.UserId, item.CreatedAt });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class RecommendationCandidateSnapshotConfiguration
    : IEntityTypeConfiguration<RecommendationCandidateSnapshot>
{
    public void Configure(EntityTypeBuilder<RecommendationCandidateSnapshot> builder)
    {
        builder.ToTable("RecommendationCandidateSnapshots");
        builder.HasKey(item => new { item.SessionId, item.MovieId, item.ShowtimeId });
        builder.HasOne(item => item.Session).WithMany(item => item.Candidates)
            .HasForeignKey(item => item.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RecommendationResultRecordConfiguration
    : IEntityTypeConfiguration<RecommendationResultRecord>
{
    public void Configure(EntityTypeBuilder<RecommendationResultRecord> builder)
    {
        builder.ToTable("RecommendationResults");
        builder.HasKey(item => new { item.SessionId, item.Rank });
        builder.Property(item => item.Score).HasPrecision(5, 2);
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne(item => item.Session).WithMany(item => item.Results)
            .HasForeignKey(item => item.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
