using FeizaiTracker.App;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.Tests;

public sealed class NutritionMathTests
{
    [Fact]
    public void ForAmount_ScalesAndRoundsToOneDecimalPlace()
    {
        var result = NutritionMath.ForAmount(165m, 150m);

        Assert.Equal(247.5m, result);
    }

    [Fact]
    public void EnergyConversion_RoundTripsWithinDisplayPrecision()
    {
        var kilojoules = NutritionMath.KilocaloriesToKilojoules(500m);
        var kilocalories = NutritionMath.KilojoulesToKilocalories(kilojoules);

        Assert.Equal(2092m, kilojoules);
        Assert.Equal(500m, kilocalories);
    }

    [Fact]
    public void BuildAnalytics_FillsMissingDaysAndCalculatesTrackedDayMetrics()
    {
        var endDate = new DateTime(2026, 10, 13);
        var entries = new[]
        {
            Entry(endDate.AddDays(-2), 1900),
            Entry(endDate.AddDays(-1), 900),
            Entry(endDate.AddDays(-1), 1000),
            Entry(endDate, 2000)
        };

        var result = NutritionMath.BuildAnalytics(entries, endDate, 7, 2000);

        Assert.Equal(7, result.Points.Count);
        Assert.Equal(3, result.TrackedDays);
        Assert.Equal(3, result.GoalDays);
        Assert.Equal(3, result.CurrentStreak);
        Assert.Equal(1933m, result.AverageCalories);
        Assert.Equal(2000m, result.HighestCalories);
        Assert.Equal(0m, result.Points[0].Value);
        Assert.Equal(2000m, result.Points[^1].Value);
    }

    [Fact]
    public void BuildAnalytics_RejectsNonPositiveRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionMath.BuildAnalytics([], DateTime.Today, 0, 2000));
    }

    private static FoodLogEntry Entry(DateTime date, decimal calories) =>
        new()
        {
            FoodName = "Test",
            LoggedAt = date,
            Calories = calories
        };
}
