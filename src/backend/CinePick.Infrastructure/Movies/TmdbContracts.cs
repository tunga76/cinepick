using System.Text.Json.Serialization;

namespace CinePick.Infrastructure.Movies;

internal sealed record TmdbMovieListResponse(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("total_pages")] int TotalPages,
    [property: JsonPropertyName("results")] IReadOnlyList<TmdbMovieListItem> Results);

internal sealed record TmdbMovieListItem([property: JsonPropertyName("id")] int Id);

internal sealed record TmdbMovieDetails(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("original_title")] string? OriginalTitle,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("runtime")] int? Runtime,
    [property: JsonPropertyName("original_language")] string? OriginalLanguage,
    [property: JsonPropertyName("vote_average")] decimal VoteAverage,
    [property: JsonPropertyName("vote_count")] int VoteCount,
    [property: JsonPropertyName("popularity")] decimal Popularity,
    [property: JsonPropertyName("genres")] IReadOnlyList<TmdbGenre>? Genres,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("release_dates")] TmdbReleaseDatesResponse? ReleaseDates);

internal sealed record TmdbGenre([property: JsonPropertyName("id")] int Id);

internal sealed record TmdbReleaseDatesResponse(
    [property: JsonPropertyName("results")] IReadOnlyList<TmdbCountryReleaseDates> Results);

internal sealed record TmdbCountryReleaseDates(
    [property: JsonPropertyName("iso_3166_1")] string CountryCode,
    [property: JsonPropertyName("release_dates")] IReadOnlyList<TmdbReleaseDate> ReleaseDates);

internal sealed record TmdbReleaseDate(
    [property: JsonPropertyName("certification")] string? Certification,
    [property: JsonPropertyName("type")] int Type);
