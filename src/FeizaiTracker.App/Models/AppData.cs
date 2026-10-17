namespace FeizaiTracker.App.Models;

public sealed class AppData
{
    public int SchemaVersion { get; set; } = 3;

    public AppSettings Settings { get; set; } = new();

    public List<FoodItem> ImportedFoods { get; set; } = [];

    public List<FoodLogEntry> Entries { get; set; } = [];

    public List<WaterLogEntry> WaterEntries { get; set; } = [];

    public List<MeasurementDefinition> MeasurementDefinitions { get; set; } =
        [MeasurementDefinition.CreateWeight()];

    public List<MeasurementEntry> MeasurementEntries { get; set; } = [];
}
