using CinePick.Domain.Cinemas;

namespace CinePick.UnitTests.Cinemas;

public sealed class CinemaTests
{
    [Fact]
    public void ConstructorRejectsCoordinatesOutsideWorldBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cinema(
            Guid.NewGuid(), Guid.NewGuid(), "Test", "Adres", 91m, 29m));
    }

    [Fact]
    public void ShowtimeRejectsNegativePrice()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Showtime(Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, -1m, "TRY", "tr", "2D",
            "https://tickets.example.invalid/test"));
    }

    [Fact]
    public void HaversineReturnsExpectedIstanbulDistance()
    {
        var distance = GeoDistance.HaversineKilometers(40.9909, 29.0284, 41.0430, 29.0094);

        Assert.InRange(distance, 5.9, 6.2);
    }
}
