using CinePick.Application.Movies.SyncCatalog;
using CinePick.Application.Cinemas.SyncShowtimes;
using CinePick.Application.Administration;

namespace CinePick.Api.Endpoints;

internal static class DevelopmentCatalogSyncEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentCatalogSyncEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/development/movie-catalog-syncs", async (
            IMovieCatalogSynchronizer synchronizer,
            CancellationToken cancellationToken) =>
                Results.Ok(await synchronizer.SynchronizeAsync(cancellationToken)))
            .WithName("SynchronizeDevelopmentMovieCatalog")
            .WithTags("Development");
        endpoints.MapPost("/api/development/showtime-catalog-syncs", async (
            IShowtimeCatalogSynchronizer synchronizer,
            CancellationToken cancellationToken) =>
                Results.Ok(await synchronizer.SynchronizeAsync(cancellationToken)))
            .WithName("SynchronizeDevelopmentShowtimeCatalog")
            .WithTags("Development");
        endpoints.MapGet("/api/development/sync-logs", async (
            IDevelopmentOperations operations, CancellationToken cancellationToken) =>
                Results.Ok(await operations.GetRecentSyncLogsAsync(cancellationToken)))
            .WithTags("Development");
        endpoints.MapGet("/api/development/showtimes", async (
            IDevelopmentOperations operations, CancellationToken cancellationToken) =>
                Results.Ok(await operations.GetShowtimesAsync(cancellationToken)))
            .WithTags("Development");
        endpoints.MapPut("/api/development/showtimes/{id:guid}/cancellation", async (
            Guid id, ShowtimeCancellationRequest request, IDevelopmentOperations operations,
            CancellationToken cancellationToken) =>
                await operations.SetShowtimeCancellationAsync(id, request.IsCancelled, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound())
            .WithTags("Development");
        return endpoints;
    }

    private sealed record ShowtimeCancellationRequest(bool IsCancelled);
}
