using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.Tests;

public sealed class FoodCatalogServiceTests
{
    [Fact]
    public async Task BundledCatalogue_LoadsAustralianFoodsAndSearchesByName()
    {
        var state = await CreateStateAsync();
        var catalog = new FoodCatalogService(state);

        await catalog.InitializeAsync();
        var results = catalog.Search("banana");

        Assert.True(catalog.AllFoods.Count >= 100);
        Assert.NotEmpty(catalog.Categories);
        Assert.Equal("Banana", results[0].Name);
        Assert.Equal("FSANZ AFCD", results[0].Source);

        Cleanup(state.DataPath);
    }

    [Fact]
    public async Task ImportedFood_IsIncludedWithoutDuplicatingItsId()
    {
        var state = await CreateStateAsync();
        var catalog = new FoodCatalogService(state);
        await catalog.InitializeAsync();
        var custom = new FoodItem
        {
            Id = "custom:test",
            Name = "Test smoothie",
            CaloriesPer100 = 80,
            Source = "User created"
        };

        await state.UpsertFoodAsync(custom);
        await state.UpsertFoodAsync(custom);

        Assert.Single(catalog.Search("Test smoothie"));

        Cleanup(state.DataPath);
    }

    private static async Task<AppState> CreateStateAsync()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"FeizaiTracker.Tests.{Guid.NewGuid():N}",
            "data.json");
        var state = new AppState(new JsonAppDataStore(path));
        await state.InitializeAsync();
        return state;
    }

    private static void Cleanup(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory);
        }
    }
}
