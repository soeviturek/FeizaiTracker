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

    public IReadOnlyList<WaterLogEntry> WaterEntries => Data.WaterEntries;

    public IReadOnlyList<MeasurementDefinition> MeasurementDefinitions =>
        Data.MeasurementDefinitions;

    public IReadOnlyList<MeasurementEntry> MeasurementEntries => Data.MeasurementEntries;

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

    public async Task AddWaterEntryAsync(
        WaterLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        Data.WaterEntries.Add(entry);
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.WaterEntries.Remove(entry);
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveWaterEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var index = Data.WaterEntries.FindIndex(entry => entry.Id == entryId);
        if (index < 0)
        {
            return;
        }

        var entry = Data.WaterEntries[index];
        Data.WaterEntries.RemoveAt(index);
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.WaterEntries.Insert(index, entry);
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddMeasurementDefinitionAsync(
        MeasurementDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (Data.MeasurementDefinitions.Any(item =>
            string.Equals(item.Id, definition.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A measurement with the ID '{definition.Id}' already exists.");
        }

        Data.MeasurementDefinitions.Add(definition);
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.MeasurementDefinitions.Remove(definition);
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveMeasurementDefinitionAsync(
        string metricId,
        CancellationToken cancellationToken = default)
    {
        var index = Data.MeasurementDefinitions.FindIndex(definition =>
            string.Equals(definition.Id, metricId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        var definition = Data.MeasurementDefinitions[index];
        if (definition.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in measurements cannot be deleted.");
        }

        var previousEntries = Data.MeasurementEntries.ToList();
        Data.MeasurementDefinitions.RemoveAt(index);
        Data.MeasurementEntries.RemoveAll(entry =>
            string.Equals(entry.MetricId, metricId, StringComparison.OrdinalIgnoreCase));
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.MeasurementDefinitions.Insert(index, definition);
            Data.MeasurementEntries.Clear();
            Data.MeasurementEntries.AddRange(previousEntries);
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpsertMeasurementEntryAsync(
        MeasurementEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!Data.MeasurementDefinitions.Any(definition =>
            string.Equals(definition.Id, entry.MetricId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"The measurement '{entry.MetricId}' does not exist.");
        }

        entry.LoggedAt = entry.LoggedAt.Date;
        var index = Data.MeasurementEntries.FindIndex(item =>
            string.Equals(item.MetricId, entry.MetricId, StringComparison.OrdinalIgnoreCase) &&
            item.LoggedAt.Date == entry.LoggedAt.Date);
        MeasurementEntry? previous = null;
        if (index >= 0)
        {
            previous = Data.MeasurementEntries[index];
            entry.Id = previous.Id;
            Data.MeasurementEntries[index] = entry;
        }
        else
        {
            Data.MeasurementEntries.Add(entry);
        }

        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            if (index >= 0 && previous is not null)
            {
                Data.MeasurementEntries[index] = previous;
            }
            else
            {
                Data.MeasurementEntries.Remove(entry);
            }

            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveMeasurementEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var index = Data.MeasurementEntries.FindIndex(entry => entry.Id == entryId);
        if (index < 0)
        {
            return;
        }

        var entry = Data.MeasurementEntries[index];
        Data.MeasurementEntries.RemoveAt(index);
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.MeasurementEntries.Insert(index, entry);
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

    public async Task UpdateDailyWaterTargetAsync(
        int targetMillilitres,
        CancellationToken cancellationToken = default)
    {
        var previous = Data.Settings.DailyWaterTargetMillilitres;
        Data.Settings.DailyWaterTargetMillilitres = targetMillilitres;
        try
        {
            await _store.SaveAsync(Data, cancellationToken);
        }
        catch
        {
            Data.Settings.DailyWaterTargetMillilitres = previous;
            throw;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
