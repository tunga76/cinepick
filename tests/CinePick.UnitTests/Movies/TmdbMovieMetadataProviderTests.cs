using System.Net;
using System.Text;
using CinePick.Domain.Movies;
using CinePick.Infrastructure.Movies;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CinePick.UnitTests.Movies;

public sealed class TmdbMovieMetadataProviderTests
{
    [Fact]
    public async Task GetCatalogAsyncMergesListsAndMapsTurkishMetadata()
    {
        using var client = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/3/movie/now_playing" => Json("""
                {"page":1,"total_pages":1,"results":[{"id":42}]}
                """),
            "/3/movie/upcoming" => Json("""
                {"page":1,"total_pages":1,"results":[{"id":42}]}
                """),
            "/3/movie/42" => Json("""
                {
                  "id":42,"title":"Örnek Film","original_title":"Sample Movie",
                  "overview":"Özet","release_date":"2026-08-29","runtime":112,
                  "original_language":"tr","vote_average":7.8,"vote_count":123,
                  "popularity":45.5,"genres":[{"id":28},{"id":999}],
                  "poster_path":"/poster.jpg","backdrop_path":"https://invalid.example/image.jpg",
                  "release_dates":{"results":[{"iso_3166_1":"TR","release_dates":[
                    {"certification":"13+","type":3}
                  ]}]}
                }
                """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var provider = CreateProvider(client);

        var result = await provider.GetCatalogAsync(CancellationToken.None);

        var movie = Assert.Single(result);
        Assert.Equal("42", movie.ExternalId);
        Assert.True(movie.IsNowPlaying);
        Assert.True(movie.IsUpcoming);
        Assert.Equal(AgeRating.Age13, movie.AgeRating);
        Assert.Equal(["aksiyon"], movie.GenreSlugs);
        Assert.Equal("/poster.jpg", movie.PosterPath);
        Assert.Null(movie.BackdropPath);
    }

    [Fact]
    public async Task GetCatalogAsyncSkipsMovieWithoutRuntime()
    {
        using var client = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/3/movie/now_playing" => Json(
                "{\"page\":1,\"total_pages\":1,\"results\":[{\"id\":7}]}"),
            "/3/movie/upcoming" => Json(
                "{\"page\":1,\"total_pages\":1,\"results\":[]}"),
            "/3/movie/7" => Json("""
                {"id":7,"title":"Eksik","release_date":"2026-08-29","runtime":null,
                 "genres":[],"release_dates":{"results":[]}}
                """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });

        var result = await CreateProvider(client).GetCatalogAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCatalogAsyncReportsUnauthorizedToken()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateProvider(client).GetCatalogAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    private static TmdbMovieMetadataProvider CreateProvider(HttpClient client) =>
        new(client, Options.Create(new TmdbOptions()),
            NullLogger<TmdbMovieMetadataProvider>.Instance);

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new StubHttpMessageHandler(response))
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/"),
        };

    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(response(request));
        }
    }
}
