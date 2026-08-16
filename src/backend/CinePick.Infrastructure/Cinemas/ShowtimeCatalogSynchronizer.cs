using CinePick.Application.Cinemas.SyncShowtimes;
using CinePick.Domain.Cinemas;
using CinePick.Domain.ExternalProviders;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CinePick.Infrastructure.Cinemas;

internal sealed class ShowtimeCatalogSynchronizer(
    IDbContextFactory<CinePickDbContext> dbContextFactory,
    IShowtimeProvider provider,
    IOptions<ShowtimeProviderOptions> options,
    TimeProvider timeProvider) : IShowtimeCatalogSynchronizer
{
    public async Task<ShowtimeCatalogSyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var log = new ExternalSyncLog(Guid.NewGuid(), provider.ProviderId, "showtime-catalog", startedAt);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ExternalSyncLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var items = await provider.GetShowtimesAsync(cancellationToken);
            EnsureValidProviderData(items);
            var movieExternalIds = items.Select(item => item.ExternalMovieId).Distinct().ToArray();
            var movies = await db.Movies.Where(movie => movie.ExternalProviderId == "mock"
                    && movieExternalIds.Contains(movie.ExternalMovieId))
                .ToDictionaryAsync(movie => movie.ExternalMovieId, cancellationToken);
            var cinemaNames = items.Select(item => item.CinemaName).Distinct().ToArray();
            var auditoriums = await db.Auditoriums.Include(item => item.Cinema)
                .Where(item => cinemaNames.Contains(item.Cinema.Name)).ToListAsync(cancellationToken);
            var auditoriumLookup = auditoriums.ToDictionary(
                item => $"{item.Cinema.Name}|{item.Name}", StringComparer.OrdinalIgnoreCase);
            var syncKeys = items.Select(item => SyncKey(item.ExternalId)).ToArray();
            var existing = await db.Showtimes.Where(item => item.ExternalSyncKey != null
                    && syncKeys.Contains(item.ExternalSyncKey))
                .ToDictionaryAsync(item => item.ExternalSyncKey!, cancellationToken);
            var inserted = 0;
            var updated = 0;
            foreach (var item in items)
            {
                if (!movies.TryGetValue(item.ExternalMovieId, out var movie))
                    throw new InvalidOperationException("Provider movie is not mapped.");
                if (!auditoriumLookup.TryGetValue($"{item.CinemaName}|{item.AuditoriumName}", out var auditorium))
                    throw new InvalidOperationException("Provider auditorium is not mapped.");
                var key = SyncKey(item.ExternalId);
                if (!existing.TryGetValue(key, out var showtime))
                {
                    showtime = new Showtime(Guid.NewGuid(), movie.Id, auditorium.Id, item.StartsAt,
                        item.Price, item.Currency, item.Language, item.Format, item.TicketUrl, key);
                    if (item.IsCancelled)
                        showtime.Update(item.StartsAt, item.Price, item.Currency, item.Language,
                            item.Format, item.TicketUrl, true);
                    db.Showtimes.Add(showtime);
                    inserted++;
                }
                else
                {
                    showtime.Update(item.StartsAt, item.Price, item.Currency, item.Language,
                        item.Format, item.TicketUrl, item.IsCancelled);
                    updated++;
                }
            }
            var completedAt = timeProvider.GetUtcNow();
            log.Complete(completedAt, items.Count, inserted, updated);
            await db.SaveChangesAsync(cancellationToken);
            return new ShowtimeCatalogSyncResult(log.Id, provider.ProviderId, items.Count, inserted,
                updated, startedAt, completedAt);
        }
        catch
        {
            await using var logDb = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            var persistedLog = await logDb.ExternalSyncLogs.SingleAsync(item => item.Id == log.Id,
                CancellationToken.None);
            persistedLog.Fail(timeProvider.GetUtcNow(), "showtime_catalog.sync_failed");
            await logDb.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private string SyncKey(string externalId) => $"{provider.ProviderId}:{externalId}";

    private void EnsureValidProviderData(IReadOnlyList<ShowtimeMetadataItem> items)
    {
        if (items.Select(item => item.ExternalId).Distinct(StringComparer.Ordinal).Count() != items.Count)
            throw new InvalidOperationException("Provider returned duplicate showtime identifiers.");
        foreach (var item in items)
        {
            if (!Uri.TryCreate(item.TicketUrl, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !options.Value.AllowedTicketHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Provider returned a disallowed ticket URL.");
        }
    }
}
