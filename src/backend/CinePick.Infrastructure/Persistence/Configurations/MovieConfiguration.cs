using CinePick.Domain.Movies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movies");
        builder.HasKey(movie => movie.Id);
        builder.Property(movie => movie.ExternalProviderId).HasMaxLength(50).IsRequired();
        builder.Property(movie => movie.ExternalMovieId).HasMaxLength(100).IsRequired();
        builder.HasIndex(movie => new { movie.ExternalProviderId, movie.ExternalMovieId }).IsUnique();
        builder.Property(movie => movie.Title).HasMaxLength(300).IsRequired();
        builder.Property(movie => movie.OriginalTitle).HasMaxLength(300).IsRequired();
        builder.Property(movie => movie.Overview).HasMaxLength(4000).IsRequired();
        builder.Property(movie => movie.OriginalLanguage).HasMaxLength(10).IsRequired();
        builder.Property(movie => movie.AgeRating).HasConversion<int>();
        builder.Property(movie => movie.PosterPath).HasMaxLength(500);
        builder.Property(movie => movie.BackdropPath).HasMaxLength(500);
        builder.Property(movie => movie.VoteAverage).HasPrecision(4, 2);
        builder.Property(movie => movie.Popularity).HasPrecision(10, 2);
        builder.HasIndex(movie => new { movie.IsNowPlaying, movie.Popularity });
        builder.HasIndex(movie => new { movie.IsUpcoming, movie.ReleaseDate });
    }
}
