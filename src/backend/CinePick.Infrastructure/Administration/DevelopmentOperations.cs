using CinePick.Application.Administration;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Administration;

internal sealed class DevelopmentOperations(IDbContextFactory<CinePickDbContext> factory)
    : IDevelopmentOperations
{
    public async Task<IReadOnlyList<SyncLogListItem>> GetRecentSyncLogsAsync(
        CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.ExternalSyncLogs.AsNoTracking().OrderByDescending(item => item.StartedAt)
            .Take(20).Select(item => new SyncLogListItem(item.Id, item.ProviderId, item.Operation,
                item.Status, item.StartedAt, item.CompletedAt, item.ReceivedCount,
                item.InsertedCount, item.UpdatedCount, item.ErrorCode)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DevelopmentShowtimeListItem>> GetShowtimesAsync(
        CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Showtimes.AsNoTracking().OrderBy(item => item.StartsAt).Take(250)
            .Select(item => new DevelopmentShowtimeListItem(item.Id, item.Movie.Title,
                item.Auditorium.Cinema.Name, item.Auditorium.Name, item.StartsAt,
                item.IsCancelled, item.ExternalSyncKey)).ToListAsync(cancellationToken);
    }

    public async Task<bool> SetShowtimeCancellationAsync(
        Guid id, bool isCancelled, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var showtime = await db.Showtimes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (showtime is null) return false;
        showtime.SetCancellation(isCancelled);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
