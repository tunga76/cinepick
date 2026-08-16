namespace CinePick.Api.Contracts;

public sealed record SystemInfoResponse(
    string Name,
    string ProviderMode,
    DateTimeOffset ServerTimeUtc);
