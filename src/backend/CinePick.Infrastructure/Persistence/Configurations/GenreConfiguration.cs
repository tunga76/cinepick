using CinePick.Domain.Movies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genres");
        builder.HasKey(genre => genre.Id);
        builder.Property(genre => genre.Name).HasMaxLength(100).IsRequired();
        builder.Property(genre => genre.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(genre => genre.Slug).IsUnique();
    }
}
