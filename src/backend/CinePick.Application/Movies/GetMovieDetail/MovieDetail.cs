namespace CinePick.Application.Movies.GetMovieDetail;

public sealed record MovieDetail(
    Guid Id,
    string Title,
    string OriginalTitle,
    string Overview,
    DateOnly ReleaseDate,
    int RuntimeMinutes,
    string OriginalLanguage,
    int AgeRating,
    string? PosterPath,
    string? BackdropPath,
    decimal VoteAverage,
    int VoteCount,
    IReadOnlyList<string> Genres,
    bool IsNowPlaying,
    bool IsUpcoming);
