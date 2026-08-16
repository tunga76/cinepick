using CinePick.Domain.Movies;
using Microsoft.EntityFrameworkCore;

namespace CinePick.Infrastructure.Persistence;

internal static class CinePickSeedData
{
    private static readonly Guid Action = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Animation = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid Comedy = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid Drama = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid Family = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid ScienceFiction = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid Thriller = Guid.Parse("10000000-0000-0000-0000-000000000007");
    private static readonly Guid Romance = Guid.Parse("10000000-0000-0000-0000-000000000008");

    public static async Task SeedAsync(
        CinePickDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Movies.AnyAsync(cancellationToken))
        {
            return;
        }

        var genres = new[]
        {
            new Genre(Action, "Aksiyon", "aksiyon"),
            new Genre(Animation, "Animasyon", "animasyon"),
            new Genre(Comedy, "Komedi", "komedi"),
            new Genre(Drama, "Dram", "dram"),
            new Genre(Family, "Aile", "aile"),
            new Genre(ScienceFiction, "Bilim Kurgu", "bilim-kurgu"),
            new Genre(Thriller, "Gerilim", "gerilim"),
            new Genre(Romance, "Romantik", "romantik"),
        };

        var movies = new[]
        {
            CreateMovie(1, "Boğazın Ötesinde", "Beyond the Bosphorus", "İstanbul'un iki yakasında aynı sırrın peşine düşen iki kardeşin tempolu macerası.", new DateOnly(2026, 7, 24), 112, AgeRating.Age13, 8.1m, 84.2m, true, false, Action, Thriller),
            CreateMovie(2, "Yıldız Tozu Ekibi", "Stardust Crew", "Genç kaşifler kayıp bir uzay istasyonunu bulmak için beklenmedik bir yolculuğa çıkar.", new DateOnly(2026, 8, 1), 104, AgeRating.Age10, 7.8m, 79.5m, true, false, ScienceFiction, Family),
            CreateMovie(3, "Mahalle Maçı", "The Neighborhood Match", "Eski dostlar mahalle sahasını kurtarmak için yıllar sonra yeniden takım olur.", new DateOnly(2026, 8, 7), 96, AgeRating.GeneralAudience, 7.4m, 76.8m, true, false, Comedy, Family),
            CreateMovie(4, "Kayıp Nota", "The Missing Note", "Bir piyanist, yarım kalmış bestesinin izinde geçmişiyle yüzleşir.", new DateOnly(2026, 7, 18), 118, AgeRating.Age10, 8.4m, 73.1m, true, false, Drama, Romance),
            CreateMovie(5, "Bulutların Çocukları", "Children of the Clouds", "Gökyüzündeki şehirlerini kurtarmaya çalışan iki çocuğun renkli serüveni.", new DateOnly(2026, 8, 8), 88, AgeRating.GeneralAudience, 7.9m, 71.4m, true, false, Animation, Family),
            CreateMovie(6, "Son Vapur", "The Last Ferry", "Gece vapurunda karşılaşan yabancılar, şehir uyumadan önce hayatlarını değiştiren bir karar verir.", new DateOnly(2026, 7, 31), 101, AgeRating.Age13, 7.7m, 68.9m, true, false, Drama, Romance),
            CreateMovie(7, "Kırmızı Hat", "Red Line", "Bir metro makinisti şehrin altında büyüyen tehlikeyi durdurmak için zamana karşı yarışır.", new DateOnly(2026, 8, 5), 109, AgeRating.Age16, 7.6m, 65.7m, true, false, Action, Thriller),
            CreateMovie(8, "Bir Yaz Akşamı", "One Summer Evening", "Sahil kasabasında yolları kesişen dört insanın sıcak ve neşeli hikâyesi.", new DateOnly(2026, 7, 10), 99, AgeRating.GeneralAudience, 7.5m, 61.2m, true, false, Comedy, Romance),
            CreateMovie(9, "Robot Pati", "Robo Paws", "Meraklı bir çocuk ile sakar yardım robotu kayıp evcil hayvanları bulmaya çalışır.", new DateOnly(2026, 8, 12), 91, AgeRating.GeneralAudience, 8.0m, 59.8m, true, false, Animation, Comedy, Family),
            CreateMovie(10, "Sessiz İstasyon", "Silent Station", "Issız bir araştırma üssünden gelen sinyal, bir kurtarma ekibini bilinmeyene sürükler.", new DateOnly(2026, 7, 25), 121, AgeRating.Age16, 7.9m, 57.4m, true, false, ScienceFiction, Thriller),
            CreateMovie(11, "İkinci Perde", "Second Act", "Emekli bir tiyatro topluluğu son kez sahneye çıkmak için yeniden bir araya gelir.", new DateOnly(2026, 8, 3), 106, AgeRating.Age10, 8.2m, 54.6m, true, false, Comedy, Drama),
            CreateMovie(12, "Rüzgârın Haritası", "Map of the Wind", "Genç bir denizci, ailesinden kalan haritayla Ege'de unutulmuş bir rotayı keşfeder.", new DateOnly(2026, 7, 17), 115, AgeRating.Age10, 7.8m, 51.9m, true, false, Drama, Family),
            CreateMovie(13, "Ay Üssü 7", "Moonbase Seven", "Ay'daki ilk sivil yerleşimin ekibi, yaklaşan güneş fırtınasına karşı birlikte hareket eder.", new DateOnly(2026, 8, 28), 124, AgeRating.Age13, 0m, 48.3m, false, true, ScienceFiction, Action),
            CreateMovie(14, "Küçük Devler", "Little Giants", "Minik orman canlıları yuvalarını korumak için büyük bir dayanışma başlatır.", new DateOnly(2026, 9, 4), 86, AgeRating.GeneralAudience, 0m, 45.7m, false, true, Animation, Family),
            CreateMovie(15, "Tesadüfler Bürosu", "The Office of Coincidences", "Tesadüfleri düzenlediğine inanan bir memurun planı gerçek aşkla karşılaşınca bozulur.", new DateOnly(2026, 8, 21), 103, AgeRating.Age10, 0m, 43.9m, false, true, Comedy, Romance),
            CreateMovie(16, "Derinlik 42", "Depth 42", "Bir araştırma denizaltısının ekibi haritalanmamış bir çukurda olağanüstü bir keşif yapar.", new DateOnly(2026, 9, 11), 117, AgeRating.Age13, 0m, 41.5m, false, true, ScienceFiction, Thriller),
            CreateMovie(17, "Eski Fotoğraf", "The Old Photograph", "Bir aile fotoğrafındaki yabancı yüz, üç kuşağı bir araya getiren bir yolculuk başlatır.", new DateOnly(2026, 9, 18), 110, AgeRating.Age10, 0m, 38.8m, false, true, Drama),
            CreateMovie(18, "Hızlı Teslimat", "Express Delivery", "İki kurye yanlış paketi doğru adrese ulaştırmak için şehri baştan sona geçer.", new DateOnly(2026, 8, 28), 94, AgeRating.Age10, 0m, 36.1m, false, true, Action, Comedy),
            CreateMovie(19, "Mercan Şarkısı", "Song of Coral", "Deniz altındaki müzik festivaline katılan genç bir balığın cesaret hikâyesi.", new DateOnly(2026, 9, 25), 89, AgeRating.GeneralAudience, 0m, 33.7m, false, true, Animation, Family),
            CreateMovie(20, "Kuzey Ekspresi", "Northern Express", "Uzun bir tren yolculuğunda başlayan sohbet iki yabancının hayatına yeni bir yön verir.", new DateOnly(2026, 10, 2), 108, AgeRating.Age13, 0m, 31.4m, false, true, Drama, Romance),
        };

        dbContext.Genres.AddRange(genres);
        dbContext.Movies.AddRange(movies);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Movie CreateMovie(
        int number,
        string title,
        string originalTitle,
        string overview,
        DateOnly releaseDate,
        int runtimeMinutes,
        AgeRating ageRating,
        decimal voteAverage,
        decimal popularity,
        bool isNowPlaying,
        bool isUpcoming,
        params Guid[] genreIds)
    {
        var id = Guid.Parse($"20000000-0000-0000-0000-{number:D12}");
        var movie = new Movie(
            id,
            "mock",
            $"mock-{number:D3}",
            title,
            originalTitle,
            overview,
            releaseDate,
            runtimeMinutes,
            "tr",
            ageRating,
            voteAverage,
            voteAverage == 0 ? 0 : 250 + (number * 37),
            popularity,
            isNowPlaying,
            isUpcoming,
            new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero));

        foreach (var genreId in genreIds)
        {
            movie.MovieGenres.Add(new MovieGenre(id, genreId));
        }

        return movie;
    }
}
