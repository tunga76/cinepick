namespace CinePick.Domain.Cinemas;

public sealed class Auditorium
{
    private Auditorium() { }
    public Auditorium(Guid id, Guid cinemaId, string name, int capacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Id = id; CinemaId = cinemaId; Name = name; Capacity = capacity;
    }
    public Guid Id { get; private set; }
    public Guid CinemaId { get; private set; }
    public Cinema Cinema { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public ICollection<Showtime> Showtimes { get; } = new List<Showtime>();
}
