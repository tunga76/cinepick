using CinePick.Application.Recommendations;
using CinePick.Domain.Recommendations;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Recommendations;

internal sealed class RecommendationAuditStore(IDbContextFactory<CinePickDbContext> factory)
    : IRecommendationAuditStore, IRecommendationHistoryQuery
{
    public async Task SaveAsync(RecommendationAuditEntry entry, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var filter = entry.Filter;
        var session = new RecommendationSession(entry.SessionId, entry.CreatedAt, entry.Method,
            filter.StartsFrom, filter.StartsBefore, filter.MaximumRuntimeMinutes, filter.GenreSlug,
            filter.CitySlug, filter.DistrictSlug, filter.MaximumPrice, filter.Language, filter.Format,
            entry.UserId);
        foreach (var candidate in entry.Candidates)
            session.Candidates.Add(new RecommendationCandidateSnapshot(entry.SessionId,
                candidate.MovieId, candidate.ShowtimeId));
        foreach (var result in entry.Results)
            session.Results.Add(new RecommendationResultRecord(entry.SessionId, result.Rank,
                result.MovieId, result.ShowtimeId, result.Score, result.Reason));
        db.RecommendationSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecommendationHistoryItem>> GetAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var sessions = await db.RecommendationSessions.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt).Take(20)
            .Select(item => new
            {
                item.Id, item.CreatedAt, item.Method,
                Results = item.Results.OrderBy(result => result.Rank).Select(result => new
                {
                    result.Rank, result.MovieId, result.Score, result.Reason,
                }).ToArray(),
            }).ToArrayAsync(cancellationToken);
        var movieIds = sessions.SelectMany(item => item.Results).Select(item => item.MovieId)
            .Distinct().ToArray();
        var titles = await db.Movies.AsNoTracking().Where(item => movieIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);
        return sessions.Select(session => new RecommendationHistoryItem(session.Id,
            session.CreatedAt, session.Method, session.Results.Length,
            session.Results.Select(result => new RecommendationHistoryResult(result.Rank,
                result.MovieId, titles.GetValueOrDefault(result.MovieId, "Bilinmeyen film"),
                result.Score, result.Reason)).ToArray())).ToArray();
    }
}
