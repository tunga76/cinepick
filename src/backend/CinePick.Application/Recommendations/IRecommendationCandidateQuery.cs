namespace CinePick.Application.Recommendations;

public interface IRecommendationCandidateQuery
{
    Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(
        RecommendationFilter filter, Guid? userId, CancellationToken cancellationToken);
}
