using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using CinePick.Application.Common;
using CinePick.Application.Movies.GetMovies;
using CinePick.Application.Movies.GetGenres;
using CinePick.Application.Movies.GetMovieDetail;
using CinePick.Application.Movies.SyncCatalog;
using CinePick.Application.Cinemas;
using CinePick.Application.Cinemas.SyncShowtimes;
using CinePick.Application.Administration;
using CinePick.Application.Recommendations;
using CinePick.Application.Users;
using CinePick.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using CinePick.Infrastructure.Identity;
using Testcontainers.MsSql;
using System.Text.Json;

namespace CinePick.IntegrationTests;

public sealed class ApiHealthTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Task InitializeAsync() => _sqlServer.StartAsync();

    public async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
    }

    [Fact]
    public async Task HealthEndpointsReportLiveAndReady()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var liveResponse = await client.GetAsync(
            new Uri("/health/live", UriKind.Relative),
            CancellationToken.None);
        var readyResponse = await client.GetAsync(
            new Uri("/health/ready", UriKind.Relative),
            CancellationToken.None);

        Assert.Equal(System.Net.HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, readyResponse.StatusCode);
    }

    [Fact]
    public async Task OpenApiPublishesThePublicAndProtectedContracts()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await using var stream = await client.GetStreamAsync(
            new Uri("/openapi/v1.json", UriKind.Relative), CancellationToken.None);
        using var document = await JsonDocument.ParseAsync(stream);
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/movies/now-playing", out _));
        Assert.True(paths.TryGetProperty("/api/recommendations", out _));
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/admin/movie-catalog-syncs", out _));
    }

    [Fact]
    public async Task MigrationsCreateThePrimaryShowtimeWindowIndex()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinePickDbContext>();

        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
        var indexCount = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value]
            FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N'[dbo].[Showtimes]')
              AND [name] = N'IX_Showtimes_IsCancelled_StartsAt'
            """).SingleAsync();

        Assert.Contains("20260815073140_LinkRecommendationSessionsToUsers", appliedMigrations);
        Assert.Contains(appliedMigrations, migration => migration.EndsWith(
            "_AddShowtimeQueryIndex", StringComparison.Ordinal));
        Assert.Equal(1, indexCount);
    }

    [Fact]
    public async Task NowPlayingEndpointReturnsSeededMovies()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<PagedResponse<MovieListItem>>(
            new Uri("/api/movies/now-playing?page=1&pageSize=12", UriKind.Relative),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(response.TotalCount >= 12);
        Assert.Equal(12, response.Items.Count);
        Assert.All(response.Items, movie => Assert.NotEmpty(movie.Genres));
    }

    [Fact]
    public async Task CatalogEndpointsSupportGenresSearchAndDetail()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var genres = await client.GetFromJsonAsync<IReadOnlyList<GenreListItem>>(
            new Uri("/api/genres", UriKind.Relative), CancellationToken.None);
        var search = await client.GetFromJsonAsync<PagedResponse<MovieListItem>>(
            new Uri("/api/movies/now-playing?search=Robot", UriKind.Relative), CancellationToken.None);
        var detail = await client.GetFromJsonAsync<MovieDetail>(
            new Uri("/api/movies/20000000-0000-0000-0000-000000000001", UriKind.Relative),
            CancellationToken.None);

        Assert.NotNull(genres);
        Assert.Equal(8, genres.Count);
        Assert.NotNull(search);
        Assert.Single(search.Items);
        Assert.Contains("Robot", search.Items[0].Title, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(detail);
        Assert.NotEmpty(detail.Genres);
    }

    [Fact]
    public async Task DevelopmentCatalogSyncIsIdempotent()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var firstResponse = await client.PostAsync(
            new Uri("/api/development/movie-catalog-syncs", UriKind.Relative), null,
            CancellationToken.None);
        using var secondResponse = await client.PostAsync(
            new Uri("/api/development/movie-catalog-syncs", UriKind.Relative), null,
            CancellationToken.None);
        var first = await firstResponse.Content.ReadFromJsonAsync<MovieCatalogSyncResult>();
        var second = await secondResponse.Content.ReadFromJsonAsync<MovieCatalogSyncResult>();

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(2, first.ReceivedCount);
        Assert.InRange(first.InsertedCount, 0, 1);
        Assert.Equal(0, second.InsertedCount);
        Assert.Equal(2, second.UpdatedCount);
    }

    [Fact]
    public async Task CinemaCatalogReturnsSeededCitiesAuditoriumsAndShowtimes()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var cities = await client.GetFromJsonAsync<IReadOnlyList<CityListItem>>(
            new Uri("/api/cities", UriKind.Relative), CancellationToken.None);
        var cinemas = await client.GetFromJsonAsync<IReadOnlyList<CinemaListItem>>(
            new Uri("/api/cinemas", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(cities);
        Assert.NotNull(cinemas);
        Assert.Equal(3, cities.Count);
        Assert.Equal(9, cinemas.Count);

        var detail = await client.GetFromJsonAsync<CinemaDetail>(
            new Uri($"/api/cinemas/{cinemas[0].Id}", UriKind.Relative), CancellationToken.None);
        var showtimeFrom = DateTimeOffset.UtcNow.Date;
        var showtimeTo = showtimeFrom.AddDays(8);
        var showtimes = await client.GetFromJsonAsync<IReadOnlyList<ShowtimeListItem>>(
            new Uri($"/api/showtimes?from={showtimeFrom:O}&to={showtimeTo:O}", UriKind.Relative),
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(3, detail.Auditoriums.Count);
        Assert.NotNull(showtimes);
        Assert.True(showtimes.Count >= 189);
        Assert.All(showtimes, showtime =>
        {
            Assert.True(showtime.EndsAt > showtime.StartsAt);
            Assert.Equal("TRY", showtime.Currency);
            Assert.StartsWith("https://tickets.example.invalid/", showtime.TicketUrl,
                StringComparison.Ordinal);
        });

        var nearby = await client.GetFromJsonAsync<IReadOnlyList<CinemaListItem>>(
            new Uri("/api/cinemas?latitude=41.015&longitude=28.979&radiusKilometers=25",
                UriKind.Relative), CancellationToken.None);
        using var invalidLocation = await client.GetAsync(
            new Uri("/api/cinemas?latitude=41.015", UriKind.Relative), CancellationToken.None);

        Assert.NotNull(nearby);
        Assert.Equal(3, nearby.Count);
        Assert.All(nearby, cinema => Assert.NotNull(cinema.DistanceKilometers));
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidLocation.StatusCode);
    }

    [Fact]
    public async Task DevelopmentShowtimeSyncIsIdempotent()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var firstResponse = await client.PostAsync(
            new Uri("/api/development/showtime-catalog-syncs", UriKind.Relative), null,
            CancellationToken.None);
        using var secondResponse = await client.PostAsync(
            new Uri("/api/development/showtime-catalog-syncs", UriKind.Relative), null,
            CancellationToken.None);
        var first = await firstResponse.Content.ReadFromJsonAsync<ShowtimeCatalogSyncResult>();
        var second = await secondResponse.Content.ReadFromJsonAsync<ShowtimeCatalogSyncResult>();

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(2, first.ReceivedCount);
        Assert.InRange(first.InsertedCount, 0, 2);
        Assert.Equal(0, second.InsertedCount);
        Assert.Equal(2, second.UpdatedCount);
    }

    [Fact]
    public async Task DevelopmentOperationsCanCancelAndRestoreShowtime()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var showtimes = await client.GetFromJsonAsync<IReadOnlyList<DevelopmentShowtimeListItem>>(
            new Uri("/api/development/showtimes", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(showtimes);
        var selected = showtimes.First(item => !item.IsCancelled);

        using var cancelResponse = await client.PutAsJsonAsync(
            new Uri($"/api/development/showtimes/{selected.Id}/cancellation", UriKind.Relative),
            new { IsCancelled = true }, CancellationToken.None);
        var showtimeFrom = DateTimeOffset.UtcNow.Date;
        var showtimeTo = showtimeFrom.AddDays(8);
        var publicShowtimes = await client.GetFromJsonAsync<IReadOnlyList<ShowtimeListItem>>(
            new Uri($"/api/showtimes?from={showtimeFrom:O}&to={showtimeTo:O}", UriKind.Relative),
            CancellationToken.None);
        using var restoreResponse = await client.PutAsJsonAsync(
            new Uri($"/api/development/showtimes/{selected.Id}/cancellation", UriKind.Relative),
            new { IsCancelled = false }, CancellationToken.None);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, cancelResponse.StatusCode);
        Assert.NotNull(publicShowtimes);
        Assert.DoesNotContain(publicShowtimes, item => item.Id == selected.Id);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, restoreResponse.StatusCode);
    }

    [Fact]
    public async Task DevelopmentOperationsAreNotMappedInProduction()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/development/sync-logs", UriKind.Relative), CancellationToken.None);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecommendationEndpointAppliesParsedMandatoryFilters()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/recommendations", UriKind.Relative),
            new { Text = "Yarın 18:00'den sonra Kadıköy'de 100 dakikadan kısa bir film" },
            CancellationToken.None);
        var recommendation = await response.Content.ReadFromJsonAsync<RecommendationResponse>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(recommendation);
        Assert.Equal("mock-ai", recommendation.Method);
        Assert.Equal(100, recommendation.Filter.MaximumRuntimeMinutes);
        Assert.Equal("kadikoy", recommendation.Filter.DistrictSlug);
        Assert.InRange(recommendation.CandidateCount, 1, 20);
        Assert.InRange(recommendation.Items.Count, 1, 3);
        Assert.All(recommendation.Items, item => Assert.Equal("Kadıköy", item.DistrictName));
        foreach (var item in recommendation.Items)
        {
            var movie = await client.GetFromJsonAsync<MovieDetail>(
                new Uri($"/api/movies/{item.MovieId}", UriKind.Relative), CancellationToken.None);
            Assert.NotNull(movie);
            Assert.True(movie.RuntimeMinutes <= 100);
        }

        var dbFactory = factory.Services.GetRequiredService<IDbContextFactory<CinePickDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(CancellationToken.None);
        var session = await db.RecommendationSessions.AsNoTracking()
            .Include(item => item.Candidates).Include(item => item.Results)
            .SingleAsync(item => item.Id == recommendation.SessionId, CancellationToken.None);
        Assert.Equal("mock-ai", session.Method);
        Assert.Equal(recommendation.CandidateCount, session.Candidates.Count);
        Assert.Equal(recommendation.Items.Count, session.Results.Count);
        Assert.DoesNotContain(session.GetType().GetProperties(), property =>
            property.Name.Contains("Text", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Prompt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OpenAiModeWithoutApiKeySafelyUsesMockRanker()
    {
        await using var factory = CreateFactory(aiMode: "OpenAI", aiApiKey: string.Empty);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/recommendations", UriKind.Relative),
            new { Text = "YarÄ±n akÅŸam KadÄ±kÃ¶y'de bir film" },
            CancellationToken.None);
        var recommendation = await response.Content.ReadFromJsonAsync<RecommendationResponse>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(recommendation);
        Assert.Equal("mock-ai", recommendation.Method);
    }

    [Fact]
    public async Task AuthenticationUsesIdentityCookieAndCsrfProtection()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var anonymousMe = await client.GetAsync(
            new Uri("/api/auth/me", UriKind.Relative), CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, anonymousMe.StatusCode);

        using var missingCsrf = await client.PostAsJsonAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            new { Email = "csrf@example.test", Password = "Strong!Pass1", DisplayName = "CSRF" },
            CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var csrf = await client.GetFromJsonAsync<CsrfResponse>(
            new Uri("/api/auth/csrf", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(csrf);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);

        var email = $"user-{Guid.NewGuid():N}@example.test";
        using var register = await client.PostAsJsonAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            new { Email = email, Password = "Strong!Pass1", DisplayName = "Test User" },
            CancellationToken.None);
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        Assert.NotNull(registered);
        Assert.Equal(email, registered.Email);

        var current = await client.GetFromJsonAsync<AuthenticatedUserResponse>(
            new Uri("/api/auth/me", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(current);
        Assert.Equal(registered.Id, current.Id);

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var authenticatedCsrf = await client.GetFromJsonAsync<CsrfResponse>(
            new Uri("/api/auth/csrf", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(authenticatedCsrf);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", authenticatedCsrf.Token);
        using var logout = await client.PostAsync(
            new Uri("/api/auth/logout", UriKind.Relative), null, CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, logout.StatusCode);
        using var signedOutMe = await client.GetAsync(
            new Uri("/api/auth/me", UriKind.Relative), CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, signedOutMe.StatusCode);
    }

    [Fact]
    public async Task UserPreferencesAndMovieStatesAreIsolatedByAuthenticatedUser()
    {
        await using var factory = CreateFactory();
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        await RegisterAndPrepareCsrfAsync(firstClient, $"first-{Guid.NewGuid():N}@example.test");
        await RegisterAndPrepareCsrfAsync(secondClient, $"second-{Guid.NewGuid():N}@example.test");
        var movies = await firstClient.GetFromJsonAsync<PagedResponse<MovieListItem>>(
            new Uri("/api/movies/now-playing?pageSize=1", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(movies);
        var movieId = movies.Items[0].Id;

        using var preferencesResponse = await firstClient.PutAsJsonAsync(
            new Uri("/api/users/me/preferences", UriKind.Relative),
            new UpdateUserPreferences("Bilim-Kurgu", "TR", 120, 25), CancellationToken.None);
        preferencesResponse.EnsureSuccessStatusCode();
        var preferences = await preferencesResponse.Content.ReadFromJsonAsync<UserPreferencesDto>();
        Assert.NotNull(preferences);
        Assert.Equal("bilim-kurgu", preferences.PreferredGenreSlug);

        using var stateResponse = await firstClient.PutAsJsonAsync(
            new Uri($"/api/users/me/movie-states/{movieId}", UriKind.Relative),
            new UpdateUserMovieState(true, true, 9), CancellationToken.None);
        stateResponse.EnsureSuccessStatusCode();
        var firstState = await stateResponse.Content.ReadFromJsonAsync<UserMovieStateDto>();
        Assert.NotNull(firstState);
        Assert.Equal(9, firstState.Rating);

        using var otherUserState = await secondClient.GetAsync(
            new Uri($"/api/users/me/movie-states/{movieId}", UriKind.Relative),
            CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, otherUserState.StatusCode);
        var otherUserStates = await secondClient.GetFromJsonAsync<IReadOnlyList<UserMovieStateDto>>(
            new Uri("/api/users/me/movie-states", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(otherUserStates);
        Assert.Empty(otherUserStates);

        using var invalidRating = await firstClient.PutAsJsonAsync(
            new Uri($"/api/users/me/movie-states/{movieId}", UriKind.Relative),
            new UpdateUserMovieState(true, true, 11), CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidRating.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRecommendationsUseMovieSignalsAndCreatePrivateHistory()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await RegisterAndPrepareCsrfAsync(client, $"history-{Guid.NewGuid():N}@example.test");
        var request = new { Text = "Yarın 18:00'den sonra Kadıköy'de bir film" };

        using var firstResponse = await client.PostAsJsonAsync(
            new Uri("/api/recommendations", UriKind.Relative), request, CancellationToken.None);
        firstResponse.EnsureSuccessStatusCode();
        var first = await firstResponse.Content.ReadFromJsonAsync<RecommendationResponse>();
        Assert.NotNull(first);
        Assert.InRange(first.Items.Count, 2, 3);
        var selected = first.Items[^1];
        using var stateResponse = await client.PutAsJsonAsync(
            new Uri($"/api/users/me/movie-states/{selected.MovieId}", UriKind.Relative),
            new UpdateUserMovieState(true, true, 10), CancellationToken.None);
        stateResponse.EnsureSuccessStatusCode();

        using var secondResponse = await client.PostAsJsonAsync(
            new Uri("/api/recommendations", UriKind.Relative), request, CancellationToken.None);
        secondResponse.EnsureSuccessStatusCode();
        var second = await secondResponse.Content.ReadFromJsonAsync<RecommendationResponse>();
        Assert.NotNull(second);
        Assert.Equal(selected.MovieId, second.Items[0].MovieId);

        var history = await client.GetFromJsonAsync<IReadOnlyList<RecommendationHistoryItem>>(
            new Uri("/api/users/me/recommendation-history", UriKind.Relative),
            CancellationToken.None);
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.All(history, item => Assert.NotEmpty(item.Results));
    }

    [Fact]
    public async Task AdministrationEndpointsRequireAdminRoleAndCsrfForMutations()
    {
        await using var factory = CreateFactory();
        var email = $"admin-{Guid.NewGuid():N}@example.test";
        using var ordinaryClient = factory.CreateClient();
        await RegisterAndPrepareCsrfAsync(ordinaryClient, email);
        using var forbidden = await ordinaryClient.GetAsync(
            new Uri("/api/admin/sync-logs", UriKind.Relative), CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, forbidden.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            var roleResult = await userManager.AddToRoleAsync(user, "Admin");
            Assert.True(roleResult.Succeeded);
        }

        using var adminClient = factory.CreateClient();
        await LoginAndPrepareCsrfAsync(adminClient, email);
        var logs = await adminClient.GetFromJsonAsync<IReadOnlyList<SyncLogListItem>>(
            new Uri("/api/admin/sync-logs", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(logs);
        adminClient.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var missingCsrf = await adminClient.PostAsync(
            new Uri("/api/admin/movie-catalog-syncs", UriKind.Relative), null,
            CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await RefreshCsrfAsync(adminClient);
        using var sync = await adminClient.PostAsync(
            new Uri("/api/admin/movie-catalog-syncs", UriKind.Relative), null,
            CancellationToken.None);
        sync.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ApiAddsSecurityHeadersAndRateLimitsAuthenticationTraffic()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var first = await client.GetAsync(new Uri("/api/auth/csrf", UriKind.Relative),
            CancellationToken.None);
        first.EnsureSuccessStatusCode();
        Assert.Equal("nosniff", first.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", first.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("no-store", first.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        Assert.Contains("no-cache", first.Headers.Pragma.Select(value => value.Name));

        for (var index = 1; index < 20; index++)
        {
            using var accepted = await client.GetAsync(
                new Uri("/api/auth/csrf", UriKind.Relative), CancellationToken.None);
            accepted.EnsureSuccessStatusCode();
        }
        using var limited = await client.GetAsync(
            new Uri("/api/auth/csrf", UriKind.Relative), CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string? environment = null, string? aiMode = null, string? aiApiKey = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                if (environment is not null)
                {
                    builder.UseEnvironment(environment);
                }
                builder.UseSetting(
                    "ConnectionStrings:CinePick",
                    _sqlServer.GetConnectionString());
                builder.UseSetting("Database:Initialize", "true");
                builder.UseSetting("OpenTelemetry:Otlp:Enabled", "false");
                if (aiMode is not null) builder.UseSetting("AI:Mode", aiMode);
                if (aiApiKey is not null) builder.UseSetting("AI:ApiKey", aiApiKey);
            });

    private sealed record CsrfResponse(string Token);
    private sealed record AuthenticatedUserResponse(Guid Id, string Email, string DisplayName,
        IReadOnlyList<string> Roles);

    private static async Task RegisterAndPrepareCsrfAsync(HttpClient client, string email)
    {
        var anonymousCsrf = await client.GetFromJsonAsync<CsrfResponse>(
            new Uri("/api/auth/csrf", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(anonymousCsrf);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", anonymousCsrf.Token);
        using var register = await client.PostAsJsonAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            new { Email = email, Password = "Strong!Pass1", DisplayName = "Test User" },
            CancellationToken.None);
        register.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        await RefreshCsrfAsync(client);
    }

    private static async Task LoginAndPrepareCsrfAsync(HttpClient client, string email)
    {
        await RefreshCsrfAsync(client);
        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { Email = email, Password = "Strong!Pass1" }, CancellationToken.None);
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        await RefreshCsrfAsync(client);
    }

    private static async Task RefreshCsrfAsync(HttpClient client)
    {
        var authenticatedCsrf = await client.GetFromJsonAsync<CsrfResponse>(
            new Uri("/api/auth/csrf", UriKind.Relative), CancellationToken.None);
        Assert.NotNull(authenticatedCsrf);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", authenticatedCsrf.Token);
    }
}
