using CinePick.Application.Common;
using CinePick.Application.Movies.GetMovieDetail;
using CinePick.Application.Movies.GetMovies;
using CinePick.Application.Movies.GetGenres;

namespace CinePick.Application.Movies;

public interface IMovieCatalogQuery
{
    Task<PagedResponse<MovieListItem>> GetNowPlayingAsync(
        MovieListQuery query,
        CancellationToken cancellationToken);

    Task<PagedResponse<MovieListItem>> GetUpcomingAsync(
        MovieListQuery query,
        CancellationToken cancellationToken);

    Task<MovieDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<GenreListItem>> GetGenresAsync(CancellationToken cancellationToken);
}
