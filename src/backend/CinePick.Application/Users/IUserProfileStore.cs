namespace CinePick.Application.Users;

public interface IUserProfileStore
{
    Task<UserPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateUserPreferences command,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<UserMovieStateDto>> GetMovieStatesAsync(Guid userId,
        CancellationToken cancellationToken);
    Task<UserMovieStateDto?> GetMovieStateAsync(Guid userId, Guid movieId,
        CancellationToken cancellationToken);
    Task<UserMovieStateDto?> UpdateMovieStateAsync(Guid userId, Guid movieId,
        UpdateUserMovieState command, CancellationToken cancellationToken);
}

public sealed record UserPreferencesDto(string? PreferredGenreSlug, string? PreferredLanguage,
    int? MaximumRuntimeMinutes, decimal? MaximumDistanceKilometers);
public sealed record UpdateUserPreferences(string? PreferredGenreSlug, string? PreferredLanguage,
    int? MaximumRuntimeMinutes, decimal? MaximumDistanceKilometers);
public sealed record UserMovieStateDto(Guid MovieId, string MovieTitle, bool IsFavorite,
    bool IsWatched, int? Rating, DateTimeOffset UpdatedAt);
public sealed record UpdateUserMovieState(bool IsFavorite, bool IsWatched, int? Rating);
