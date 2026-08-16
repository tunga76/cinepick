namespace CinePick.Application.Administration;

public interface IDevelopmentOperations
{
    Task<IReadOnlyList<SyncLogListItem>> GetRecentSyncLogsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DevelopmentShowtimeListItem>> GetShowtimesAsync(CancellationToken cancellationToken);
    Task<bool> SetShowtimeCancellationAsync(Guid id, bool isCancelled, CancellationToken cancellationToken);
}

public sealed record SyncLogListItem(Guid Id, string ProviderId, string Operation, string Status,
    DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int ReceivedCount, int InsertedCount,
    int UpdatedCount, string? ErrorCode);

public sealed record DevelopmentShowtimeListItem(Guid Id, string MovieTitle, string CinemaName,
    string AuditoriumName, DateTimeOffset StartsAt, bool IsCancelled, string? ExternalSyncKey);
