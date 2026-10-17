namespace FeizaiTracker.App.Models;

public sealed class WaterLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime LoggedAt { get; set; } = DateTime.Now;

    public int AmountMillilitres { get; set; }

    public string AmountLabel => $"{AmountMillilitres:N0} mL";
}
