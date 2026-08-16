using CinePick.Domain.Movies;

namespace CinePick.UnitTests.Movies;

public sealed class MovieTests
{
    [Fact]
    public void ConstructorRejectsNonPositiveRuntime()
    {
        var action = () => new Movie(
            Guid.NewGuid(),
            "mock",
            "movie-1",
            "Film",
            "Movie",
            "Özet",
            new DateOnly(2026, 8, 14),
            0,
            "tr",
            AgeRating.GeneralAudience,
            7.5m,
            100,
            20m,
            true,
            false,
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
