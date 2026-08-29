using CinePick.Domain.Cinemas;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Persistence;

internal static class CinemaSeedData
{
    public static async Task SeedAsync(
        CinePickDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (await db.Cities.AnyAsync(cancellationToken)) return;

        var movieIds = await db.Movies
            .OrderByDescending(movie => movie.IsNowPlaying)
            .ThenByDescending(movie => movie.Popularity)
            .Select(movie => movie.Id)
            .Take(12)
            .ToArrayAsync(cancellationToken);

        var cityData = new[]
        {
            ("İstanbul", "istanbul", 41.015m, 28.979m, new[] { "Kadıköy", "Beşiktaş", "Şişli" }),
            ("Ankara", "ankara", 39.933m, 32.860m, new[] { "Çankaya", "Yenimahalle", "Keçiören" }),
            ("İzmir", "izmir", 38.423m, 27.142m, new[] { "Konak", "Karşıyaka", "Bornova" }),
        };
        var auditoriums = new List<Auditorium>();
        var cinemaNumber = 0;
        foreach (var (cityName, citySlug, latitude, longitude, districtNames) in cityData)
        {
            var cityId = Id("30000000", ++cinemaNumber);
            db.Cities.Add(new City(cityId, cityName, citySlug));
            for (var districtIndex = 0; districtIndex < districtNames.Length; districtIndex++)
            {
                var sequence = ((cinemaNumber - 1) * 3) + districtIndex + 1;
                var districtId = Id("31000000", sequence);
                var cinemaId = Id("32000000", sequence);
                var districtName = districtNames[districtIndex];
                db.Districts.Add(new District(districtId, cityId, districtName,
                    districtName.ToLowerInvariant().Replace('ı', 'i').Replace('ş', 's').Replace('ç', 'c').Replace('ö', 'o').Replace('ü', 'u')));
                db.Cinemas.Add(new Cinema(cinemaId, districtId, $"CinePick {districtName}",
                    $"{districtName} merkez, {cityName}", latitude + (districtIndex * 0.012m),
                    longitude + (districtIndex * 0.014m)));
                for (var room = 1; room <= 3; room++)
                {
                    var auditorium = new Auditorium(Id("33000000", (sequence * 10) + room), cinemaId,
                        $"Salon {room}", 90 + (room * 40));
                    auditoriums.Add(auditorium);
                    db.Auditoriums.Add(auditorium);
                }
            }
        }

        if (movieIds.Length > 0)
        {
            var firstShowtimeDate = timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1);
            var showtimeNumber = 0;
            for (var day = 0; day < 7; day++)
            {
                for (var index = 0; index < auditoriums.Count; index++)
                {
                    var auditorium = auditoriums[index];
                    var movieId = movieIds[(index + day) % movieIds.Length];
                    var startsAt = new DateTimeOffset(firstShowtimeDate.AddDays(day)
                        .AddHours(15 + ((index % 4) * 2)).AddMinutes(30), TimeSpan.Zero);
                    db.Showtimes.Add(new Showtime(Id("34000000", ++showtimeNumber), movieId,
                        auditorium.Id, startsAt, 220m + ((index % 3) * 25m), "TRY",
                        index % 2 == 0 ? "tr" : "en", index % 4 == 0 ? "IMAX" : "2D",
                        $"https://tickets.example.invalid/showtimes/{showtimeNumber}"));
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Guid Id(string prefix, int number) =>
        Guid.Parse($"{prefix}-0000-0000-0000-{number:D12}");
}
