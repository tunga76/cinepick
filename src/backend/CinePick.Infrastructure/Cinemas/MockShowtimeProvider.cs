using CinePick.Application.Cinemas.SyncShowtimes;

namespace CinePick.Infrastructure.Cinemas;

internal sealed class MockShowtimeProvider(TimeProvider timeProvider) : IShowtimeProvider
{
    public string ProviderId => "mock-showtimes";

    public Task<IReadOnlyList<ShowtimeMetadataItem>> GetShowtimesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tomorrow = timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1);
        IReadOnlyList<ShowtimeMetadataItem> items =
        [
            new("showtime-001", "mock-001", "CinePick Kadıköy", "Salon 1",
                new DateTimeOffset(tomorrow.AddHours(18).AddMinutes(30), TimeSpan.Zero),
                275m, "TRY", "tr", "2D",
                "https://tickets.example.invalid/provider/showtime-001", false),
            new("showtime-002", "mock-002", "CinePick Kadıköy", "Salon 2",
                new DateTimeOffset(tomorrow.AddHours(20).AddMinutes(30), TimeSpan.Zero),
                325m, "TRY", "tr", "IMAX",
                "https://tickets.example.invalid/provider/showtime-002", false),
        ];
        return Task.FromResult(items);
    }
}
