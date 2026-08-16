using System.Globalization;
using System.Text.RegularExpressions;
using CinePick.Application.Recommendations;

namespace CinePick.Infrastructure.Recommendations;

internal sealed partial class MockRecommendationRequestParser(TimeProvider timeProvider)
    : IRecommendationRequestParser
{
    private static readonly TimeZoneInfo Istanbul =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public RecommendationFilter Parse(string text)
    {
        var normalized = text.Trim().ToLower(new CultureInfo("tr-TR"));
        var now = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(now, Istanbul);
        var day = normalized.Contains("yarın", StringComparison.Ordinal)
            ? DateOnly.FromDateTime(localNow.Date).AddDays(1)
            : DateOnly.FromDateTime(localNow.Date);
        var hasSpecificDay = normalized.Contains("yarın", StringComparison.Ordinal)
            || normalized.Contains("bugün", StringComparison.Ordinal)
            || normalized.Contains("bu akşam", StringComparison.Ordinal);
        var hourMatch = HourPattern().Match(normalized);
        var hour = hourMatch.Success ? int.Parse(hourMatch.Groups[1].Value, CultureInfo.InvariantCulture)
            : normalized.Contains("akşam", StringComparison.Ordinal) ? 18 : 0;
        var minute = hourMatch.Success ? int.Parse(hourMatch.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        var startsFrom = hasSpecificDay
            ? ToUtc(day, new TimeOnly(hour, minute))
            : now;
        var startsBefore = hasSpecificDay ? ToUtc(day.AddDays(1), TimeOnly.MinValue) : now.AddDays(7);

        int? maximumRuntime = null;
        if (normalized.Contains("iki saatten kısa", StringComparison.Ordinal)
            || normalized.Contains("2 saatten kısa", StringComparison.Ordinal)) maximumRuntime = 120;
        var runtimeMatch = RuntimePattern().Match(normalized);
        if (runtimeMatch.Success)
            maximumRuntime = int.Parse(runtimeMatch.Groups[1].Value, CultureInfo.InvariantCulture);

        decimal? maximumPrice = null;
        var priceMatch = PricePattern().Match(normalized);
        if (priceMatch.Success)
            maximumPrice = decimal.Parse(priceMatch.Groups[1].Value, CultureInfo.InvariantCulture);

        return new RecommendationFilter(startsFrom, startsBefore, maximumRuntime,
            Find(normalized, ["bilim-kurgu", "animasyon", "aksiyon", "komedi", "gerilim", "romantik", "dram", "aile"]),
            Find(normalized, ["istanbul", "ankara", "izmir"]),
            Find(normalized, ["kadikoy", "besiktas", "sisli", "cankaya", "yenimahalle", "kecioren", "konak", "karsiyaka", "bornova"]),
            maximumPrice,
            normalized.Contains("türkçe", StringComparison.Ordinal) ? "tr"
                : normalized.Contains("ingilizce", StringComparison.Ordinal) ? "en" : null,
            normalized.Contains("imax", StringComparison.Ordinal) ? "IMAX" : null);
    }

    private static string? Find(string text, IReadOnlyList<string> values)
    {
        var slug = ToSlug(text);
        return values.FirstOrDefault(value => slug.Contains(value, StringComparison.Ordinal));
    }

    private static string ToSlug(string value) => value.Replace('ı', 'i').Replace('ş', 's')
        .Replace('ç', 'c').Replace('ö', 'o').Replace('ü', 'u').Replace('ğ', 'g').Replace(' ', '-');

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Istanbul), TimeSpan.Zero);
    }

    [GeneratedRegex(@"\b([01]?\d|2[0-3])[\.:]([0-5]\d)\b", RegexOptions.CultureInvariant)]
    private static partial Regex HourPattern();
    [GeneratedRegex(@"\b(\d{2,3})\s*dakika", RegexOptions.CultureInvariant)]
    private static partial Regex RuntimePattern();
    [GeneratedRegex(@"\b(\d{2,4})\s*(?:tl|lira)", RegexOptions.CultureInvariant)]
    private static partial Regex PricePattern();
}
