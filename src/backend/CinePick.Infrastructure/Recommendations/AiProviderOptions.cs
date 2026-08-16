namespace CinePick.Infrastructure.Recommendations;

internal sealed class AiProviderOptions
{
    public string Mode { get; init; } = "Mock";
    public string ApiKey { get; init; } = string.Empty;
    public string Endpoint { get; init; } = "https://api.openai.com/v1/responses";
    public string Model { get; init; } = "gpt-5-mini";
}
