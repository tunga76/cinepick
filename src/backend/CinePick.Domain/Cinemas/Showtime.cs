using CinePick.Domain.Movies;

namespace CinePick.Domain.Cinemas;

public sealed class Showtime
{
    private Showtime() { }
    public Showtime(Guid id, Guid movieId, Guid auditoriumId, DateTimeOffset startsAt,
        decimal price, string currency, string language, string format, string ticketUrl,
        string? externalSyncKey = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketUrl);
        Id = id; MovieId = movieId; AuditoriumId = auditoriumId; StartsAt = startsAt;
        Price = price; Currency = currency; Language = language; Format = format; TicketUrl = ticketUrl;
        ExternalSyncKey = externalSyncKey;
    }
    public Guid Id { get; private set; }
    public Guid MovieId { get; private set; }
    public Movie Movie { get; private set; } = null!;
    public Guid AuditoriumId { get; private set; }
    public Auditorium Auditorium { get; private set; } = null!;
    public DateTimeOffset StartsAt { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public string Format { get; private set; } = string.Empty;
    public string TicketUrl { get; private set; } = string.Empty;
    public bool IsCancelled { get; private set; }
    public string? ExternalSyncKey { get; private set; }

    public void Update(DateTimeOffset startsAt, decimal price, string currency, string language,
        string format, string ticketUrl, bool isCancelled)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketUrl);
        StartsAt = startsAt; Price = price; Currency = currency; Language = language;
        Format = format; TicketUrl = ticketUrl; IsCancelled = isCancelled;
    }

    public void SetCancellation(bool isCancelled) => IsCancelled = isCancelled;
}
