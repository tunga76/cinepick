namespace CinePick.Domain.Cinemas;

public sealed class Cinema
{
    private Cinema() { }
    public Cinema(Guid id, Guid districtId, string name, string address, decimal latitude, decimal longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        Id = id; DistrictId = districtId; Name = name; Address = address;
        Latitude = latitude; Longitude = longitude;
    }
    public Guid Id { get; private set; }
    public Guid DistrictId { get; private set; }
    public District District { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public ICollection<Auditorium> Auditoriums { get; } = new List<Auditorium>();
}
