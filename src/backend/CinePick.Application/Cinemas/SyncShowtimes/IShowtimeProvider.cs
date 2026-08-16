namespace CinePick.Application.Cinemas.SyncShowtimes;

public interface IShowtimeProvider
{
    string ProviderId { get; }
    Task<IReadOnlyList<ShowtimeMetadataItem>> GetShowtimesAsync(CancellationToken cancellationToken);
}

public sealed record ShowtimeMetadataItem(string ExternalId, string ExternalMovieId,
    string CinemaName, string AuditoriumName, DateTimeOffset StartsAt, decimal Price,
    string Currency, string Language, string Format, string TicketUrl, bool IsCancelled);
