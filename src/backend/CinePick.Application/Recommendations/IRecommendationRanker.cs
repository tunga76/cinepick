namespace CinePick.Application.Recommendations;

public interface IRecommendationRanker
{
    string Method { get; }

    Task<IReadOnlyList<RankedRecommendation>> RankAsync(string requestText,
        RecommendationFilter filter, IReadOnlyList<RecommendationCandidate> candidates,
        CancellationToken cancellationToken);
}

public sealed record RankedRecommendation(Guid MovieId, Guid ShowtimeId, decimal Score, string Reason);
