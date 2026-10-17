using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.Tests;

public sealed class JsonAppDataStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsAppData()
    {
        var path = NewPath();
        var store = new JsonAppDataStore(path);
        var data = new AppData
        {
            Settings = new AppSettings
            {
                DailyCalorieTarget = 2350,
                DailyWaterTargetMillilitres = 2800
            },
            Entries =
            [
                new FoodLogEntry
                {
                    FoodName = "Banana",
                    LoggedAt = new DateTime(2026, 10, 13, 8, 30, 0),
                    Calories = 105
                }
            ],
            WaterEntries =
            [
                new WaterLogEntry
                {
                    LoggedAt = new DateTime(2026, 10, 13, 9, 0, 0),
                    AmountMillilitres = 500
                }
            ],
            MeasurementDefinitions =
            [
                MeasurementDefinition.CreateWeight(),
                new MeasurementDefinition
                {
                    Id = "custom:biceps",
                    Name = "Biceps",
                    Unit = "cm"
                }
            ],
            MeasurementEntries =
            [
                new MeasurementEntry
                {
                    MetricId = "custom:biceps",
                    LoggedAt = new DateTime(2026, 10, 13),
                    Value = 36.25m
                }
            ]
        };

        await store.SaveAsync(data);
        var loaded = await store.LoadAsync();

        Assert.Equal(2350, loaded.Settings.DailyCalorieTarget);
        Assert.Equal(2800, loaded.Settings.DailyWaterTargetMillilitres);
        Assert.Single(loaded.Entries);
        Assert.Equal("Banana", loaded.Entries[0].FoodName);
        Assert.Single(loaded.WaterEntries);
        Assert.Equal(500, loaded.WaterEntries[0].AmountMillilitres);
        Assert.Equal(2, loaded.MeasurementDefinitions.Count);
        Assert.Equal(36.25m, Assert.Single(loaded.MeasurementEntries).Value);
        Assert.Equal(3, loaded.SchemaVersion);
        Assert.False(File.Exists(path + ".tmp"));

        File.Delete(path);
        Directory.Delete(Path.GetDirectoryName(path)!);
    }

    [Fact]
    public async Task Load_LegacyDataAddsTrackingDefaults()
    {
        var path = NewPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(
            path,
            """
            {
              "schemaVersion": 1,
              "settings": {
                "dailyCalorieTarget": 2100
              },
              "importedFoods": [],
              "entries": []
            }
            """);
        var store = new JsonAppDataStore(path);

        var loaded = await store.LoadAsync();

        Assert.Equal(3, loaded.SchemaVersion);
        Assert.Equal(2500, loaded.Settings.DailyWaterTargetMillilitres);
        Assert.Empty(loaded.WaterEntries);
        var weight = Assert.Single(loaded.MeasurementDefinitions);
        Assert.Equal(MeasurementDefinition.WeightId, weight.Id);
        Assert.Equal("kg", weight.Unit);
        Assert.True(weight.IsBuiltIn);
        Assert.Empty(loaded.MeasurementEntries);

        File.Delete(path);
        Directory.Delete(Path.GetDirectoryName(path)!);
    }

    [Fact]
    public async Task Load_InvalidJsonSurfacesDataError()
    {
        var path = NewPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{ not valid json");
        var store = new JsonAppDataStore(path);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            async () => await store.LoadAsync());

        Assert.Contains("has not been changed", exception.Message);

        File.Delete(path);
        Directory.Delete(Path.GetDirectoryName(path)!);
    }

    private static string NewPath() =>
        Path.Combine(
            Path.GetTempPath(),
            $"FeizaiTracker.Tests.{Guid.NewGuid():N}",
            "data.json");
}
