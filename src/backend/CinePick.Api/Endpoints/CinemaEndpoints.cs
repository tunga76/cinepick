using CinePick.Application.Cinemas;

namespace CinePick.Api.Endpoints;

internal static class CinemaEndpoints
{
    public static IEndpointRouteBuilder MapCinemaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/cities", async (ICinemaCatalogQuery catalog, CancellationToken token) =>
            Results.Ok(await catalog.GetCitiesAsync(token))).WithTags("Cinemas");

        var cinemas = endpoints.MapGroup("/api/cinemas").WithTags("Cinemas");
        cinemas.MapGet("/", async (Guid? cityId, double? latitude, double? longitude,
            double? radiusKilometers, ICinemaCatalogQuery catalog, CancellationToken token) =>
        {
            var hasLatitude = latitude is not null;
            var hasLongitude = longitude is not null;
            if (hasLatitude != hasLongitude || latitude is < -90 or > 90 || longitude is < -180 or > 180
                || radiusKilometers is <= 0 or > 100)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["location"] = ["Geçerli latitude/longitude birlikte verilmeli; yarıçap 0–100 km olmalıdır."],
                });
            }
            return Results.Ok(await catalog.GetCinemasAsync(cityId, latitude, longitude,
                hasLatitude ? radiusKilometers ?? 25 : null, token));
        });
        cinemas.MapGet("/{id:guid}", async (Guid id, ICinemaCatalogQuery catalog, CancellationToken token) =>
        {
            var cinema = await catalog.GetCinemaAsync(id, token);
            return cinema is null
                ? Results.Problem(statusCode: 404, title: "Sinema bulunamadı.",
                    extensions: new Dictionary<string, object?> { ["errorCode"] = "cinemas.not_found" })
                : Results.Ok(cinema);
        });

        endpoints.MapGet("/api/showtimes", async (Guid? cinemaId, Guid? movieId,
            DateTimeOffset? from, DateTimeOffset? to, ICinemaCatalogQuery catalog,
            CancellationToken token) =>
        {
            var effectiveFrom = from ?? DateTimeOffset.UtcNow;
            var effectiveTo = to ?? effectiveFrom.AddDays(7);
            if (effectiveTo <= effectiveFrom || effectiveTo - effectiveFrom > TimeSpan.FromDays(8))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["to"] = ["Bitiş zamanı başlangıçtan sonra ve en fazla 8 gün içinde olmalıdır."],
                });
            }
            return Results.Ok(await catalog.GetShowtimesAsync(cinemaId, movieId,
                effectiveFrom, effectiveTo, token));
        }).WithTags("Showtimes");

        return endpoints;
    }
}
