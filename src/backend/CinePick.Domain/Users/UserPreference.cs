namespace CinePick.Domain.Users;

public sealed class UserPreference
{
    private UserPreference() { }

    public UserPreference(Guid userId) => UserId = userId;

    public Guid UserId { get; private set; }
    public string? PreferredGenreSlug { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public int? MaximumRuntimeMinutes { get; private set; }
    public decimal? MaximumDistanceKilometers { get; private set; }

    public void Update(string? preferredGenreSlug, string? preferredLanguage,
        int? maximumRuntimeMinutes, decimal? maximumDistanceKilometers)
    {
        if (maximumRuntimeMinutes is <= 0 or > 600)
            throw new ArgumentOutOfRangeException(nameof(maximumRuntimeMinutes));
        if (maximumDistanceKilometers is <= 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maximumDistanceKilometers));
        PreferredGenreSlug = Normalize(preferredGenreSlug);
        PreferredLanguage = Normalize(preferredLanguage);
        MaximumRuntimeMinutes = maximumRuntimeMinutes;
        MaximumDistanceKilometers = maximumDistanceKilometers;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
