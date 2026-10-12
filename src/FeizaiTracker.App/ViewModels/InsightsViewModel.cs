using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.App.ViewModels;

public sealed class InsightsViewModel : ObservableObject
{
    private readonly AppState _state;
    private RangeOption _selectedRange;
    private IReadOnlyList<ChartPoint> _chartPoints = [];
    private IReadOnlyList<DailySummary> _history = [];
    private decimal _averageCalories;
    private int _trackedDays;
    private int _goalDays;
    private int _currentStreak;
    private decimal _highestCalories;

    public InsightsViewModel(AppState state)
    {
        _state = state;
        _state.Changed += (_, _) => Refresh();
        RangeOptions =
        [
            new RangeOption("Last 7 days", 7),
            new RangeOption("Last 30 days", 30),
            new RangeOption("Last 90 days", 90)
        ];
        _selectedRange = RangeOptions[1];
    }

    public IReadOnlyList<RangeOption> RangeOptions { get; }

    public RangeOption SelectedRange
    {
        get => _selectedRange;
        set
        {
            if (SetProperty(ref _selectedRange, value))
            {
                Refresh();
            }
        }
    }

    public IReadOnlyList<ChartPoint> ChartPoints
    {
        get => _chartPoints;
        private set => SetProperty(ref _chartPoints, value);
    }

    public IReadOnlyList<DailySummary> History
    {
        get => _history;
        private set => SetProperty(ref _history, value);
    }

    public decimal AverageCalories
    {
        get => _averageCalories;
        private set => SetProperty(ref _averageCalories, value);
    }

    public int TrackedDays
    {
        get => _trackedDays;
        private set => SetProperty(ref _trackedDays, value);
    }

    public int GoalDays
    {
        get => _goalDays;
        private set => SetProperty(ref _goalDays, value);
    }

    public int CurrentStreak
    {
        get => _currentStreak;
        private set => SetProperty(ref _currentStreak, value);
    }

    public decimal HighestCalories
    {
        get => _highestCalories;
        private set => SetProperty(ref _highestCalories, value);
    }

    public int DailyTarget => _state.Settings.DailyCalorieTarget;

    public decimal Consistency =>
        SelectedRange.Days == 0
            ? 0
            : Math.Round((decimal)TrackedDays / SelectedRange.Days * 100);

    public void Initialize() => Refresh();

    public void Refresh()
    {
        var snapshot = NutritionMath.BuildAnalytics(
            _state.Entries,
            DateTime.Today,
            SelectedRange.Days,
            DailyTarget);

        ChartPoints = snapshot.Points;
        History = snapshot.Days
            .Where(day => day.EntryCount > 0)
            .OrderByDescending(day => day.Date)
            .ToList();
        AverageCalories = snapshot.AverageCalories;
        TrackedDays = snapshot.TrackedDays;
        GoalDays = snapshot.GoalDays;
        CurrentStreak = snapshot.CurrentStreak;
        HighestCalories = snapshot.HighestCalories;
        OnPropertyChanged(nameof(DailyTarget));
        OnPropertyChanged(nameof(Consistency));
        OnPropertyChanged(nameof(HasHistory));
    }

    public bool HasHistory => History.Count > 0;

    public sealed record RangeOption(string Label, int Days);
}
