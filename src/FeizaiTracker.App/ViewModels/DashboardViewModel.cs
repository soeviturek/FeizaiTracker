using System.Collections.ObjectModel;
using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly RelayCommand _nextDayCommand;
    private DateTime _selectedDate = DateTime.Today;
    private decimal _totalCalories;
    private decimal _protein;
    private decimal _carbohydrates;
    private decimal _fat;
    private IReadOnlyList<ChartPoint> _weekPoints = [];
    private string _quickCalories = string.Empty;
    private string _quickDescription = string.Empty;
    private MealType _selectedQuickMeal = MealType.Snack;
    private string _statusMessage = string.Empty;

    public DashboardViewModel(AppState state)
    {
        _state = state;
        _state.Changed += (_, _) => Refresh();

        PreviousDayCommand = new RelayCommand(() => ChangeDate(-1));
        _nextDayCommand = new RelayCommand(
            () => ChangeDate(1),
            () => SelectedDate.Date < DateTime.Today);
        NextDayCommand = _nextDayCommand;
        TodayCommand = new RelayCommand(() =>
        {
            SelectedDate = DateTime.Today;
            Refresh();
        });
        QuickAddCommand = new AsyncRelayCommand(AddQuickCaloriesAsync);
        RemoveEntryCommand = new AsyncRelayCommand(RemoveEntryAsync);
    }

    public ObservableCollection<FoodLogEntry> Entries { get; } = [];

    public IReadOnlyList<MealType> Meals { get; } = Enum.GetValues<MealType>();

    public RelayCommand PreviousDayCommand { get; }

    public RelayCommand NextDayCommand { get; }

    public RelayCommand TodayCommand { get; }

    public AsyncRelayCommand QuickAddCommand { get; }

    public AsyncRelayCommand RemoveEntryCommand { get; }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set
        {
            if (SetProperty(ref _selectedDate, value.Date))
            {
                OnPropertyChanged(nameof(DateLabel));
                OnPropertyChanged(nameof(IsToday));
                _nextDayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string DateLabel =>
        IsToday ? "Today" :
        SelectedDate.Date == DateTime.Today.AddDays(-1) ? "Yesterday" :
        SelectedDate.ToString("dddd, d MMMM");

    public bool IsToday => SelectedDate.Date == DateTime.Today;

    public decimal TotalCalories
    {
        get => _totalCalories;
        private set
        {
            if (SetProperty(ref _totalCalories, value))
            {
                OnPropertyChanged(nameof(RemainingCalories));
                OnPropertyChanged(nameof(RemainingLabel));
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    public int DailyTarget => _state.Settings.DailyCalorieTarget;

    public decimal RemainingCalories => Math.Abs(DailyTarget - TotalCalories);

    public string RemainingLabel => TotalCalories <= DailyTarget
        ? $"{RemainingCalories:0} kcal remaining"
        : $"{RemainingCalories:0} kcal over target";

    public decimal ProgressPercentage =>
        DailyTarget <= 0 ? 0 : Math.Round(TotalCalories / DailyTarget * 100);

    public decimal Protein
    {
        get => _protein;
        private set => SetProperty(ref _protein, value);
    }

    public decimal Carbohydrates
    {
        get => _carbohydrates;
        private set => SetProperty(ref _carbohydrates, value);
    }

    public decimal Fat
    {
        get => _fat;
        private set => SetProperty(ref _fat, value);
    }

    public decimal ProteinGoal => Math.Round(DailyTarget * 0.2m / 4m);

    public decimal CarbohydrateGoal => Math.Round(DailyTarget * 0.5m / 4m);

    public decimal FatGoal => Math.Round(DailyTarget * 0.3m / 9m);

    public decimal ProteinProgress => Progress(Protein, ProteinGoal);

    public decimal CarbohydrateProgress => Progress(Carbohydrates, CarbohydrateGoal);

    public decimal FatProgress => Progress(Fat, FatGoal);

    public IReadOnlyList<ChartPoint> WeekPoints
    {
        get => _weekPoints;
        private set => SetProperty(ref _weekPoints, value);
    }

    public string QuickCalories
    {
        get => _quickCalories;
        set => SetProperty(ref _quickCalories, value);
    }

    public string QuickDescription
    {
        get => _quickDescription;
        set => SetProperty(ref _quickDescription, value);
    }

    public MealType SelectedQuickMeal
    {
        get => _selectedQuickMeal;
        set => SetProperty(ref _selectedQuickMeal, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasEntries => Entries.Count > 0;

    public void Initialize() => Refresh();

    public void Refresh()
    {
        var dayEntries = _state.Entries
            .Where(entry => entry.LoggedAt.Date == SelectedDate.Date)
            .OrderByDescending(entry => entry.LoggedAt)
            .ToList();

        Entries.Clear();
        foreach (var entry in dayEntries)
        {
            Entries.Add(entry);
        }

        TotalCalories = dayEntries.Sum(entry => entry.Calories);
        Protein = dayEntries.Sum(entry => entry.Protein);
        Carbohydrates = dayEntries.Sum(entry => entry.Carbohydrates);
        Fat = dayEntries.Sum(entry => entry.Fat);

        var analytics = NutritionMath.BuildAnalytics(
            _state.Entries,
            SelectedDate,
            7,
            DailyTarget);
        WeekPoints = analytics.Points;

        OnPropertyChanged(nameof(DailyTarget));
        OnPropertyChanged(nameof(RemainingCalories));
        OnPropertyChanged(nameof(RemainingLabel));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(ProteinGoal));
        OnPropertyChanged(nameof(CarbohydrateGoal));
        OnPropertyChanged(nameof(FatGoal));
        OnPropertyChanged(nameof(ProteinProgress));
        OnPropertyChanged(nameof(CarbohydrateProgress));
        OnPropertyChanged(nameof(FatProgress));
        OnPropertyChanged(nameof(HasEntries));
    }

    private void ChangeDate(int days)
    {
        SelectedDate = SelectedDate.AddDays(days);
        StatusMessage = string.Empty;
        Refresh();
    }

    private async Task AddQuickCaloriesAsync()
    {
        if (!InputParser.TryPositiveDecimal(QuickCalories, out var calories) ||
            calories > 10000)
        {
            StatusMessage = "Enter calories between 1 and 10,000.";
            return;
        }

        var loggedAt = IsToday
            ? DateTime.Now
            : SelectedDate.AddHours(12);
        var description = string.IsNullOrWhiteSpace(QuickDescription)
            ? "Quick add"
            : QuickDescription.Trim();

        try
        {
            await _state.AddEntryAsync(new FoodLogEntry
            {
                FoodId = $"quick:{Guid.NewGuid():N}",
                FoodName = description,
                Meal = SelectedQuickMeal,
                LoggedAt = loggedAt,
                Amount = 1,
                MeasureUnit = "serve",
                Calories = Math.Round(calories, 1)
            });
            QuickCalories = string.Empty;
            QuickDescription = string.Empty;
            StatusMessage = $"{description} added.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save the entry: {exception.Message}";
        }
    }

    private async Task RemoveEntryAsync(object? parameter)
    {
        if (parameter is not FoodLogEntry entry)
        {
            return;
        }

        try
        {
            await _state.RemoveEntryAsync(entry.Id);
            StatusMessage = $"{entry.FoodName} removed.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not remove the entry: {exception.Message}";
        }
    }

    private static decimal Progress(decimal value, decimal target) =>
        target <= 0 ? 0 : Math.Min(100, Math.Round(value / target * 100));
}
