using CinePick.Domain.Cinemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinePick.Infrastructure.Persistence.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("Districts"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.CityId, x.Slug }).IsUnique();
        builder.HasOne(x => x.City).WithMany(x => x.Districts).HasForeignKey(x => x.CityId);
    }
}

internal sealed class CinemaConfiguration : IEntityTypeConfiguration<Cinema>
{
    public void Configure(EntityTypeBuilder<Cinema> builder)
    {
        builder.ToTable("Cinemas"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.HasOne(x => x.District).WithMany(x => x.Cinemas).HasForeignKey(x => x.DistrictId);
    }
}

internal sealed class AuditoriumConfiguration : IEntityTypeConfiguration<Auditorium>
{
    public void Configure(EntityTypeBuilder<Auditorium> builder)
    {
        builder.ToTable("Auditoriums"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.CinemaId, x.Name }).IsUnique();
        builder.HasOne(x => x.Cinema).WithMany(x => x.Auditoriums).HasForeignKey(x => x.CinemaId);
    }
}

internal sealed class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.ToTable("Showtimes"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TicketUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExternalSyncKey).HasMaxLength(200);
        builder.HasIndex(x => x.ExternalSyncKey).IsUnique().HasFilter("[ExternalSyncKey] IS NOT NULL");
        builder.HasIndex(x => new { x.IsCancelled, x.StartsAt });
        builder.HasIndex(x => new { x.MovieId, x.StartsAt, x.IsCancelled });
        builder.HasIndex(x => new { x.AuditoriumId, x.StartsAt, x.IsCancelled });
        builder.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId);
        builder.HasOne(x => x.Auditorium).WithMany(x => x.Showtimes).HasForeignKey(x => x.AuditoriumId);
    }
}
