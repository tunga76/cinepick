using CinePick.Application.Movies.SyncCatalog;
using CinePick.Domain.Movies;

namespace CinePick.Infrastructure.Movies;

internal sealed class MockMovieMetadataProvider : IMovieMetadataProvider
{
    public string ProviderId => "mock";

    public Task<IReadOnlyList<MovieMetadataItem>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<MovieMetadataItem> items =
        [
            new("mock-001", "Boğazın Ötesinde", "Beyond the Bosphorus",
                "İstanbul'un iki yakasında aynı sırrın peşine düşen iki kardeşin tempolu macerası.",
                new DateOnly(2026, 7, 24), 112, "tr", AgeRating.Age13, 8.1m, 287, 84.2m,
                true, false, ["aksiyon", "gerilim"]),
            new("mock-sync-021", "Gece Kütüphanesi", "The Night Library",
                "Kapanmak üzere olan bir kütüphanede kitapların sakladığı gizemi keşfeden üç arkadaşın hikâyesi.",
                new DateOnly(2026, 8, 14), 98, "tr", AgeRating.Age10, 7.6m, 146, 49.8m,
                true, false, ["aile", "dram"]),
        ];
        return Task.FromResult(items);
    }
}
