using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public sealed class AppState
{
    private readonly IAppDataStore _store;
    private AppData? _data;

    public AppState(IAppDataStore store)
    {
        _store = store;
    }

    public event EventHandler? Changed;

    public string DataPath => _store.DataPath;

    public AppSettings Settings => Data.Settings;

    public IReadOnlyList<FoodItem> ImportedFoods => Data.ImportedFoods;

    public IReadOnlyList<FoodLogEntry> Entries => Data.Entries;

    private AppData Data =>
        _data ?? throw new InvalidOperationException("App state has not been initialised.");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _data = await _store.LoadAsync(cancellationToken);
    }

    public async Task AddEntryAsync(
        FoodLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        Data.Entries.Add(entry);
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.Entries.Remove(entry);
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var index = Data.Entries.FindIndex(entry => entry.Id == entryId);
        if (index < 0)
        {
            return;
        }

        var entry = Data.Entries[index];
        Data.Entries.RemoveAt(index);
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.Entries.Insert(index, entry);
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpsertFoodAsync(
        FoodItem food,
        CancellationToken cancellationToken = default)
    {
        var index = Data.ImportedFoods.FindIndex(item => item.Id == food.Id);
        FoodItem? previous = null;
        if (index >= 0)
        {
            previous = Data.ImportedFoods[index];
            Data.ImportedFoods[index] = food;
        }
        else
        {
            Data.ImportedFoods.Add(food);
        }

        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            if (index >= 0 && previous is not null)
            {
                Data.ImportedFoods[index] = previous;
            }
            else
            {
                Data.ImportedFoods.Remove(food);
            }

            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateDailyTargetAsync(
        int target,
        CancellationToken cancellationToken = default)
    {
        var previous = Data.Settings.DailyCalorieTarget;
        Data.Settings.DailyCalorieTarget = target;
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.Settings.DailyCalorieTarget = previous;
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
