using CinePick.Domain.Movies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class MovieGenreConfiguration : IEntityTypeConfiguration<MovieGenre>
{
    public void Configure(EntityTypeBuilder<MovieGenre> builder)
    {
        builder.ToTable("MovieGenres");
        builder.HasKey(movieGenre => new { movieGenre.MovieId, movieGenre.GenreId });
        builder.HasOne(movieGenre => movieGenre.Movie)
            .WithMany(movie => movie.MovieGenres)
            .HasForeignKey(movieGenre => movieGenre.MovieId);
        builder.HasOne(movieGenre => movieGenre.Genre)
            .WithMany(genre => genre.MovieGenres)
            .HasForeignKey(movieGenre => movieGenre.GenreId);
    }
}
