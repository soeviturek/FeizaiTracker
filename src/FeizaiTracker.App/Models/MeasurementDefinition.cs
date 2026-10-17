namespace FeizaiTracker.App.Models;

public sealed class MeasurementDefinition
{
    public const string WeightId = "builtin:weight";

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static MeasurementDefinition CreateWeight() => new()
    {
        Id = WeightId,
        Name = "Weight",
        Unit = "kg",
        IsBuiltIn = true,
        CreatedAt = DateTime.UnixEpoch
    };
}
