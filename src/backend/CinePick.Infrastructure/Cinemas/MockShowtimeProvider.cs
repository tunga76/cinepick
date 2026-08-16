using CinePick.Application.Cinemas.SyncShowtimes;

namespace CinePick.Infrastructure.Cinemas;

internal sealed class MockShowtimeProvider : IShowtimeProvider
{
    public string ProviderId => "mock-showtimes";

    public Task<IReadOnlyList<ShowtimeMetadataItem>> GetShowtimesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ShowtimeMetadataItem> items =
        [
            new("showtime-001", "mock-001", "CinePick Kadıköy", "Salon 1",
                new DateTimeOffset(2026, 8, 16, 18, 30, 0, TimeSpan.Zero), 275m, "TRY", "tr", "2D",
                "https://tickets.example.invalid/provider/showtime-001", false),
            new("showtime-002", "mock-002", "CinePick Kadıköy", "Salon 2",
                new DateTimeOffset(2026, 8, 16, 20, 30, 0, TimeSpan.Zero), 325m, "TRY", "tr", "IMAX",
                "https://tickets.example.invalid/provider/showtime-002", false),
        ];
        return Task.FromResult(items);
    }
}
