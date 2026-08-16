using CinePick.Domain.Movies;
using CinePick.Domain.Users;
using CinePick.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(item => item.UserId);
        builder.Property(item => item.PreferredGenreSlug).HasMaxLength(100);
        builder.Property(item => item.PreferredLanguage).HasMaxLength(10);
        builder.Property(item => item.MaximumDistanceKilometers).HasPrecision(6, 2);
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserPreference>(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserMovieStateConfiguration : IEntityTypeConfiguration<UserMovieState>
{
    public void Configure(EntityTypeBuilder<UserMovieState> builder)
    {
        builder.ToTable("UserMovieStates");
        builder.HasKey(item => new { item.UserId, item.MovieId });
        builder.HasIndex(item => new { item.UserId, item.IsFavorite });
        builder.HasIndex(item => new { item.UserId, item.IsWatched });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Movie>().WithMany().HasForeignKey(item => item.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
