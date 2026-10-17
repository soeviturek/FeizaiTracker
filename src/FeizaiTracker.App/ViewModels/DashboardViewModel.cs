using System.Collections.ObjectModel;
using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly RelayCommand _nextDayCommand;
    private readonly AsyncRelayCommand _undoLastWaterCommand;
    private DateTime _selectedDate = DateTime.Today;
    private decimal _totalCalories;
    private decimal _protein;
    private decimal _carbohydrates;
    private decimal _fat;
    private int _totalWaterMillilitres;
    private WaterLogEntry? _latestWaterEntry;
    private IReadOnlyList<ChartPoint> _weekPoints = [];
    private string _quickCalories = string.Empty;
    private string _quickDescription = string.Empty;
    private MealType _selectedQuickMeal = MealType.Snack;
    private string _statusMessage = string.Empty;
    private string _waterAmount = "250";
    private string _waterStatusMessage = string.Empty;

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
        QuickAddWaterCommand = new AsyncRelayCommand(AddQuickWaterAsync);
        AddWaterCommand = new AsyncRelayCommand(AddCustomWaterAsync);
        _undoLastWaterCommand = new AsyncRelayCommand(
            RemoveLatestWaterAsync,
            () => LatestWaterEntry is not null);
        UndoLastWaterCommand = _undoLastWaterCommand;
    }

    public ObservableCollection<FoodLogEntry> Entries { get; } = [];

    public IReadOnlyList<MealType> Meals { get; } = Enum.GetValues<MealType>();

    public RelayCommand PreviousDayCommand { get; }

    public RelayCommand NextDayCommand { get; }

    public RelayCommand TodayCommand { get; }

    public AsyncRelayCommand QuickAddCommand { get; }

    public AsyncRelayCommand RemoveEntryCommand { get; }

    public AsyncRelayCommand QuickAddWaterCommand { get; }

    public AsyncRelayCommand AddWaterCommand { get; }

    public AsyncRelayCommand UndoLastWaterCommand { get; }

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

    public int TotalWaterMillilitres
    {
        get => _totalWaterMillilitres;
        private set
        {
            if (SetProperty(ref _totalWaterMillilitres, value))
            {
                OnPropertyChanged(nameof(WaterSummaryLabel));
                OnPropertyChanged(nameof(WaterRemainingLabel));
                OnPropertyChanged(nameof(WaterProgress));
            }
        }
    }

    public int DailyWaterTarget => _state.Settings.DailyWaterTargetMillilitres;

    public string WaterSummaryLabel =>
        $"{TotalWaterMillilitres:N0} / {DailyWaterTarget:N0} mL";

    public string WaterRemainingLabel
    {
        get
        {
            var difference = DailyWaterTarget - TotalWaterMillilitres;
            return difference switch
            {
                > 0 => $"{difference:N0} mL to goal",
                < 0 => $"{Math.Abs(difference):N0} mL over goal",
                _ => "Daily water goal reached"
            };
        }
    }

    public decimal WaterProgress => Progress(TotalWaterMillilitres, DailyWaterTarget);

    public WaterLogEntry? LatestWaterEntry
    {
        get => _latestWaterEntry;
        private set
        {
            if (SetProperty(ref _latestWaterEntry, value))
            {
                OnPropertyChanged(nameof(LatestWaterLabel));
                OnPropertyChanged(nameof(HasWaterEntries));
                _undoLastWaterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LatestWaterLabel => LatestWaterEntry is null
        ? "No water logged for this day."
        : $"Last drink: {LatestWaterEntry.AmountLabel} at {LatestWaterEntry.LoggedAt:t}";

    public bool HasWaterEntries => LatestWaterEntry is not null;

    public string WaterAmount
    {
        get => _waterAmount;
        set => SetProperty(ref _waterAmount, value);
    }

    public string WaterStatusMessage
    {
        get => _waterStatusMessage;
        private set
        {
            if (SetProperty(ref _waterStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasWaterStatusMessage));
            }
        }
    }

    public bool HasWaterStatusMessage => !string.IsNullOrWhiteSpace(WaterStatusMessage);

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

        var dayWaterEntries = _state.WaterEntries
            .Where(entry => entry.LoggedAt.Date == SelectedDate.Date)
            .OrderByDescending(entry => entry.LoggedAt)
            .ToList();
        TotalWaterMillilitres = dayWaterEntries.Sum(entry => entry.AmountMillilitres);
        LatestWaterEntry = dayWaterEntries.FirstOrDefault();

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
        OnPropertyChanged(nameof(DailyWaterTarget));
        OnPropertyChanged(nameof(WaterSummaryLabel));
        OnPropertyChanged(nameof(WaterRemainingLabel));
        OnPropertyChanged(nameof(WaterProgress));
        OnPropertyChanged(nameof(HasEntries));
    }

    private void ChangeDate(int days)
    {
        SelectedDate = SelectedDate.AddDays(days);
        StatusMessage = string.Empty;
        WaterStatusMessage = string.Empty;
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

    private Task AddQuickWaterAsync(object? parameter)
    {
        if (parameter is null ||
            !int.TryParse(parameter.ToString(), out var amountMillilitres))
        {
            WaterStatusMessage = "Choose a valid water amount.";
            return Task.CompletedTask;
        }

        return AddWaterAsync(amountMillilitres);
    }

    private async Task AddCustomWaterAsync()
    {
        if (!int.TryParse(WaterAmount, out var amountMillilitres) ||
            amountMillilitres is < 1 or > 5000)
        {
            WaterStatusMessage = "Enter a water amount between 1 and 5,000 mL.";
            return;
        }

        await AddWaterAsync(amountMillilitres);
    }

    private async Task AddWaterAsync(int amountMillilitres)
    {
        var loggedAt = IsToday
            ? DateTime.Now
            : SelectedDate.AddHours(12);

        try
        {
            await _state.AddWaterEntryAsync(new WaterLogEntry
            {
                LoggedAt = loggedAt,
                AmountMillilitres = amountMillilitres
            });
            WaterStatusMessage = $"{amountMillilitres:N0} mL water added.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            WaterStatusMessage = $"Could not save the water entry: {exception.Message}";
        }
    }

    private async Task RemoveLatestWaterAsync()
    {
        var entry = LatestWaterEntry;
        if (entry is null)
        {
            return;
        }

        try
        {
            await _state.RemoveWaterEntryAsync(entry.Id);
            WaterStatusMessage = $"{entry.AmountMillilitres:N0} mL water entry removed.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            WaterStatusMessage = $"Could not remove the water entry: {exception.Message}";
        }
    }

    private static decimal Progress(decimal value, decimal target) =>
        target <= 0 ? 0 : Math.Min(100, Math.Round(value / target * 100));
}
