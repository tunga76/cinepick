using CinePick.Application.Movies.SyncCatalog;
using CinePick.Domain.ExternalProviders;
using CinePick.Domain.Movies;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Movies;

internal sealed class MovieCatalogSynchronizer(
    IDbContextFactory<CinePickDbContext> dbContextFactory,
    IMovieMetadataProvider provider,
    TimeProvider timeProvider) : IMovieCatalogSynchronizer
{
    public async Task<MovieCatalogSyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var log = new ExternalSyncLog(Guid.NewGuid(), provider.ProviderId, "movie-catalog", startedAt);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ExternalSyncLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var items = await provider.GetCatalogAsync(cancellationToken);
            var genres = await dbContext.Genres.ToDictionaryAsync(genre => genre.Slug, cancellationToken);
            var existing = await dbContext.Movies
                .Include(movie => movie.MovieGenres)
                .Where(movie => movie.ExternalProviderId == provider.ProviderId)
                .ToDictionaryAsync(movie => movie.ExternalMovieId, cancellationToken);

            var inserted = 0;
            var updated = 0;
            var synchronizedAt = timeProvider.GetUtcNow();
            var currentExternalIds = items.Select(item => item.ExternalId).ToHashSet(StringComparer.Ordinal);
            foreach (var staleMovie in existing.Values.Where(movie =>
                         !currentExternalIds.Contains(movie.ExternalMovieId)
                         && (movie.IsNowPlaying || movie.IsUpcoming)))
            {
                staleMovie.UpdateAvailability(false, false, synchronizedAt);
                updated++;
            }

            foreach (var item in items)
            {
                if (!existing.TryGetValue(item.ExternalId, out var movie))
                {
                    movie = new Movie(Guid.NewGuid(), provider.ProviderId, item.ExternalId, item.Title,
                        item.OriginalTitle, item.Overview, item.ReleaseDate, item.RuntimeMinutes,
                        item.OriginalLanguage, item.AgeRating, item.VoteAverage, item.VoteCount,
                        item.Popularity, item.IsNowPlaying, item.IsUpcoming, synchronizedAt,
                        item.PosterPath, item.BackdropPath);
                    dbContext.Movies.Add(movie);
                    inserted++;
                }
                else
                {
                    movie.UpdateMetadata(item.Title, item.OriginalTitle, item.Overview, item.ReleaseDate,
                        item.RuntimeMinutes, item.OriginalLanguage, item.AgeRating, item.VoteAverage,
                        item.VoteCount, item.Popularity, item.IsNowPlaying, item.IsUpcoming,
                        synchronizedAt, item.PosterPath, item.BackdropPath);
                    movie.MovieGenres.Clear();
                    updated++;
                }

                foreach (var slug in item.GenreSlugs.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!genres.TryGetValue(slug, out var genre))
                    {
                        throw new InvalidOperationException($"Provider genre is not mapped: {slug}");
                    }

                    movie.MovieGenres.Add(new MovieGenre(movie.Id, genre.Id));
                }
            }

            var completedAt = timeProvider.GetUtcNow();
            log.Complete(completedAt, items.Count, inserted, updated);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new MovieCatalogSyncResult(log.Id, provider.ProviderId, items.Count, inserted,
                updated, startedAt, completedAt);
        }
        catch
        {
            await using var logDbContext = await dbContextFactory.CreateDbContextAsync(
                CancellationToken.None);
            var persistedLog = await logDbContext.ExternalSyncLogs.SingleAsync(
                item => item.Id == log.Id, CancellationToken.None);
            persistedLog.Fail(timeProvider.GetUtcNow(), "movie_catalog.sync_failed");
            await logDbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
