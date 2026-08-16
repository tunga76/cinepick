using CinePick.Application.Recommendations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;

namespace CinePick.Api.Endpoints;

internal static class RecommendationEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/recommendations", async (RecommendationRequest request,
            ClaimsPrincipal principal, RecommendationService service,
            IAntiforgery antiforgery, HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 500)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["text"] = ["İstek metni 1–500 karakter arasında olmalıdır."],
                }, extensions: new Dictionary<string, object?> { ["errorCode"] = "validation.failed" });
            }
            if (principal.Identity?.IsAuthenticated == true)
            {
                try { await antiforgery.ValidateRequestAsync(httpContext); }
                catch (AntiforgeryValidationException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                        title: "Geçersiz CSRF belirteci.");
                }
            }
            Guid? userId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),
                out var parsedUserId) ? parsedUserId : null;
            return Results.Ok(await service.RecommendAsync(request.Text, cancellationToken, userId));
        }).WithTags("Recommendations").RequireRateLimiting("recommendations");

        endpoints.MapGet("/api/users/me/recommendation-history", async (
            ClaimsPrincipal principal, IRecommendationHistoryQuery history,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            return Results.Ok(await history.GetAsync(userId, cancellationToken));
        }).RequireAuthorization().WithTags("Recommendations");
        return endpoints;
    }

    private sealed record RecommendationRequest(string Text);
}
