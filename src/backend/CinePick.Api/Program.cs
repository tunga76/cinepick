using CinePick.Api.Contracts;
using CinePick.Api.Endpoints;
using CinePick.Api.Observability;
using CinePick.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CinePick.Api")
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddOpenApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => FixedWindow(context, 20, TimeSpan.FromMinutes(1)));
    options.AddPolicy("recommendations", context => FixedWindow(context, 30, TimeSpan.FromMinutes(1)));
    options.AddPolicy("admin", context => FixedWindow(context, 60, TimeSpan.FromMinutes(1)));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CinePick.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCinePickObservability(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:Initialize"))
{
    await app.Services.InitializeCinePickDatabaseAsync();
}
else if (app.Configuration.GetValue<bool>("Identity:BootstrapAdmin:Enabled"))
{
    await app.Services.InitializeCinePickIdentityAsync(CancellationToken.None);
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(self)");
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }
    await next();
});
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevelopmentCatalogSyncEndpoints();
}

app.MapGet("/", () => Results.Redirect("/health/live"))
    .ExcludeFromDescription();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});
var configuredMovieProvider = builder.Configuration["MovieProviders:Mode"];
var effectiveMovieProvider = string.Equals(configuredMovieProvider, "TMDb",
        StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(builder.Configuration["TMDb:ReadAccessToken"])
        ? "tmdb"
        : "mock";
app.MapGet("/api/system/info", () => TypedResults.Ok(new SystemInfoResponse(
        "CinePick API",
        effectiveMovieProvider,
        DateTimeOffset.UtcNow)))
    .WithName("GetSystemInfo")
    .WithTags("System");
app.MapMovieEndpoints();
app.MapCinemaEndpoints();
app.MapRecommendationEndpoints();
app.MapAuthenticationEndpoints();
app.MapUserProfileEndpoints();
app.MapAdministrationEndpoints();

app.Run();

static RateLimitPartition<string> FixedWindow(HttpContext context, int permitLimit,
    TimeSpan window)
{
    var key = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0,
        AutoReplenishment = true,
    });
}

#pragma warning disable CA1050 // WebApplicationFactory requires the top-level Program type.
public partial class Program;
#pragma warning restore CA1050
