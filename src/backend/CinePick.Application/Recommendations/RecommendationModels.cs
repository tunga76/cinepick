namespace CinePick.Application.Recommendations;

public sealed record RecommendationCandidate(Guid MovieId, Guid ShowtimeId, string MovieTitle,
    string CinemaName, string DistrictName, string AuditoriumName, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, int RuntimeMinutes, decimal Price, string Currency, string Language,
    string Format, decimal VoteAverage, decimal Popularity, IReadOnlyList<string> Genres,
    string TicketUrl, decimal PersonalizationScore = 0);

public sealed record RecommendationItem(Guid MovieId, Guid ShowtimeId, string MovieTitle,
    string CinemaName, string DistrictName, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    decimal Price, string Currency, string Language, string Format, decimal Score,
    string Reason, string TicketUrl);

public sealed record RecommendationResponse(Guid SessionId, RecommendationFilter Filter, string Method,
    int CandidateCount, IReadOnlyList<RecommendationItem> Items);
