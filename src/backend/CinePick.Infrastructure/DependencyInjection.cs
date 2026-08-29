using CinePick.Infrastructure.Health;
using CinePick.Infrastructure.Identity;
using CinePick.Infrastructure.Movies;
using CinePick.Infrastructure.Persistence;
using CinePick.Application.Movies;
using CinePick.Application.Movies.SyncCatalog;
using CinePick.Application.Cinemas;
using CinePick.Infrastructure.Cinemas;
using CinePick.Application.Cinemas.SyncShowtimes;
using CinePick.Application.Administration;
using CinePick.Infrastructure.Administration;
using CinePick.Application.Recommendations;
using CinePick.Infrastructure.Recommendations;
using CinePick.Application.Users;
using CinePick.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace CinePick.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CinePick")
            ?? throw new InvalidOperationException(
                "Connection string 'CinePick' is required.");

        services.AddPooledDbContextFactory<CinePickDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
                sqlServer.EnableRetryOnFailure(maxRetryCount: 3)));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<CinePickDbContext>()
            .AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "CinePick.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"));
        services.AddScoped<IMovieCatalogQuery, MovieCatalogQuery>();
        services.Configure<TmdbOptions>(configuration.GetSection(TmdbOptions.SectionName));
        services.AddScoped<MockMovieMetadataProvider>();
        services.AddHttpClient<TmdbMovieMetadataProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ReadAccessToken);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IMovieMetadataProvider>(serviceProvider =>
        {
            var mode = configuration["MovieProviders:Mode"] ?? "Mock";
            var options = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;
            return string.Equals(mode, "TMDb", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(options.ReadAccessToken)
                    ? serviceProvider.GetRequiredService<TmdbMovieMetadataProvider>()
                    : serviceProvider.GetRequiredService<MockMovieMetadataProvider>();
        });
        services.AddScoped<IMovieCatalogSynchronizer, MovieCatalogSynchronizer>();
        services.AddScoped<ICinemaCatalogQuery, CinemaCatalogQuery>();
        services.AddScoped<IShowtimeProvider, MockShowtimeProvider>();
        services.AddScoped<IShowtimeCatalogSynchronizer, ShowtimeCatalogSynchronizer>();
        services.AddScoped<IDevelopmentOperations, DevelopmentOperations>();
        services.AddScoped<IRecommendationRequestParser, MockRecommendationRequestParser>();
        services.AddScoped<IRecommendationCandidateQuery, RecommendationCandidateQuery>();
        var aiOptions = new AiProviderOptions
        {
            Mode = configuration["AI:Mode"] ?? "Mock",
            ApiKey = configuration["AI:ApiKey"] ?? string.Empty,
            Endpoint = configuration["AI:Endpoint"] ?? "https://api.openai.com/v1/responses",
            Model = configuration["AI:Model"] ?? "gpt-5-mini"
        };
        services.AddSingleton(aiOptions);
        services.AddScoped<MockRecommendationRanker>();
        services.AddHttpClient<OpenAiRecommendationRanker>(client =>
            client.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<IRecommendationRanker>(provider =>
            string.Equals(aiOptions.Mode, "OpenAI", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(aiOptions.ApiKey)
                ? provider.GetRequiredService<OpenAiRecommendationRanker>()
                : provider.GetRequiredService<MockRecommendationRanker>());
        services.AddScoped<RecommendationAuditStore>();
        services.AddScoped<IRecommendationAuditStore>(provider =>
            provider.GetRequiredService<RecommendationAuditStore>());
        services.AddScoped<IRecommendationHistoryQuery>(provider =>
            provider.GetRequiredService<RecommendationAuditStore>());
        services.AddScoped<RecommendationService>();
        services.AddScoped<IUserProfileStore, UserProfileStore>();
        services.Configure<ShowtimeProviderOptions>(options =>
        {
            var configuredHosts = configuration
                .GetSection($"{ShowtimeProviderOptions.SectionName}:AllowedTicketHosts")
                .GetChildren().Select(item => item.Value).OfType<string>().ToArray();
            if (configuredHosts.Length > 0)
            {
                options.AllowedTicketHosts = configuredHosts;
            }
        });
        services.AddSingleton(TimeProvider.System);

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("sqlserver", tags: ["ready"]);

        return services;
    }

    public static async Task InitializeCinePickDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<CinePickDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await CinePickSeedData.SeedAsync(dbContext, cancellationToken);
        await CinemaSeedData.SeedAsync(dbContext, cancellationToken);
        await InitializeCinePickIdentityAsync(services, cancellationToken);
    }

    public static async Task InitializeCinePickIdentityAsync(this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Admin role could not be initialized.");
        }
        var configuration = scopedServices.GetRequiredService<IConfiguration>();
        var options = new IdentityBootstrapOptions
        {
            Enabled = configuration.GetValue<bool>("Identity:BootstrapAdmin:Enabled"),
            Email = configuration["Identity:BootstrapAdmin:Email"] ?? string.Empty,
            Password = configuration["Identity:BootstrapAdmin:Password"] ?? string.Empty,
            DisplayName = configuration["Identity:BootstrapAdmin:DisplayName"] ?? "CinePick Admin",
        };
        if (!options.Enabled) return;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
            throw new InvalidOperationException("Enabled admin bootstrap requires email and password.");
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(options.Email);
        if (user is null)
        {
            user = new ApplicationUser { Id = Guid.NewGuid(), UserName = options.Email,
                Email = options.Email, DisplayName = options.DisplayName,
                CreatedAt = TimeProvider.System.GetUtcNow() };
            var userResult = await userManager.CreateAsync(user, options.Password);
            if (!userResult.Succeeded)
                throw new InvalidOperationException("Bootstrap admin user could not be created.");
        }
        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Admin role could not be assigned.");
        }
    }
}
