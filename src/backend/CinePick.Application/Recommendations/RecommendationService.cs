namespace CinePick.Application.Recommendations;

public sealed class RecommendationService(
    IRecommendationRequestParser parser,
    IRecommendationCandidateQuery candidateQuery,
    IRecommendationRanker ranker,
    IRecommendationAuditStore auditStore,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan RankerTimeout = TimeSpan.FromSeconds(2);

    public async Task<RecommendationResponse> RecommendAsync(
        string text, CancellationToken cancellationToken, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var filter = parser.Parse(text);
        var candidates = await candidateQuery.GetCandidatesAsync(filter, userId, cancellationToken);
        string method;
        IReadOnlyList<RecommendationItem> items;
        if (candidates.Count == 0)
        {
            method = "no-candidates";
            items = [];
        }
        else
        {
            try
            {
                using var rankerCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                rankerCancellation.CancelAfter(RankerTimeout);
                var ranked = await ranker.RankAsync(text, filter, candidates, rankerCancellation.Token)
                    .WaitAsync(RankerTimeout, cancellationToken);
                items = ValidateAndMap(ranked, candidates);
                method = ranker.Method;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                method = "deterministic-fallback";
                items = DeterministicRank(candidates, filter);
            }
            catch (Exception exception) when (exception is TimeoutException
                or InvalidOperationException or FormatException or HttpRequestException)
            {
                method = "deterministic-fallback";
                items = DeterministicRank(candidates, filter);
            }
        }

        var sessionId = Guid.NewGuid();
        var auditEntry = new RecommendationAuditEntry(sessionId, timeProvider.GetUtcNow(), filter,
            method, candidates.Select(candidate => new RecommendationCandidateReference(
                candidate.MovieId, candidate.ShowtimeId)).ToArray(),
            items.Select((item, index) => new RecommendationAuditResult(index + 1,
                item.MovieId, item.ShowtimeId, item.Score, item.Reason)).ToArray(), userId);
        await auditStore.SaveAsync(auditEntry, cancellationToken);
        return new RecommendationResponse(sessionId, filter, method, candidates.Count, items);
    }

    private static RecommendationItem[] ValidateAndMap(
        IReadOnlyList<RankedRecommendation> ranked,
        IReadOnlyList<RecommendationCandidate> candidates)
    {
        if (ranked.Count is < 1 or > 3)
            throw new InvalidOperationException("Ranker result count is outside the allowed range.");
        if (ranked.Select(item => item.ShowtimeId).Distinct().Count() != ranked.Count)
            throw new InvalidOperationException("Ranker returned duplicate showtime identifiers.");
        var allowlist = candidates.ToDictionary(item => (item.MovieId, item.ShowtimeId));
        return ranked.Select(item =>
        {
            if (!allowlist.TryGetValue((item.MovieId, item.ShowtimeId), out var candidate))
                throw new InvalidOperationException("Ranker returned an identifier outside the candidate set.");
            if (item.Score is < 0 or > 100 || string.IsNullOrWhiteSpace(item.Reason))
                throw new InvalidOperationException("Ranker returned an invalid score or reason.");
            return Map(candidate, item.Score, item.Reason.Trim());
        }).ToArray();
    }

    private static RecommendationItem[] DeterministicRank(
        IReadOnlyList<RecommendationCandidate> candidates, RecommendationFilter filter) =>
        candidates.Select(candidate => new { Candidate = candidate, Score = Score(candidate, filter) })
            .OrderByDescending(item => item.Score).ThenBy(item => item.Candidate.StartsAt)
            .ThenBy(item => item.Candidate.MovieId).Take(3)
            .Select(item => Map(item.Candidate, item.Score, BuildReason(item.Candidate, filter)))
            .ToArray();

    private static RecommendationItem Map(
        RecommendationCandidate candidate, decimal score, string reason) =>
        new(candidate.MovieId, candidate.ShowtimeId, candidate.MovieTitle, candidate.CinemaName,
            candidate.DistrictName, candidate.StartsAt, candidate.EndsAt, candidate.Price,
            candidate.Currency, candidate.Language, candidate.Format, score, reason,
            candidate.TicketUrl);

    private static decimal Score(RecommendationCandidate candidate, RecommendationFilter filter)
    {
        var rating = Math.Clamp(candidate.VoteAverage / 10m, 0m, 1m) * 60m;
        var popularity = Math.Clamp(candidate.Popularity / 100m, 0m, 1m) * 20m;
        var totalWindowMinutes = Math.Max(1, (filter.StartsBefore - filter.StartsFrom).TotalMinutes);
        var elapsedMinutes = Math.Max(0, (candidate.StartsAt - filter.StartsFrom).TotalMinutes);
        var time = (decimal)Math.Clamp(1 - (elapsedMinutes / totalWindowMinutes), 0, 1) * 20m;
        return decimal.Round(Math.Min(100m, rating + popularity + time
            + candidate.PersonalizationScore), 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildReason(RecommendationCandidate candidate, RecommendationFilter filter)
    {
        var matches = new List<string> { $"{candidate.VoteAverage:0.0}/10 puan" };
        if (filter.MaximumRuntimeMinutes is not null) matches.Add($"{candidate.RuntimeMinutes} dakika");
        if (filter.GenreSlug is not null)
            matches.Add(candidate.Genres.First(genre => string.Equals(ToSlug(genre),
                filter.GenreSlug, StringComparison.Ordinal)));
        if (filter.DistrictSlug is not null) matches.Add(candidate.DistrictName);
        return string.Join(", ", matches) + " tercihlerinle eşleşiyor.";
    }

    private static string ToSlug(string value) => value.ToLowerInvariant()
        .Replace('ı', 'i').Replace('ş', 's').Replace('ç', 'c').Replace('ö', 'o').Replace('ü', 'u')
        .Replace(' ', '-');
}
