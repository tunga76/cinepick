using CinePick.Application.Movies;
using CinePick.Application.Movies.GetMovies;

namespace CinePick.Api.Endpoints;

internal static class MovieEndpoints
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/movies").WithTags("Movies");

        endpoints.MapGet("/api/genres", async (
            IMovieCatalogQuery catalog,
            CancellationToken cancellationToken) =>
                Results.Ok(await catalog.GetGenresAsync(cancellationToken)))
            .WithName("GetGenres")
            .WithTags("Genres");

        group.MapGet("/now-playing", async (
            int? page,
            int? pageSize,
            string? search,
            Guid? genreId,
            int? maximumRuntimeMinutes,
            IMovieCatalogQuery catalog,
            CancellationToken cancellationToken) =>
        {
            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 12;
            var validationResult = Validate(effectivePage, effectivePageSize, maximumRuntimeMinutes);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await catalog.GetNowPlayingAsync(
                new MovieListQuery(effectivePage, effectivePageSize, search, genreId, maximumRuntimeMinutes),
                cancellationToken);
            return Results.Ok(result);
        }).WithName("GetNowPlayingMovies");

        group.MapGet("/upcoming", async (
            int? page,
            int? pageSize,
            string? search,
            Guid? genreId,
            int? maximumRuntimeMinutes,
            IMovieCatalogQuery catalog,
            CancellationToken cancellationToken) =>
        {
            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 12;
            var validationResult = Validate(effectivePage, effectivePageSize, maximumRuntimeMinutes);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await catalog.GetUpcomingAsync(
                new MovieListQuery(effectivePage, effectivePageSize, search, genreId, maximumRuntimeMinutes),
                cancellationToken);
            return Results.Ok(result);
        }).WithName("GetUpcomingMovies");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMovieCatalogQuery catalog,
            CancellationToken cancellationToken) =>
        {
            var movie = await catalog.GetByIdAsync(id, cancellationToken);
            return movie is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Film bulunamadı.",
                    extensions: new Dictionary<string, object?> { ["errorCode"] = "movies.not_found" })
                : Results.Ok(movie);
        }).WithName("GetMovieById");

        return endpoints;
    }

    private static IResult? Validate(int page, int pageSize, int? maximumRuntimeMinutes)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["Sayfa numarası en az 1 olmalıdır."];
        }

        if (pageSize is < 1 or > 50)
        {
            errors["pageSize"] = ["Sayfa boyutu 1 ile 50 arasında olmalıdır."];
        }

        if (maximumRuntimeMinutes is <= 0)
        {
            errors["maximumRuntimeMinutes"] = ["Maksimum süre pozitif olmalıdır."];
        }

        return errors.Count == 0
            ? null
            : Results.ValidationProblem(errors, extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "validation.failed",
            });
    }
}
