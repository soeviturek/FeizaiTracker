using System.Collections.ObjectModel;
using System.Windows;
using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.App.ViewModels;

public sealed class MeasurementsViewModel : ObservableObject
{
    private readonly AppState _state;
    private MeasurementDefinition? _selectedMetric;
    private MeasurementRangeOption _selectedRange;
    private MeasurementChartType _selectedChartType = MeasurementChartType.Line;
    private IReadOnlyList<ChartPoint> _chartPoints = [];
    private IReadOnlyList<MeasurementHistoryItem> _history = [];
    private DateTime? _entryDate = DateTime.Today;
    private string _entryValue = string.Empty;
    private string _entryStatusMessage = string.Empty;
    private string _customMetricName = string.Empty;
    private string _customMetricUnit = "cm";
    private string _customMetricStatusMessage = string.Empty;
    private string _latestValueLabel = "—";
    private string _changeLabel = "—";
    private string _minimumLabel = "—";
    private string _maximumLabel = "—";

    public MeasurementsViewModel(AppState state)
    {
        _state = state;
        _state.Changed += (_, _) => Refresh();

        RangeOptions =
        [
            new MeasurementRangeOption("Last 30 days", 30),
            new MeasurementRangeOption("Last 90 days", 90),
            new MeasurementRangeOption("Last year", 365),
            new MeasurementRangeOption("All time", 0)
        ];
        _selectedRange = RangeOptions[1];

        SaveEntryCommand = new AsyncRelayCommand(SaveEntryAsync);
        RemoveEntryCommand = new AsyncRelayCommand(RemoveEntryAsync);
        CreateMetricCommand = new AsyncRelayCommand(CreateMetricAsync);
        DeleteMetricCommand = new AsyncRelayCommand(DeleteSelectedMetricAsync);
    }

    public ObservableCollection<MeasurementDefinition> Metrics { get; } = [];

    public IReadOnlyList<MeasurementRangeOption> RangeOptions { get; }

    public IReadOnlyList<MeasurementChartType> ChartTypes { get; } =
        Enum.GetValues<MeasurementChartType>();

    public IReadOnlyList<string> UnitSuggestions { get; } =
        ["kg", "cm", "%", "count", "min", "km", "mmHg"];

    public AsyncRelayCommand SaveEntryCommand { get; }

    public AsyncRelayCommand RemoveEntryCommand { get; }

    public AsyncRelayCommand CreateMetricCommand { get; }

    public AsyncRelayCommand DeleteMetricCommand { get; }

    public MeasurementDefinition? SelectedMetric
    {
        get => _selectedMetric;
        set
        {
            if (SetProperty(ref _selectedMetric, value))
            {
                EntryValue = string.Empty;
                EntryStatusMessage = string.Empty;
                OnPropertyChanged(nameof(HasSelectedMetric));
                OnPropertyChanged(nameof(CanDeleteSelectedMetric));
                OnPropertyChanged(nameof(SelectedMetricUnit));
                RefreshSelectedMetric();
            }
        }
    }

    public bool HasSelectedMetric => SelectedMetric is not null;

    public bool CanDeleteSelectedMetric => SelectedMetric is { IsBuiltIn: false };

    public string SelectedMetricUnit => SelectedMetric?.Unit ?? string.Empty;

    public MeasurementRangeOption SelectedRange
    {
        get => _selectedRange;
        set
        {
            if (SetProperty(ref _selectedRange, value))
            {
                RefreshSelectedMetric();
            }
        }
    }

    public MeasurementChartType SelectedChartType
    {
        get => _selectedChartType;
        set => SetProperty(ref _selectedChartType, value);
    }

    public IReadOnlyList<ChartPoint> ChartPoints
    {
        get => _chartPoints;
        private set => SetProperty(ref _chartPoints, value);
    }

    public IReadOnlyList<MeasurementHistoryItem> History
    {
        get => _history;
        private set
        {
            if (SetProperty(ref _history, value))
            {
                OnPropertyChanged(nameof(HasHistory));
                OnPropertyChanged(nameof(HistoryCountLabel));
            }
        }
    }

    public bool HasHistory => History.Count > 0;

    public string HistoryCountLabel =>
        History.Count == 1 ? "1 entry" : $"{History.Count} entries";

    public DateTime? EntryDate
    {
        get => _entryDate;
        set => SetProperty(ref _entryDate, value);
    }

    public DateTime MinimumEntryDate { get; } = new(1900, 1, 1);

    public DateTime MaximumEntryDate => DateTime.Today;

    public string EntryValue
    {
        get => _entryValue;
        set => SetProperty(ref _entryValue, value);
    }

    public string EntryStatusMessage
    {
        get => _entryStatusMessage;
        private set
        {
            if (SetProperty(ref _entryStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasEntryStatusMessage));
            }
        }
    }

    public bool HasEntryStatusMessage => !string.IsNullOrWhiteSpace(EntryStatusMessage);

    public string CustomMetricName
    {
        get => _customMetricName;
        set => SetProperty(ref _customMetricName, value);
    }

    public string CustomMetricUnit
    {
        get => _customMetricUnit;
        set => SetProperty(ref _customMetricUnit, value);
    }

    public string CustomMetricStatusMessage
    {
        get => _customMetricStatusMessage;
        private set
        {
            if (SetProperty(ref _customMetricStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasCustomMetricStatusMessage));
            }
        }
    }

    public bool HasCustomMetricStatusMessage =>
        !string.IsNullOrWhiteSpace(CustomMetricStatusMessage);

    public string LatestValueLabel
    {
        get => _latestValueLabel;
        private set => SetProperty(ref _latestValueLabel, value);
    }

    public string ChangeLabel
    {
        get => _changeLabel;
        private set => SetProperty(ref _changeLabel, value);
    }

    public string MinimumLabel
    {
        get => _minimumLabel;
        private set => SetProperty(ref _minimumLabel, value);
    }

    public string MaximumLabel
    {
        get => _maximumLabel;
        private set => SetProperty(ref _maximumLabel, value);
    }

    public void Initialize() => Refresh();

    public void Refresh()
    {
        var selectedId = SelectedMetric?.Id ?? MeasurementDefinition.WeightId;
        Metrics.Clear();
        foreach (var metric in _state.MeasurementDefinitions
            .OrderByDescending(metric => metric.IsBuiltIn)
            .ThenBy(metric => metric.CreatedAt)
            .ThenBy(metric => metric.Name))
        {
            Metrics.Add(metric);
        }

        var nextSelection = Metrics.FirstOrDefault(metric =>
                string.Equals(metric.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            ?? Metrics.FirstOrDefault(metric =>
                string.Equals(
                    metric.Id,
                    MeasurementDefinition.WeightId,
                    StringComparison.OrdinalIgnoreCase))
            ?? Metrics.FirstOrDefault();

        if (!ReferenceEquals(SelectedMetric, nextSelection))
        {
            SelectedMetric = nextSelection;
        }
        else
        {
            RefreshSelectedMetric();
        }
    }

    private void RefreshSelectedMetric()
    {
        if (SelectedMetric is null)
        {
            ChartPoints = [];
            History = [];
            SetEmptyStatistics();
            return;
        }

        var cutoff = SelectedRange.Days > 0
            ? DateTime.Today.AddDays(-(SelectedRange.Days - 1))
            : DateTime.MinValue;
        var entries = _state.MeasurementEntries
            .Where(entry =>
                string.Equals(
                    entry.MetricId,
                    SelectedMetric.Id,
                    StringComparison.OrdinalIgnoreCase) &&
                entry.LoggedAt.Date >= cutoff)
            .OrderBy(entry => entry.LoggedAt)
            .ToList();

        ChartPoints = entries
            .Select(entry => new ChartPoint(
                entry.LoggedAt.Date,
                entry.Value,
                entry.LoggedAt.ToString("d MMM")))
            .ToList();
        History = entries
            .OrderByDescending(entry => entry.LoggedAt)
            .Select(entry => new MeasurementHistoryItem(
                entry,
                entry.LoggedAt.ToString("dddd, d MMM yyyy"),
                WithUnit(entry.Value, SelectedMetric.Unit)))
            .ToList();

        if (entries.Count == 0)
        {
            SetEmptyStatistics();
            return;
        }

        LatestValueLabel = WithUnit(entries[^1].Value, SelectedMetric.Unit);
        ChangeLabel = entries.Count < 2
            ? "—"
            : WithSignedUnit(entries[^1].Value - entries[^2].Value, SelectedMetric.Unit);
        MinimumLabel = WithUnit(entries.Min(entry => entry.Value), SelectedMetric.Unit);
        MaximumLabel = WithUnit(entries.Max(entry => entry.Value), SelectedMetric.Unit);
    }

    private async Task SaveEntryAsync()
    {
        if (SelectedMetric is null)
        {
            EntryStatusMessage = "Select a measurement first.";
            return;
        }

        if (EntryDate is null ||
            EntryDate.Value.Date > DateTime.Today ||
            EntryDate.Value.Date < new DateTime(1900, 1, 1))
        {
            EntryStatusMessage = "Choose a date between 1 Jan 1900 and today.";
            return;
        }

        if (!InputParser.TryDecimal(EntryValue, out var value) ||
            Math.Abs(value) > 1000000)
        {
            EntryStatusMessage = "Enter a value between -1,000,000 and 1,000,000.";
            return;
        }

        if (string.Equals(
                SelectedMetric.Id,
                MeasurementDefinition.WeightId,
                StringComparison.OrdinalIgnoreCase) &&
            value is <= 0 or > 1000)
        {
            EntryStatusMessage = "Enter a weight greater than 0 and up to 1,000 kg.";
            return;
        }

        var metric = SelectedMetric;
        var date = EntryDate.Value.Date;
        var isUpdate = _state.MeasurementEntries.Any(entry =>
            string.Equals(entry.MetricId, metric.Id, StringComparison.OrdinalIgnoreCase) &&
            entry.LoggedAt.Date == date);

        try
        {
            await _state.UpsertMeasurementEntryAsync(new MeasurementEntry
            {
                MetricId = metric.Id,
                LoggedAt = date,
                Value = Math.Round(value, 4)
            });
            EntryValue = string.Empty;
            EntryStatusMessage = isUpdate
                ? $"{metric.Name} updated for {date:d MMM yyyy}."
                : $"{metric.Name} added for {date:d MMM yyyy}.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            EntryStatusMessage = $"Could not save the measurement: {exception.Message}";
        }
    }

    private async Task RemoveEntryAsync(object? parameter)
    {
        if (parameter is not MeasurementHistoryItem historyItem)
        {
            return;
        }

        try
        {
            await _state.RemoveMeasurementEntryAsync(historyItem.Entry.Id);
            EntryStatusMessage = $"Entry for {historyItem.Entry.LoggedAt:d MMM yyyy} removed.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            EntryStatusMessage = $"Could not remove the measurement: {exception.Message}";
        }
    }

    private async Task CreateMetricAsync()
    {
        var name = CustomMetricName.Trim();
        var unit = CustomMetricUnit.Trim();
        if (name.Length is < 1 or > 40)
        {
            CustomMetricStatusMessage = "Enter a name between 1 and 40 characters.";
            return;
        }

        if (unit.Length is < 1 or > 12)
        {
            CustomMetricStatusMessage = "Enter a unit between 1 and 12 characters.";
            return;
        }

        if (_state.MeasurementDefinitions.Any(metric =>
            string.Equals(metric.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            CustomMetricStatusMessage = "A measurement with that name already exists.";
            return;
        }

        var definition = new MeasurementDefinition
        {
            Id = $"custom:{Guid.NewGuid():N}",
            Name = name,
            Unit = unit,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _state.AddMeasurementDefinitionAsync(definition);
            CustomMetricName = string.Empty;
            CustomMetricStatusMessage = $"{name} created.";
            SelectedMetric = Metrics.First(metric => metric.Id == definition.Id);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            CustomMetricStatusMessage =
                $"Could not create the measurement: {exception.Message}";
        }
    }

    private async Task DeleteSelectedMetricAsync()
    {
        var metric = SelectedMetric;
        if (metric is null || metric.IsBuiltIn)
        {
            return;
        }

        var entryCount = _state.MeasurementEntries.Count(entry =>
            string.Equals(entry.MetricId, metric.Id, StringComparison.OrdinalIgnoreCase));
        var result = MessageBox.Show(
            $"Delete {metric.Name} and its {entryCount} " +
            $"{(entryCount == 1 ? "entry" : "entries")}? This cannot be undone.",
            "Delete measurement",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _state.RemoveMeasurementDefinitionAsync(metric.Id);
            EntryStatusMessage = $"{metric.Name} deleted.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            EntryStatusMessage = $"Could not delete the measurement: {exception.Message}";
        }
    }

    private void SetEmptyStatistics()
    {
        LatestValueLabel = "—";
        ChangeLabel = "—";
        MinimumLabel = "—";
        MaximumLabel = "—";
    }

    private static string WithUnit(decimal value, string unit) =>
        $"{value:0.####} {unit}";

    private static string WithSignedUnit(decimal value, string unit) =>
        $"{value:+0.####;-0.####;0} {unit}";

    public sealed record MeasurementRangeOption(string Label, int Days);

    public sealed record MeasurementHistoryItem(
        MeasurementEntry Entry,
        string DateLabel,
        string ValueLabel);
}
