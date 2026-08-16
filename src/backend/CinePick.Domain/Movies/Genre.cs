namespace CinePick.Domain.Movies;

public sealed class Genre
{
    private Genre()
    {
    }

    public Genre(Guid id, string name, string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Id = id;
        Name = name;
        Slug = slug;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public ICollection<MovieGenre> MovieGenres { get; } = new List<MovieGenre>();
}
