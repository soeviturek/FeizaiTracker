namespace FeizaiTracker.App.Models;

public sealed class MeasurementEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string MetricId { get; set; } = string.Empty;

    public DateTime LoggedAt { get; set; } = DateTime.Today;

    public decimal Value { get; set; }
}
