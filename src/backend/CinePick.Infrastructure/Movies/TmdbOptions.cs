namespace CinePick.Infrastructure.Movies;

internal sealed class TmdbOptions
{
    public const string SectionName = "TMDb";

    public string BaseUrl { get; init; } = "https://api.themoviedb.org/3/";
    public string ReadAccessToken { get; init; } = string.Empty;
    public string Language { get; init; } = "tr-TR";
    public string Region { get; init; } = "TR";
    public int MaxPages { get; init; } = 2;
}
