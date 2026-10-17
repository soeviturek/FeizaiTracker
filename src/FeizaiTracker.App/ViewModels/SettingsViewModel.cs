using System.Diagnostics;
using System.Reflection;
using FeizaiTracker.App.Services;
using Microsoft.Win32;

namespace FeizaiTracker.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppState _state;
    private string _dailyTarget = string.Empty;
    private string _dailyWaterTarget = string.Empty;
    private string _statusMessage = string.Empty;

    public SettingsViewModel(AppState state)
    {
        _state = state;
        SaveGoalCommand = new AsyncRelayCommand(SaveGoalAsync);
        SaveWaterGoalCommand = new AsyncRelayCommand(SaveWaterGoalAsync);
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenUrlCommand = new RelayCommand(OpenUrl);
    }

    public AsyncRelayCommand SaveGoalCommand { get; }

    public AsyncRelayCommand SaveWaterGoalCommand { get; }

    public AsyncRelayCommand ExportCsvCommand { get; }

    public RelayCommand OpenDataFolderCommand { get; }

    public RelayCommand OpenUrlCommand { get; }

    public string DailyTarget
    {
        get => _dailyTarget;
        set => SetProperty(ref _dailyTarget, value);
    }

    public string DailyWaterTarget
    {
        get => _dailyWaterTarget;
        set => SetProperty(ref _dailyWaterTarget, value);
    }

    public string DataPath => _state.DataPath;

    public string AppVersion =>
        $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0"}";

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

    public void Initialize() => Refresh();

    public void Refresh()
    {
        DailyTarget = _state.Settings.DailyCalorieTarget.ToString();
        DailyWaterTarget = _state.Settings.DailyWaterTargetMillilitres.ToString();
        OnPropertyChanged(nameof(DataPath));
    }

    private async Task SaveWaterGoalAsync()
    {
        if (!int.TryParse(DailyWaterTarget, out var target) || target is < 500 or > 10000)
        {
            StatusMessage = "Enter a daily water target between 500 and 10,000 mL.";
            return;
        }

        try
        {
            await _state.UpdateDailyWaterTargetAsync(target);
            StatusMessage = "Daily water target saved.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save the water target: {exception.Message}";
        }
    }

    private async Task SaveGoalAsync()
    {
        if (!int.TryParse(DailyTarget, out var target) || target is < 500 or > 10000)
        {
            StatusMessage = "Enter a daily target between 500 and 10,000 kcal.";
            return;
        }

        try
        {
            await _state.UpdateDailyTargetAsync(target);
            StatusMessage = "Daily target saved.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save the target: {exception.Message}";
        }
    }

    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export tracking history",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"feizai-tracker-{DateTime.Today:yyyy-MM-dd}.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await CsvExportService.ExportAsync(
                dialog.FileName,
                _state.Entries,
                _state.WaterEntries,
                _state.MeasurementDefinitions,
                _state.MeasurementEntries);
            StatusMessage =
                $"Exported {_state.Entries.Count} food, {_state.WaterEntries.Count} water " +
                $"and {_state.MeasurementEntries.Count} measurement entries.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Export failed: {exception.Message}";
        }
    }

    private void OpenDataFolder()
    {
        try
        {
            var folder = Path.GetDirectoryName(DataPath)
                ?? throw new InvalidOperationException("The data folder could not be resolved.");
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
            StatusMessage = "Opened the local data folder.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusMessage = $"Could not open the data folder: {exception.Message}";
        }
    }

    private void OpenUrl(object? parameter)
    {
        if (parameter is not string url ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http"))
        {
            StatusMessage = "The link is invalid.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            StatusMessage = $"Could not open the link: {exception.Message}";
        }
    }
}
