using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.Tests;

public sealed class CsvExportServiceTests
{
    [Fact]
    public async Task Export_QuotesTextAndUsesInvariantNumbers()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"feizai-export-{Guid.NewGuid():N}.csv");
        var entries = new[]
        {
            new FoodLogEntry
            {
                LoggedAt = new DateTime(2026, 10, 13, 12, 30, 0),
                Meal = MealType.Lunch,
                FoodName = "Rice, cooked",
                Brand = "Home \"kitchen\"",
                Amount = 150.5m,
                Calories = 195.7m
            }
        };

        await CsvExportService.ExportAsync(path, entries);
        var csv = await File.ReadAllTextAsync(path);

        Assert.Contains("\"Rice, cooked\"", csv);
        Assert.Contains("\"Home \"\"kitchen\"\"\"", csv);
        Assert.Contains(",150.5,", csv);
        Assert.Contains(",195.7,", csv);

        File.Delete(path);
    }
}
