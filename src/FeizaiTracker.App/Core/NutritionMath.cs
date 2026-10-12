using FeizaiTracker.App.Models;

namespace FeizaiTracker.App;

public static class NutritionMath
{
    private const decimal KilojoulesPerKilocalorie = 4.184m;

    public static decimal ForAmount(decimal valuePer100, decimal amount) =>
        Math.Round(valuePer100 * amount / 100m, 1, MidpointRounding.AwayFromZero);

    public static decimal KilocaloriesToKilojoules(decimal kilocalories) =>
        Math.Round(kilocalories * KilojoulesPerKilocalorie, MidpointRounding.AwayFromZero);

    public static decimal KilojoulesToKilocalories(decimal kilojoules) =>
        Math.Round(kilojoules / KilojoulesPerKilocalorie, 1, MidpointRounding.AwayFromZero);

    public static AnalyticsSnapshot BuildAnalytics(
        IEnumerable<FoodLogEntry> entries,
        DateTime endDate,
        int dayCount,
        decimal dailyTarget)
    {
        if (dayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount));
        }

        var end = endDate.Date;
        var start = end.AddDays(-(dayCount - 1));
        var totals = entries
            .Where(entry => entry.LoggedAt.Date >= start && entry.LoggedAt.Date <= end)
            .GroupBy(entry => entry.LoggedAt.Date)
            .ToDictionary(group => group.Key, group => new
            {
                Calories = group.Sum(entry => entry.Calories),
                Count = group.Count()
            });

        var days = Enumerable.Range(0, dayCount)
            .Select(offset =>
            {
                var date = start.AddDays(offset);
                totals.TryGetValue(date, out var total);
                return new DailySummary(
                    date,
                    total?.Calories ?? 0,
                    total?.Count ?? 0,
                    dailyTarget);
            })
            .ToList();

        var tracked = days.Where(day => day.EntryCount > 0).ToList();
        var trackedDates = entries
            .Where(entry => entry.LoggedAt.Date <= end)
            .Select(entry => entry.LoggedAt.Date)
            .ToHashSet();
        var currentStreak = 0;
        for (var date = end; trackedDates.Contains(date); date = date.AddDays(-1))
        {
            currentStreak++;
        }

        var points = days
            .Select(day => new ChartPoint(day.Date, day.Calories, day.Date.ToString("ddd")[..1]))
            .ToList();

        return new AnalyticsSnapshot(
            tracked.Count == 0 ? 0 : Math.Round(tracked.Average(day => day.Calories)),
            tracked.Count,
            tracked.Count(day =>
                day.Calories >= dailyTarget * 0.9m &&
                day.Calories <= dailyTarget * 1.1m),
            currentStreak,
            tracked.Count == 0 ? 0 : tracked.Max(day => day.Calories),
            points,
            days);
    }
}
