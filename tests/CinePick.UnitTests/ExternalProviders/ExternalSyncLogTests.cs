using CinePick.Domain.ExternalProviders;

namespace CinePick.UnitTests.ExternalProviders;

public sealed class ExternalSyncLogTests
{
    [Fact]
    public void CompleteStoresOnlyOperationalSummary()
    {
        var startedAt = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        var log = new ExternalSyncLog(Guid.NewGuid(), "mock", "movie-catalog", startedAt);

        log.Complete(startedAt.AddSeconds(2), 2, 1, 1);

        Assert.Equal("succeeded", log.Status);
        Assert.Equal(2, log.ReceivedCount);
        Assert.Null(log.ErrorCode);
    }
}
