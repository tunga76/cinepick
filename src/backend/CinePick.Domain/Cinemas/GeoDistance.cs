namespace CinePick.Domain.Cinemas;

public static class GeoDistance
{
    private const double EarthRadiusKilometers = 6371.0088;

    public static double HaversineKilometers(
        double latitude1, double longitude1, double latitude2, double longitude2)
    {
        Validate(latitude1, longitude1);
        Validate(latitude2, longitude2);
        var latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        var longitudeDelta = DegreesToRadians(longitude2 - longitude1);
        var firstLatitude = DegreesToRadians(latitude1);
        var secondLatitude = DegreesToRadians(latitude2);
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + (Math.Cos(firstLatitude) * Math.Cos(secondLatitude)
                * Math.Pow(Math.Sin(longitudeDelta / 2), 2));
        return EarthRadiusKilometers * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180;

    private static void Validate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
    }
}
