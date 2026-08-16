using CinePick.Application.Recommendations;

namespace CinePick.Infrastructure.Recommendations;

internal sealed class MockRecommendationRanker : IRecommendationRanker
{
    public string Method => "mock-ai";

    public Task<IReadOnlyList<RankedRecommendation>> RankAsync(string requestText,
        RecommendationFilter filter, IReadOnlyList<RecommendationCandidate> candidates,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RankedRecommendation> ranked = candidates
            .OrderByDescending(item => item.PersonalizationScore)
            .ThenByDescending(item => item.VoteAverage)
            .ThenByDescending(item => item.Popularity)
            .ThenBy(item => item.StartsAt)
            .Take(3)
            .Select((item, index) => new RankedRecommendation(item.MovieId, item.ShowtimeId,
                90m - (index * 5m), $"{item.MovieTitle}, doğrulanmış adaylar içinde güçlü bir eşleşme."))
            .ToArray();
        return Task.FromResult(ranked);
    }
}
