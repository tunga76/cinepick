using System.Security.Claims;
using CinePick.Infrastructure.Identity;
using CinePick.Api.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace CinePick.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication")
            .RequireRateLimiting("auth");

        group.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return TypedResults.Ok(new CsrfResponse(tokens.RequestToken!));
        }).AllowAnonymous();

        group.MapPost("/register", RegisterAsync).AllowAnonymous()
            .AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);
        group.MapPost("/login", LoginAsync).AllowAnonymous()
            .AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization()
            .AddEndpointFilter(AntiforgeryEndpointFilter.ValidateAsync);
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request,
        UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        var email = request.Email.Trim();
        var displayName = request.DisplayName.Trim();
        if (displayName.Length is < 2 or > 80)
            return ValidationProblem("displayName", "Görünen ad 2-80 karakter olmalıdır.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors
                .GroupBy(error => error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                    ? "password" : "email")
                .ToDictionary(group => group.Key,
                    group => group.Select(error => error.Description).ToArray()));
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return TypedResults.Ok(Map(user, []));
    }

    private static async Task<IResult> LoginAsync(LoginRequest request,
        UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Results.Unauthorized();
        var result = await signInManager.PasswordSignInAsync(user, request.Password,
            isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Results.Unauthorized();
        var roles = await userManager.GetRolesAsync(user);
        return TypedResults.Ok(Map(user, roles));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();
        return TypedResults.Ok(Map(user, await userManager.GetRolesAsync(user)));
    }

    private static IResult ValidationProblem(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static AuthenticatedUserResponse Map(ApplicationUser user,
        IEnumerable<string> roles) => new(user.Id, user.Email!, user.DisplayName, roles.ToArray());

    private sealed record RegisterRequest(string Email, string Password, string DisplayName);
    private sealed record LoginRequest(string Email, string Password);
    private sealed record CsrfResponse(string Token);
    private sealed record AuthenticatedUserResponse(Guid Id, string Email, string DisplayName,
        IReadOnlyList<string> Roles);
}
