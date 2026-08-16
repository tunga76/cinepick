using CinePick.Application.Users;
using CinePick.Domain.Users;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Users;

internal sealed class UserProfileStore(
    IDbContextFactory<CinePickDbContext> dbContextFactory, TimeProvider timeProvider)
    : IUserProfileStore
{
    public async Task<UserPreferencesDto> GetPreferencesAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserPreferences.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => new UserPreferencesDto(item.PreferredGenreSlug,
                item.PreferredLanguage, item.MaximumRuntimeMinutes,
                item.MaximumDistanceKilometers)).SingleOrDefaultAsync(cancellationToken)
            ?? new UserPreferencesDto(null, null, null, null);
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(Guid userId,
        UpdateUserPreferences command, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var preferences = await db.UserPreferences.SingleOrDefaultAsync(
            item => item.UserId == userId, cancellationToken) ?? new UserPreference(userId);
        if (db.Entry(preferences).State == EntityState.Detached) db.UserPreferences.Add(preferences);
        preferences.Update(command.PreferredGenreSlug, command.PreferredLanguage,
            command.MaximumRuntimeMinutes, command.MaximumDistanceKilometers);
        await db.SaveChangesAsync(cancellationToken);
        return new UserPreferencesDto(preferences.PreferredGenreSlug,
            preferences.PreferredLanguage, preferences.MaximumRuntimeMinutes,
            preferences.MaximumDistanceKilometers);
    }

    public async Task<IReadOnlyList<UserMovieStateDto>> GetMovieStatesAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserMovieStates.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .Join(db.Movies, state => state.MovieId, movie => movie.Id,
                (state, movie) => new UserMovieStateDto(state.MovieId, movie.Title,
                    state.IsFavorite, state.IsWatched, state.Rating, state.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<UserMovieStateDto?> GetMovieStateAsync(Guid userId, Guid movieId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserMovieStates.AsNoTracking()
            .Where(item => item.UserId == userId && item.MovieId == movieId)
            .Join(db.Movies, state => state.MovieId, movie => movie.Id,
                (state, movie) => new UserMovieStateDto(state.MovieId, movie.Title,
                    state.IsFavorite, state.IsWatched, state.Rating, state.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserMovieStateDto?> UpdateMovieStateAsync(Guid userId, Guid movieId,
        UpdateUserMovieState command, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var title = await db.Movies.AsNoTracking().Where(item => item.Id == movieId)
            .Select(item => item.Title).SingleOrDefaultAsync(cancellationToken);
        if (title is null) return null;
        var state = await db.UserMovieStates.SingleOrDefaultAsync(
            item => item.UserId == userId && item.MovieId == movieId, cancellationToken)
            ?? new UserMovieState(userId, movieId, timeProvider.GetUtcNow());
        if (db.Entry(state).State == EntityState.Detached) db.UserMovieStates.Add(state);
        state.Update(command.IsFavorite, command.IsWatched, command.Rating,
            timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return new UserMovieStateDto(movieId, title, state.IsFavorite, state.IsWatched,
            state.Rating, state.UpdatedAt);
    }
}
