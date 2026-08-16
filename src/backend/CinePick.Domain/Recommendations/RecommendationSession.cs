namespace CinePick.Domain.Recommendations;

public sealed class RecommendationSession
{
    private RecommendationSession() { }

    public RecommendationSession(Guid id, DateTimeOffset createdAt, string method,
        DateTimeOffset startsFrom, DateTimeOffset startsBefore, int? maximumRuntimeMinutes,
        string? genreSlug, string? citySlug, string? districtSlug, decimal? maximumPrice,
        string? language, string? format, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        Id = id; CreatedAt = createdAt; Method = method; StartsFrom = startsFrom;
        StartsBefore = startsBefore; MaximumRuntimeMinutes = maximumRuntimeMinutes;
        GenreSlug = genreSlug; CitySlug = citySlug; DistrictSlug = districtSlug;
        MaximumPrice = maximumPrice; Language = language; Format = format;
        UserId = userId;
    }

    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public DateTimeOffset StartsFrom { get; private set; }
    public DateTimeOffset StartsBefore { get; private set; }
    public int? MaximumRuntimeMinutes { get; private set; }
    public string? GenreSlug { get; private set; }
    public string? CitySlug { get; private set; }
    public string? DistrictSlug { get; private set; }
    public decimal? MaximumPrice { get; private set; }
    public string? Language { get; private set; }
    public string? Format { get; private set; }
    public Guid? UserId { get; private set; }
    public ICollection<RecommendationCandidateSnapshot> Candidates { get; } = new List<RecommendationCandidateSnapshot>();
    public ICollection<RecommendationResultRecord> Results { get; } = new List<RecommendationResultRecord>();
}

public sealed class RecommendationCandidateSnapshot
{
    private RecommendationCandidateSnapshot() { }
    public RecommendationCandidateSnapshot(Guid sessionId, Guid movieId, Guid showtimeId)
    { SessionId = sessionId; MovieId = movieId; ShowtimeId = showtimeId; }
    public Guid SessionId { get; private set; }
    public RecommendationSession Session { get; private set; } = null!;
    public Guid MovieId { get; private set; }
    public Guid ShowtimeId { get; private set; }
}

public sealed class RecommendationResultRecord
{
    private RecommendationResultRecord() { }
    public RecommendationResultRecord(Guid sessionId, int rank, Guid movieId, Guid showtimeId,
        decimal score, string reason)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rank);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        SessionId = sessionId; Rank = rank; MovieId = movieId; ShowtimeId = showtimeId;
        Score = score; Reason = reason;
    }
    public Guid SessionId { get; private set; }
    public RecommendationSession Session { get; private set; } = null!;
    public int Rank { get; private set; }
    public Guid MovieId { get; private set; }
    public Guid ShowtimeId { get; private set; }
    public decimal Score { get; private set; }
    public string Reason { get; private set; } = string.Empty;
}
