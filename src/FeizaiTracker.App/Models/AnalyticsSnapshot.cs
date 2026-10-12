namespace FeizaiTracker.App.Models;

public sealed record AnalyticsSnapshot(
    decimal AverageCalories,
    int TrackedDays,
    int GoalDays,
    int CurrentStreak,
    decimal HighestCalories,
    IReadOnlyList<ChartPoint> Points,
    IReadOnlyList<DailySummary> Days);
