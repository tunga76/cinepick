namespace CinePick.Application.Movies.GetMovies;

public sealed record MovieListItem(
    Guid Id,
    string Title,
    string Overview,
    DateOnly ReleaseDate,
    int RuntimeMinutes,
    string OriginalLanguage,
    int AgeRating,
    string? PosterPath,
    decimal VoteAverage,
    decimal Popularity,
    IReadOnlyList<string> Genres);
