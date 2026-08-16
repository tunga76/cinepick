using System.Security.Claims;
using CinePick.Api.Filters;
using CinePick.Application.Users;

namespace CinePick.Api.Endpoints;

internal static class UserProfileEndpoints
{
    public static IEndpointRouteBuilder MapUserProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users/me").WithTags("User profile")
            .RequireAuthorization();

        group.MapGet("/preferences", async (ClaimsPrincipal principal,
            IUserProfileStore store, CancellationToken cancellationToken) =>
            TryGetUserId(principal, out var userId)
                ? Results.Ok(await store.GetPreferencesAsync(userId, cancellationToken))
                : Results.Unauthorized());

        group.MapPut("/preferences", async (UpdateUserPreferences request,
            ClaimsPrincipal principal, IUserProfileStore store,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var errors = Validate(request);
            return errors.Count > 0
                ? Results.ValidationProblem(errors)
                : Results.Ok(await store.UpdatePreferencesAsync(userId, request, cancellationToken));
        }).AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);

        group.MapGet("/movie-states", async (ClaimsPrincipal principal,
            IUserProfileStore store, CancellationToken cancellationToken) =>
            TryGetUserId(principal, out var userId)
                ? Results.Ok(await store.GetMovieStatesAsync(userId, cancellationToken))
                : Results.Unauthorized());

        group.MapGet("/movie-states/{movieId:guid}", async (Guid movieId,
            ClaimsPrincipal principal, IUserProfileStore store,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var state = await store.GetMovieStateAsync(userId, movieId, cancellationToken);
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPut("/movie-states/{movieId:guid}", async (Guid movieId,
            UpdateUserMovieState request, ClaimsPrincipal principal, IUserProfileStore store,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            if (request.Rating is < 1 or > 10)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["rating"] = ["Puan 1 ile 10 arasında olmalıdır."] });
            var state = await store.UpdateMovieStateAsync(userId, movieId, request,
                cancellationToken);
            return state is null ? Results.NotFound() : Results.Ok(state);
        }).AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);

        return endpoints;
    }

    private static Dictionary<string, string[]> Validate(UpdateUserPreferences request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.MaximumRuntimeMinutes is <= 0 or > 600)
            errors["maximumRuntimeMinutes"] = ["Süre 1 ile 600 dakika arasında olmalıdır."];
        if (request.MaximumDistanceKilometers is <= 0 or > 100)
            errors["maximumDistanceKilometers"] = ["Mesafe 0 ile 100 km arasında olmalıdır."];
        if (request.PreferredGenreSlug?.Length > 100)
            errors["preferredGenreSlug"] = ["Tür değeri en fazla 100 karakter olabilir."];
        if (request.PreferredLanguage?.Length > 10)
            errors["preferredLanguage"] = ["Dil değeri en fazla 10 karakter olabilir."];
        return errors;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
