namespace CinePick.Application.Movies.GetMovies;

public sealed record MovieListQuery(
    int Page = 1,
    int PageSize = 12,
    string? Search = null,
    Guid? GenreId = null,
    int? MaximumRuntimeMinutes = null);
