namespace CinePick.Domain.Cinemas;

public sealed class City
{
    private City() { }
    public City(Guid id, string name, string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        Id = id; Name = name; Slug = slug;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public ICollection<District> Districts { get; } = new List<District>();
}
