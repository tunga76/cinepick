namespace CinePick.Domain.Cinemas;

public sealed class District
{
    private District() { }
    public District(Guid id, Guid cityId, string name, string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        Id = id; CityId = cityId; Name = name; Slug = slug;
    }
    public Guid Id { get; private set; }
    public Guid CityId { get; private set; }
    public City City { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public ICollection<Cinema> Cinemas { get; } = new List<Cinema>();
}
