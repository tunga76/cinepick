using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinePick.Application.Recommendations;

namespace CinePick.Infrastructure.Recommendations;

internal sealed class OpenAiRecommendationRanker(HttpClient httpClient, AiProviderOptions options)
    : IRecommendationRanker
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public string Method => "openai-responses";

    public async Task<IReadOnlyList<RankedRecommendation>> RankAsync(string requestText,
        RecommendationFilter filter, IReadOnlyList<RecommendationCandidate> candidates,
        CancellationToken cancellationToken)
    {
        _ = requestText;
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = options.Model,
            input = new object[]
            {
                new { role = "system", content = "Yalnızca verilen adayları sırala. Kimlikleri değiştirme. En fazla üç sonuç döndür." },
                new { role = "user", content = JsonSerializer.Serialize(new { filter, candidates }) },
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "cinepick_ranking",
                    strict = true,
                    schema = CreateSchema(),
                },
            },
        });
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var outputText = FindOutputText(document.RootElement)
                ?? throw new FormatException("Responses API did not return output_text.");
            var payload = JsonSerializer.Deserialize<RankingPayload>(outputText, SerializerOptions)
                ?? throw new FormatException("Responses API ranking payload is empty.");
            return payload.Items.Select(item => new RankedRecommendation(item.MovieId,
                item.ShowtimeId, item.Score, item.Reason)).ToArray();
        }
        catch (JsonException exception)
        {
            throw new FormatException("Responses API returned malformed JSON.", exception);
        }
    }

    private static object CreateSchema() => new
    {
        type = "object",
        properties = new
        {
            items = new
            {
                type = "array",
                minItems = 1,
                maxItems = 3,
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        movieId = new { type = "string" },
                        showtimeId = new { type = "string" },
                        score = new { type = "number", minimum = 0, maximum = 100 },
                        reason = new { type = "string", minLength = 1, maxLength = 500 },
                    },
                    required = new[] { "movieId", "showtimeId", "score", "reason" },
                    additionalProperties = false,
                },
            },
        },
        required = new[] { "items" },
        additionalProperties = false,
    };

    private static string? FindOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text))
                    return text.GetString();
            }
        }
        return null;
    }

    private sealed record RankingPayload(IReadOnlyList<RankingItem> Items);
    private sealed record RankingItem(Guid MovieId, Guid ShowtimeId, decimal Score, string Reason);
}
