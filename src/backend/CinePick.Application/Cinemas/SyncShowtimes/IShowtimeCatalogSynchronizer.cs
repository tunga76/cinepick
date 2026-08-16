namespace CinePick.Application.Cinemas.SyncShowtimes;

public interface IShowtimeCatalogSynchronizer
{
    Task<ShowtimeCatalogSyncResult> SynchronizeAsync(CancellationToken cancellationToken);
}

public sealed record ShowtimeCatalogSyncResult(Guid SyncId, string ProviderId, int ReceivedCount,
    int InsertedCount, int UpdatedCount, DateTimeOffset StartedAt, DateTimeOffset CompletedAt);
