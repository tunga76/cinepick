namespace CinePick.Domain.Movies;

public sealed class Movie
{
    private Movie()
    {
    }

    public Movie(
        Guid id,
        string externalProviderId,
        string externalMovieId,
        string title,
        string originalTitle,
        string overview,
        DateOnly releaseDate,
        int runtimeMinutes,
        string originalLanguage,
        AgeRating ageRating,
        decimal voteAverage,
        int voteCount,
        decimal popularity,
        bool isNowPlaying,
        bool isUpcoming,
        DateTimeOffset lastSynchronizedAt,
        string? posterPath = null,
        string? backdropPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalProviderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalMovieId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runtimeMinutes);

        Id = id;
        ExternalProviderId = externalProviderId;
        ExternalMovieId = externalMovieId;
        Title = title;
        OriginalTitle = originalTitle;
        Overview = overview;
        ReleaseDate = releaseDate;
        RuntimeMinutes = runtimeMinutes;
        OriginalLanguage = originalLanguage;
        AgeRating = ageRating;
        VoteAverage = voteAverage;
        VoteCount = voteCount;
        Popularity = popularity;
        IsNowPlaying = isNowPlaying;
        IsUpcoming = isUpcoming;
        LastSynchronizedAt = lastSynchronizedAt;
        PosterPath = posterPath;
        BackdropPath = backdropPath;
    }

    public Guid Id { get; private set; }
    public string ExternalProviderId { get; private set; } = string.Empty;
    public string ExternalMovieId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string OriginalTitle { get; private set; } = string.Empty;
    public string Overview { get; private set; } = string.Empty;
    public DateOnly ReleaseDate { get; private set; }
    public int RuntimeMinutes { get; private set; }
    public string OriginalLanguage { get; private set; } = string.Empty;
    public AgeRating AgeRating { get; private set; }
    public string? PosterPath { get; private set; }
    public string? BackdropPath { get; private set; }
    public decimal VoteAverage { get; private set; }
    public int VoteCount { get; private set; }
    public decimal Popularity { get; private set; }
    public bool IsNowPlaying { get; private set; }
    public bool IsUpcoming { get; private set; }
    public DateTimeOffset LastSynchronizedAt { get; private set; }
    public ICollection<MovieGenre> MovieGenres { get; } = new List<MovieGenre>();

    public void UpdateMetadata(
        string title,
        string originalTitle,
        string overview,
        DateOnly releaseDate,
        int runtimeMinutes,
        string originalLanguage,
        AgeRating ageRating,
        decimal voteAverage,
        int voteCount,
        decimal popularity,
        bool isNowPlaying,
        bool isUpcoming,
        DateTimeOffset synchronizedAt,
        string? posterPath = null,
        string? backdropPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runtimeMinutes);
        Title = title;
        OriginalTitle = originalTitle;
        Overview = overview;
        ReleaseDate = releaseDate;
        RuntimeMinutes = runtimeMinutes;
        OriginalLanguage = originalLanguage;
        AgeRating = ageRating;
        VoteAverage = voteAverage;
        VoteCount = voteCount;
        Popularity = popularity;
        IsNowPlaying = isNowPlaying;
        IsUpcoming = isUpcoming;
        LastSynchronizedAt = synchronizedAt;
        PosterPath = posterPath;
        BackdropPath = backdropPath;
    }

    public void UpdateAvailability(bool isNowPlaying, bool isUpcoming, DateTimeOffset synchronizedAt)
    {
        IsNowPlaying = isNowPlaying;
        IsUpcoming = isUpcoming;
        LastSynchronizedAt = synchronizedAt;
    }
}
