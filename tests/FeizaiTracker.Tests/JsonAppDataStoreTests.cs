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
            Settings = new AppSettings { DailyCalorieTarget = 2350 },
            Entries =
            [
                new FoodLogEntry
                {
                    FoodName = "Banana",
                    LoggedAt = new DateTime(2026, 10, 13, 8, 30, 0),
                    Calories = 105
                }
            ]
        };

        await store.SaveAsync(data);
        var loaded = await store.LoadAsync();

        Assert.Equal(2350, loaded.Settings.DailyCalorieTarget);
        Assert.Single(loaded.Entries);
        Assert.Equal("Banana", loaded.Entries[0].FoodName);
        Assert.False(File.Exists(path + ".tmp"));

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
