using CinePick.Domain.Movies;

namespace CinePick.Application.Movies.SyncCatalog;

public sealed record MovieMetadataItem(
    string ExternalId,
    string Title,
    string OriginalTitle,
    string Overview,
    DateOnly ReleaseDate,
    int RuntimeMinutes,
    string OriginalLanguage,
    AgeRating AgeRating,
    decimal VoteAverage,
    int VoteCount,
    decimal Popularity,
    bool IsNowPlaying,
    bool IsUpcoming,
    IReadOnlyList<string> GenreSlugs,
    string? PosterPath = null,
    string? BackdropPath = null);
