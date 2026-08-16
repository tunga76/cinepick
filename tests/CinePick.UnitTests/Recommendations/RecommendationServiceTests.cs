using CinePick.Application.Recommendations;

namespace CinePick.UnitTests.Recommendations;

public sealed class RecommendationServiceTests
{
    [Fact]
    public async Task RankingIsDeterministicAndLimitedToThreeItems()
    {
        var filter = new RecommendationFilter(
            new DateTimeOffset(2026, 8, 15, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero),
            120, null, null, "kadikoy", null, null, null);
        var candidates = Enumerable.Range(1, 5).Select(index => new RecommendationCandidate(
            Guid.Parse($"20000000-0000-0000-0000-{index:D12}"),
            Guid.Parse($"34000000-0000-0000-0000-{index:D12}"), $"Film {index}",
            "Sinema", "Kadıköy", "Salon 1", filter.StartsFrom.AddMinutes(index * 30),
            filter.StartsFrom.AddMinutes(100 + index * 30), 100, 250, "TRY", "tr", "2D",
            7 + (index / 10m), 50 + index, ["Dram"], "https://tickets.example.invalid/test"))
            .ToArray();
        var service = new RecommendationService(new StubParser(filter), new StubQuery(candidates),
            new ThrowingRanker(), new RecordingAuditStore(), TimeProvider.System);

        var first = await service.RecommendAsync("test", CancellationToken.None);
        var second = await service.RecommendAsync("test", CancellationToken.None);

        Assert.Equal(3, first.Items.Count);
        Assert.Equal(first.Items.Select(item => item.ShowtimeId),
            second.Items.Select(item => item.ShowtimeId));
        Assert.Equal("deterministic-fallback", first.Method);
    }

    [Fact]
    public async Task HallucinatedIdentifierTriggersDeterministicFallback()
    {
        var filter = new RecommendationFilter(DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1), null, null, null, null, null, null, null);
        var candidate = new RecommendationCandidate(Guid.NewGuid(), Guid.NewGuid(), "Film",
            "Sinema", "Kadıköy", "Salon", filter.StartsFrom.AddHours(1),
            filter.StartsFrom.AddHours(3), 120, 200, "TRY", "tr", "2D", 8, 50,
            ["Dram"], "https://tickets.example.invalid/test");
        var service = new RecommendationService(new StubParser(filter),
            new StubQuery([candidate]), new HallucinatingRanker(), new RecordingAuditStore(),
            TimeProvider.System);

        var result = await service.RecommendAsync("test", CancellationToken.None);

        Assert.Equal("deterministic-fallback", result.Method);
        Assert.Single(result.Items);
        Assert.Equal(candidate.ShowtimeId, result.Items[0].ShowtimeId);
    }

    [Fact]
    public async Task RankerTimeoutTriggersDeterministicFallback()
    {
        var filter = new RecommendationFilter(DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1), null, null, null, null, null, null, null);
        var candidate = new RecommendationCandidate(Guid.NewGuid(), Guid.NewGuid(), "Film",
            "Sinema", "Kadıköy", "Salon", filter.StartsFrom.AddHours(1),
            filter.StartsFrom.AddHours(3), 120, 200, "TRY", "tr", "2D", 8, 50,
            ["Dram"], "https://tickets.example.invalid/test");
        var service = new RecommendationService(new StubParser(filter),
            new StubQuery([candidate]), new NeverCompletingRanker(), new RecordingAuditStore(),
            TimeProvider.System);

        var result = await service.RecommendAsync("test", CancellationToken.None);

        Assert.Equal("deterministic-fallback", result.Method);
        Assert.Single(result.Items);
    }

    private sealed class StubParser(RecommendationFilter filter) : IRecommendationRequestParser
    {
        public RecommendationFilter Parse(string text) => filter;
    }

    private sealed class StubQuery(IReadOnlyList<RecommendationCandidate> candidates)
        : IRecommendationCandidateQuery
    {
        public Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(
            RecommendationFilter filter, Guid? userId, CancellationToken cancellationToken) =>
            Task.FromResult(candidates);
    }

    private sealed class ThrowingRanker : IRecommendationRanker
    {
        public string Method => "broken";
        public Task<IReadOnlyList<RankedRecommendation>> RankAsync(string requestText,
            RecommendationFilter filter, IReadOnlyList<RecommendationCandidate> candidates,
            CancellationToken cancellationToken) => throw new InvalidOperationException("test");
    }

    private sealed class HallucinatingRanker : IRecommendationRanker
    {
        public string Method => "broken";
        public Task<IReadOnlyList<RankedRecommendation>> RankAsync(string requestText,
            RecommendationFilter filter, IReadOnlyList<RecommendationCandidate> candidates,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RankedRecommendation>>(
                [new RankedRecommendation(Guid.NewGuid(), Guid.NewGuid(), 99, "uydurma")]);
    }

    private sealed class NeverCompletingRanker : IRecommendationRanker
    {
        public string Method => "timeout";
        public Task<IReadOnlyList<RankedRecommendation>> RankAsync(string requestText,
            RecommendationFilter filter, IReadOnlyList<RecommendationCandidate> candidates,
            CancellationToken cancellationToken) =>
            new TaskCompletionSource<IReadOnlyList<RankedRecommendation>>().Task;
    }

    private sealed class RecordingAuditStore : IRecommendationAuditStore
    {
        public Task SaveAsync(RecommendationAuditEntry entry, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
