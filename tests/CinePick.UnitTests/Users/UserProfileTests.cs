using CinePick.Domain.Users;

namespace CinePick.UnitTests.Users;

public sealed class UserProfileTests
{
    [Fact]
    public void MovieStateAcceptsOnlyRatingsFromOneToTen()
    {
        var state = new UserMovieState(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.Update(true, true, 11, DateTimeOffset.UtcNow));
        state.Update(true, true, 8, DateTimeOffset.UtcNow);

        Assert.True(state.IsFavorite);
        Assert.True(state.IsWatched);
        Assert.Equal(8, state.Rating);
    }

    [Fact]
    public void PreferencesNormalizeTextAndValidateLimits()
    {
        var preferences = new UserPreference(Guid.NewGuid());

        preferences.Update("  Bilim-Kurgu ", " TR ", 120, 25);

        Assert.Equal("bilim-kurgu", preferences.PreferredGenreSlug);
        Assert.Equal("tr", preferences.PreferredLanguage);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            preferences.Update(null, null, 0, null));
    }
}
