using CinePick.Api.Filters;
using CinePick.Application.Administration;
using CinePick.Application.Cinemas.SyncShowtimes;
using CinePick.Application.Movies.SyncCatalog;

namespace CinePick.Api.Endpoints;

internal static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin").WithTags("Administration")
            .RequireAuthorization("Admin").RequireRateLimiting("admin");
        group.MapPost("/movie-catalog-syncs", async (IMovieCatalogSynchronizer synchronizer,
            CancellationToken cancellationToken) =>
                Results.Ok(await synchronizer.SynchronizeAsync(cancellationToken)))
            .AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);
        group.MapPost("/showtime-catalog-syncs", async (IShowtimeCatalogSynchronizer synchronizer,
            CancellationToken cancellationToken) =>
                Results.Ok(await synchronizer.SynchronizeAsync(cancellationToken)))
            .AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);
        group.MapGet("/sync-logs", async (IDevelopmentOperations operations,
            CancellationToken cancellationToken) =>
                Results.Ok(await operations.GetRecentSyncLogsAsync(cancellationToken)));
        group.MapGet("/showtimes", async (IDevelopmentOperations operations,
            CancellationToken cancellationToken) =>
                Results.Ok(await operations.GetShowtimesAsync(cancellationToken)));
        group.MapPut("/showtimes/{id:guid}/cancellation", async (Guid id,
            ShowtimeCancellationRequest request, IDevelopmentOperations operations,
            CancellationToken cancellationToken) =>
                await operations.SetShowtimeCancellationAsync(id, request.IsCancelled,
                    cancellationToken) ? Results.NoContent() : Results.NotFound())
            .AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);
        return endpoints;
    }

    private sealed record ShowtimeCancellationRequest(bool IsCancelled);
}
