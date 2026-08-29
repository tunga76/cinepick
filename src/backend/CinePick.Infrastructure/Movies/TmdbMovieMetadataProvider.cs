using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CinePick.Application.Movies.SyncCatalog;
using CinePick.Domain.Movies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinePick.Infrastructure.Movies;

internal sealed class TmdbMovieMetadataProvider(
    HttpClient httpClient,
    IOptions<TmdbOptions> options,
    ILogger<TmdbMovieMetadataProvider> logger) : IMovieMetadataProvider
{
    private static readonly Action<ILogger, int, Exception?> LogSkippedCatalogRecords =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(2101, "TmdbSkippedCatalogRecords"),
            "TMDb catalog skipped {SkippedCount} invalid movie records.");

    private static readonly Action<ILogger, int, Exception?> LogInvalidMovie =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(2102, "TmdbInvalidMovie"),
            "TMDb movie {MovieId} has no usable title, release date, or runtime.");

    private static readonly IReadOnlyDictionary<int, string> GenreSlugs =
        new Dictionary<int, string>
        {
            [28] = "aksiyon",
            [16] = "animasyon",
            [35] = "komedi",
            [18] = "dram",
            [10751] = "aile",
            [878] = "bilim-kurgu",
            [53] = "gerilim",
            [10749] = "romantik",
        };

    private readonly TmdbOptions _options = options.Value;

    public string ProviderId => "tmdb";

    public async Task<IReadOnlyList<MovieMetadataItem>> GetCatalogAsync(
        CancellationToken cancellationToken)
    {
        var nowPlaying = await GetMovieIdsAsync("movie/now_playing", cancellationToken);
        var upcoming = await GetMovieIdsAsync("movie/upcoming", cancellationToken);
        var allIds = nowPlaying.Concat(upcoming).Distinct().ToArray();

        using var concurrency = new SemaphoreSlim(4);
        var tasks = allIds.Select(async id =>
        {
            await concurrency.WaitAsync(cancellationToken);
            try
            {
                var details = await GetAsync<TmdbMovieDetails>(
                    $"movie/{id}?language={Uri.EscapeDataString(_options.Language)}&append_to_response=release_dates",
                    cancellationToken);
                return Map(details, nowPlaying.Contains(id), upcoming.Contains(id));
            }
            finally
            {
                concurrency.Release();
            }
        });

        var mapped = await Task.WhenAll(tasks);
        var skipped = mapped.Count(item => item is null);
        if (skipped > 0)
        {
            LogSkippedCatalogRecords(logger, skipped, null);
        }

        return mapped.OfType<MovieMetadataItem>().ToArray();
    }

    private async Task<HashSet<int>> GetMovieIdsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<int>();
        var page = 1;
        var totalPages = 1;
        var maximumPages = Math.Clamp(_options.MaxPages, 1, 20);

        while (page <= totalPages && page <= maximumPages)
        {
            var response = await GetAsync<TmdbMovieListResponse>(
                $"{path}?language={Uri.EscapeDataString(_options.Language)}&region={Uri.EscapeDataString(_options.Region)}&page={page}",
                cancellationToken);
            foreach (var item in response.Results)
            {
                ids.Add(item.Id);
            }

            totalPages = Math.Max(1, response.TotalPages);
            page++;
        }

        return ids;
    }

    private async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException("TMDb rejected the configured read access token.", null,
                response.StatusCode);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("TMDb request limit was reached. Retry later.", null,
                response.StatusCode);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("TMDb returned an empty JSON response.");
    }

    private MovieMetadataItem? Map(TmdbMovieDetails details, bool isNowPlaying, bool isUpcoming)
    {
        if (details.Runtime is null or <= 0
            || string.IsNullOrWhiteSpace(details.Title)
            || !DateOnly.TryParseExact(details.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var releaseDate))
        {
            LogInvalidMovie(logger, details.Id, null);
            return null;
        }

        var genres = (details.Genres ?? [])
            .Select(genre => GenreSlugs.GetValueOrDefault(genre.Id))
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MovieMetadataItem(
            details.Id.ToString(CultureInfo.InvariantCulture),
            details.Title,
            string.IsNullOrWhiteSpace(details.OriginalTitle) ? details.Title : details.OriginalTitle,
            details.Overview ?? string.Empty,
            releaseDate,
            details.Runtime.Value,
            string.IsNullOrWhiteSpace(details.OriginalLanguage) ? "und" : details.OriginalLanguage,
            MapAgeRating(details.ReleaseDates),
            details.VoteAverage,
            details.VoteCount,
            details.Popularity,
            isNowPlaying,
            isUpcoming,
            genres,
            NormalizeImagePath(details.PosterPath),
            NormalizeImagePath(details.BackdropPath));
    }

    private AgeRating MapAgeRating(TmdbReleaseDatesResponse? releaseDates)
    {
        var ratings = releaseDates?.Results
            .Where(item => string.Equals(item.CountryCode, _options.Region,
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.ReleaseDates)
            .Where(item => item.Type is 2 or 3)
            .Select(item => ParseCertification(item.Certification))
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToArray();

        // Unknown must not accidentally weaken an age restriction.
        return ratings is { Length: > 0 } ? ratings.Max() : AgeRating.Age18;
    }

    private static AgeRating? ParseCertification(string? certification)
    {
        if (string.IsNullOrWhiteSpace(certification)) return null;

        var normalized = RemoveDiacritics(certification).ToUpperInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("+", string.Empty, StringComparison.Ordinal);

        if (normalized.Contains("GENELIZLEYICI", StringComparison.Ordinal))
            return AgeRating.GeneralAudience;
        if (normalized.StartsWith("18", StringComparison.Ordinal)) return AgeRating.Age18;
        if (normalized.StartsWith("16", StringComparison.Ordinal)) return AgeRating.Age16;
        if (normalized.StartsWith("13", StringComparison.Ordinal)) return AgeRating.Age13;
        if (normalized.StartsWith("10", StringComparison.Ordinal)) return AgeRating.Age10;
        if (normalized.StartsWith('6')) return AgeRating.Age6;
        return null;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Replace('ı', 'i').Replace('İ', 'I')
            .Normalize(NormalizationForm.FormD);
        var characters = normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);
        return new string(characters.ToArray()).Normalize(NormalizationForm.FormC);
    }

    private static string? NormalizeImagePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && path.StartsWith('/')
            ? path
            : null;
}
