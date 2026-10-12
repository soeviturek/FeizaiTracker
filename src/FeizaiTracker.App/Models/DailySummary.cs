namespace FeizaiTracker.App.Models;

public sealed record DailySummary(
    DateTime Date,
    decimal Calories,
    int EntryCount,
    decimal Target)
{
    public string DateLabel => Date.ToString("ddd, d MMM");

    public decimal TargetPercentage =>
        Target <= 0 ? 0 : Math.Round(Calories / Target * 100);

    public string Status =>
        Calories == 0 ? "Not tracked" :
        Calories >= Target * 0.9m && Calories <= Target * 1.1m ? "On target" :
        Calories < Target * 0.9m ? "Below target" : "Above target";
}
