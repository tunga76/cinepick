namespace CinePick.Application.Recommendations;

public sealed record RecommendationFilter(
    DateTimeOffset StartsFrom,
    DateTimeOffset StartsBefore,
    int? MaximumRuntimeMinutes,
    string? GenreSlug,
    string? CitySlug,
    string? DistrictSlug,
    decimal? MaximumPrice,
    string? Language,
    string? Format);
