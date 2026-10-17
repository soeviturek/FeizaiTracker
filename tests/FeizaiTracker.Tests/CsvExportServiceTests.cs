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
        var waterEntries = new[]
        {
            new WaterLogEntry
            {
                LoggedAt = new DateTime(2026, 10, 13, 14, 0, 0),
                AmountMillilitres = 500
            }
        };
        var measurementDefinitions = new[]
        {
            new MeasurementDefinition
            {
                Id = "custom:biceps",
                Name = "Biceps",
                Unit = "cm"
            }
        };
        var measurementEntries = new[]
        {
            new MeasurementEntry
            {
                MetricId = "custom:biceps",
                LoggedAt = new DateTime(2026, 10, 14),
                Value = 36.25m
            }
        };

        await CsvExportService.ExportAsync(
            path,
            entries,
            waterEntries,
            measurementDefinitions,
            measurementEntries);
        var csv = await File.ReadAllTextAsync(path);

        Assert.Contains("Type,Date,Time", csv);
        Assert.Contains("\"Food\"", csv);
        Assert.Contains("\"Rice, cooked\"", csv);
        Assert.Contains("\"Home \"\"kitchen\"\"\"", csv);
        Assert.Contains(",150.5,", csv);
        Assert.Contains(",195.7,", csv);
        Assert.Contains("\"Water\",\"2026-10-13\",\"14:00\"", csv);
        Assert.Contains(",500,\"mL\",", csv);
        Assert.Contains(
            "\"Measurement\",\"2026-10-14\",\"00:00\",,,,,,,,,,,\"Biceps\",36.25,\"cm\"",
            csv);

        File.Delete(path);
    }
}
