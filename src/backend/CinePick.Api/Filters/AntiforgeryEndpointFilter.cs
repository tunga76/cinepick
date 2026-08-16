using Microsoft.AspNetCore.Antiforgery;

namespace CinePick.Api.Filters;

internal static class AntiforgeryEndpointFilter
{
    public static async ValueTask<object?> ValidateAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Geçersiz CSRF belirteci.");
        }
        return await next(context);
    }
}
