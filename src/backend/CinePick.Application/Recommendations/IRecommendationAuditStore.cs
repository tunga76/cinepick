namespace CinePick.Application.Recommendations;

public interface IRecommendationAuditStore
{
    Task SaveAsync(RecommendationAuditEntry entry, CancellationToken cancellationToken);
}

public sealed record RecommendationAuditEntry(Guid SessionId, DateTimeOffset CreatedAt,
    RecommendationFilter Filter, string Method, IReadOnlyList<RecommendationCandidateReference> Candidates,
    IReadOnlyList<RecommendationAuditResult> Results, Guid? UserId = null);

public sealed record RecommendationCandidateReference(Guid MovieId, Guid ShowtimeId);
public sealed record RecommendationAuditResult(int Rank, Guid MovieId, Guid ShowtimeId,
    decimal Score, string Reason);

public interface IRecommendationHistoryQuery
{
    Task<IReadOnlyList<RecommendationHistoryItem>> GetAsync(Guid userId,
        CancellationToken cancellationToken);
}

public sealed record RecommendationHistoryItem(Guid SessionId, DateTimeOffset CreatedAt,
    string Method, int ResultCount, IReadOnlyList<RecommendationHistoryResult> Results);
public sealed record RecommendationHistoryResult(int Rank, Guid MovieId, string MovieTitle,
    decimal Score, string Reason);
