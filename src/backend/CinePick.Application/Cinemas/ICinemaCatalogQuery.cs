namespace CinePick.Application.Cinemas;

public interface ICinemaCatalogQuery
{
    Task<IReadOnlyList<CityListItem>> GetCitiesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CinemaListItem>> GetCinemasAsync(Guid? cityId, double? latitude,
        double? longitude, double? radiusKilometers, CancellationToken cancellationToken);
    Task<CinemaDetail?> GetCinemaAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShowtimeListItem>> GetShowtimesAsync(
        Guid? cinemaId, Guid? movieId, DateTimeOffset startsFrom, DateTimeOffset startsBefore,
        CancellationToken cancellationToken);
}

public sealed record CityListItem(Guid Id, string Name);

public sealed record CinemaListItem(Guid Id, string Name, string City, string District,
    string Address, decimal Latitude, decimal Longitude, double? DistanceKilometers);

public sealed record CinemaDetail(Guid Id, string Name, string City, string District,
    string Address, decimal Latitude, decimal Longitude, IReadOnlyList<AuditoriumListItem> Auditoriums);

public sealed record AuditoriumListItem(Guid Id, string Name, int Capacity);

public sealed record ShowtimeListItem(Guid Id, Guid MovieId, string MovieTitle, Guid CinemaId,
    string CinemaName, Guid AuditoriumId, string AuditoriumName, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, decimal Price, string Currency, string Language, string Format,
    string TicketUrl);
