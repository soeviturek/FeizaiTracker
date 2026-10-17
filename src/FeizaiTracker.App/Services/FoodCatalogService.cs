using System.Reflection;
using System.Text.Json;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public sealed class FoodCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppState _state;
    private List<FoodItem> _bundledFoods = [];

    public FoodCatalogService(AppState state)
    {
        _state = state;
    }

    public bool IsInitialized { get; private set; }

    public IReadOnlyList<string> Categories =>
        AllFoods
            .Select(food => food.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category)
            .ToList();

    public IReadOnlyList<FoodItem> AllFoods =>
        _state.ImportedFoods
            .Concat(_bundledFoods)
            .DistinctBy(food => food.Id)
            .ToList();

    public bool IsBundledFood(string foodId) =>
        _bundledFoods.Any(food =>
            string.Equals(food.Id, foodId, StringComparison.OrdinalIgnoreCase));

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name =>
                name.EndsWith("Data.common-foods.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The bundled food catalogue is missing.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The bundled food catalogue could not be opened.");
        _bundledFoods = await JsonSerializer.DeserializeAsync<List<FoodItem>>(
                stream,
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidDataException("The bundled food catalogue is invalid.");
        IsInitialized = true;
    }

    public IReadOnlyList<FoodItem> Search(
        string? query,
        string? category = null,
        int limit = 250)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var foods = AllFoods.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category) &&
            !string.Equals(category, "All categories", StringComparison.OrdinalIgnoreCase))
        {
            foods = foods.Where(food =>
                string.Equals(food.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (normalizedQuery.Length == 0)
        {
            return foods
                .OrderBy(food => food.Category)
                .ThenBy(food => food.Name)
                .Take(limit)
                .ToList();
        }

        return foods
            .Select(food => new { Food = food, Score = Score(food, normalizedQuery) })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Food.Name)
            .Take(limit)
            .Select(result => result.Food)
            .ToList();
    }

    private static int Score(FoodItem food, string query)
    {
        if (string.Equals(food.Name, query, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (string.Equals(food.Barcode, query, StringComparison.OrdinalIgnoreCase))
        {
            return 95;
        }

        if (food.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 80;
        }

        if (food.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 60;
        }

        if (food.Brand.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 45;
        }

        if (food.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            food.Stores.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        return 0;
    }
}
