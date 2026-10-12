namespace FeizaiTracker.App.Models;

public sealed class AppData
{
    public int SchemaVersion { get; set; } = 1;

    public AppSettings Settings { get; set; } = new();

    public List<FoodItem> ImportedFoods { get; set; } = [];

    public List<FoodLogEntry> Entries { get; set; } = [];
}
