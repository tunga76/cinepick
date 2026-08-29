using CinePick.Application.Common;
using CinePick.Application.Movies;
using CinePick.Application.Movies.GetMovieDetail;
using CinePick.Application.Movies.GetMovies;
using CinePick.Application.Movies.GetGenres;
using CinePick.Domain.Movies;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Movies;

internal sealed class MovieCatalogQuery(
    IDbContextFactory<CinePickDbContext> dbContextFactory) : IMovieCatalogQuery
{
    public Task<PagedResponse<MovieListItem>> GetNowPlayingAsync(
        MovieListQuery query,
        CancellationToken cancellationToken) => GetMoviesAsync(query, true, cancellationToken);

    public Task<PagedResponse<MovieListItem>> GetUpcomingAsync(
        MovieListQuery query,
        CancellationToken cancellationToken) => GetMoviesAsync(query, false, cancellationToken);

    public async Task<MovieDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => new MovieDetail(
                movie.Id,
                movie.Title,
                movie.OriginalTitle,
                movie.Overview,
                movie.ReleaseDate,
                movie.RuntimeMinutes,
                movie.OriginalLanguage,
                (int)movie.AgeRating,
                movie.PosterPath,
                movie.BackdropPath,
                movie.VoteAverage,
                movie.VoteCount,
                movie.MovieGenres.OrderBy(movieGenre => movieGenre.Genre.Name)
                    .Select(movieGenre => movieGenre.Genre.Name)
                    .ToArray(),
                movie.IsNowPlaying,
                movie.IsUpcoming))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GenreListItem>> GetGenresAsync(
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Genres
            .AsNoTracking()
            .OrderBy(genre => genre.Name)
            .Select(genre => new GenreListItem(genre.Id, genre.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task<PagedResponse<MovieListItem>> GetMoviesAsync(
        MovieListQuery request,
        bool nowPlaying,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Movie> query = dbContext.Movies.AsNoTracking();
        query = nowPlaying
            ? query.Where(movie => movie.IsNowPlaying)
            : query.Where(movie => movie.IsUpcoming);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(movie => movie.Title.Contains(search));
        }

        if (request.GenreId is not null)
        {
            query = query.Where(movie => movie.MovieGenres.Any(
                movieGenre => movieGenre.GenreId == request.GenreId));
        }

        if (request.MaximumRuntimeMinutes is > 0)
        {
            query = query.Where(movie => movie.RuntimeMinutes <= request.MaximumRuntimeMinutes);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = nowPlaying
            ? query.OrderByDescending(movie => movie.Popularity).ThenBy(movie => movie.Title)
            : query.OrderBy(movie => movie.ReleaseDate).ThenByDescending(movie => movie.Popularity);
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(movie => new MovieListItem(
                movie.Id,
                movie.Title,
                movie.Overview,
                movie.ReleaseDate,
                movie.RuntimeMinutes,
                movie.OriginalLanguage,
                (int)movie.AgeRating,
                movie.PosterPath,
                movie.VoteAverage,
                movie.Popularity,
                movie.MovieGenres.OrderBy(movieGenre => movieGenre.Genre.Name)
                    .Select(movieGenre => movieGenre.Genre.Name)
                    .ToArray()))
            .ToListAsync(cancellationToken);

        return new PagedResponse<MovieListItem>(items, page, pageSize, totalCount);
    }
}
