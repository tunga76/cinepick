using CinePick.Application.Cinemas;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CinePick.Domain.Cinemas;

namespace CinePick.Infrastructure.Cinemas;

internal sealed class CinemaCatalogQuery(IDbContextFactory<CinePickDbContext> factory) : ICinemaCatalogQuery
{
    public async Task<IReadOnlyList<CityListItem>> GetCitiesAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Cities.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new CityListItem(x.Id, x.Name)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CinemaListItem>> GetCinemasAsync(Guid? cityId, double? latitude,
        double? longitude, double? radiusKilometers, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Cinemas.AsNoTracking().AsQueryable();
        if (cityId is not null) query = query.Where(x => x.District.CityId == cityId);
        var cinemas = await query.OrderBy(x => x.District.City.Name).ThenBy(x => x.Name)
            .Select(x => new CinemaListItem(x.Id, x.Name, x.District.City.Name, x.District.Name,
                x.Address, x.Latitude, x.Longitude, null)).ToListAsync(cancellationToken);
        if (latitude is null || longitude is null) return cinemas;

        return cinemas.Select(cinema => cinema with
            {
                DistanceKilometers = GeoDistance.HaversineKilometers(latitude.Value, longitude.Value,
                    (double)cinema.Latitude, (double)cinema.Longitude),
            })
            .Where(cinema => radiusKilometers is null || cinema.DistanceKilometers <= radiusKilometers)
            .OrderBy(cinema => cinema.DistanceKilometers)
            .ToArray();
    }

    public async Task<CinemaDetail?> GetCinemaAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Cinemas.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new CinemaDetail(x.Id, x.Name, x.District.City.Name, x.District.Name,
                x.Address, x.Latitude, x.Longitude, x.Auditoriums.OrderBy(a => a.Name)
                    .Select(a => new AuditoriumListItem(a.Id, a.Name, a.Capacity)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShowtimeListItem>> GetShowtimesAsync(Guid? cinemaId, Guid? movieId,
        DateTimeOffset startsFrom, DateTimeOffset startsBefore, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Showtimes.AsNoTracking().Where(x => !x.IsCancelled
            && x.StartsAt >= startsFrom && x.StartsAt < startsBefore);
        if (cinemaId is not null) query = query.Where(x => x.Auditorium.CinemaId == cinemaId);
        if (movieId is not null) query = query.Where(x => x.MovieId == movieId);
        return await query.OrderBy(x => x.StartsAt).Take(500)
            .Select(x => new ShowtimeListItem(x.Id, x.MovieId, x.Movie.Title,
                x.Auditorium.CinemaId, x.Auditorium.Cinema.Name, x.AuditoriumId, x.Auditorium.Name,
                x.StartsAt, x.StartsAt.AddMinutes(x.Movie.RuntimeMinutes), x.Price, x.Currency,
                x.Language, x.Format, x.TicketUrl)).ToListAsync(cancellationToken);
    }
}
