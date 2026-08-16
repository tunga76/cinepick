using CinePick.Application.Recommendations;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Recommendations;

internal sealed class RecommendationCandidateQuery(IDbContextFactory<CinePickDbContext> factory)
    : IRecommendationCandidateQuery
{
    public async Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(
        RecommendationFilter filter, Guid? userId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Showtimes.AsNoTracking().Where(item => !item.IsCancelled
            && item.StartsAt >= filter.StartsFrom && item.StartsAt < filter.StartsBefore);
        if (filter.MaximumRuntimeMinutes is not null)
            query = query.Where(item => item.Movie.RuntimeMinutes <= filter.MaximumRuntimeMinutes);
        if (filter.GenreSlug is not null)
            query = query.Where(item => item.Movie.MovieGenres.Any(link => link.Genre.Slug == filter.GenreSlug));
        if (filter.CitySlug is not null)
            query = query.Where(item => item.Auditorium.Cinema.District.City.Slug == filter.CitySlug);
        if (filter.DistrictSlug is not null)
            query = query.Where(item => item.Auditorium.Cinema.District.Slug == filter.DistrictSlug);
        if (filter.MaximumPrice is not null)
            query = query.Where(item => item.Price <= filter.MaximumPrice);
        if (filter.Language is not null)
            query = query.Where(item => item.Language == filter.Language);
        if (filter.Format is not null)
            query = query.Where(item => item.Format == filter.Format);

        var candidates = await query.OrderByDescending(item => item.Movie.Popularity)
            .ThenBy(item => item.StartsAt).Take(20)
            .Select(item => new RecommendationCandidate(item.MovieId, item.Id, item.Movie.Title,
                item.Auditorium.Cinema.Name, item.Auditorium.Cinema.District.Name,
                item.Auditorium.Name, item.StartsAt,
                item.StartsAt.AddMinutes(item.Movie.RuntimeMinutes), item.Movie.RuntimeMinutes,
                item.Price, item.Currency, item.Language, item.Format, item.Movie.VoteAverage,
                item.Movie.Popularity, item.Movie.MovieGenres.OrderBy(link => link.Genre.Name)
                    .Select(link => link.Genre.Name).ToArray(), item.TicketUrl))
            .ToListAsync(cancellationToken);
        if (userId is null || candidates.Count == 0) return candidates;
        var movieIds = candidates.Select(item => item.MovieId).Distinct().ToArray();
        var signals = await db.UserMovieStates.AsNoTracking()
            .Where(item => item.UserId == userId && movieIds.Contains(item.MovieId))
            .ToDictionaryAsync(item => item.MovieId,
                item => (item.IsFavorite ? 8m : 0m) + (item.Rating ?? 0), cancellationToken);
        return candidates.Select(item => item with
            { PersonalizationScore = signals.GetValueOrDefault(item.MovieId) }).ToArray();
    }
}
