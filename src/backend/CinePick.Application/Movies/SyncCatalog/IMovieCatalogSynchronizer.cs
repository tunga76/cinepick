namespace CinePick.Application.Movies.SyncCatalog;

public interface IMovieCatalogSynchronizer
{
    Task<MovieCatalogSyncResult> SynchronizeAsync(CancellationToken cancellationToken);
}

public sealed record MovieCatalogSyncResult(
    Guid SyncId,
    string ProviderId,
    int ReceivedCount,
    int InsertedCount,
    int UpdatedCount,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);
