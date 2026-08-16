namespace CinePick.Application.Movies.SyncCatalog;

public interface IMovieMetadataProvider
{
    string ProviderId { get; }

    Task<IReadOnlyList<MovieMetadataItem>> GetCatalogAsync(CancellationToken cancellationToken);
}
