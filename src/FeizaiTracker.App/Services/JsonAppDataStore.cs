using System.Text.Json;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public sealed class JsonAppDataStore : IAppDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonAppDataStore(string dataPath)
    {
        DataPath = dataPath;
    }

    public string DataPath { get; }

    public static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FeizaiTracker", "data.json");
    }

    public async Task<AppData> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(DataPath))
        {
            return new AppData();
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.OpenRead(DataPath);
            var data = await JsonSerializer.DeserializeAsync<AppData>(
                stream,
                JsonOptions,
                cancellationToken);

            return data ?? throw new InvalidDataException(
                $"The data file at '{DataPath}' is empty or invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"The data file at '{DataPath}' could not be read. Your data has not been changed.",
                exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(DataPath)
                ?? throw new InvalidOperationException("The data path has no parent directory.");
            Directory.CreateDirectory(directory);

            var temporaryPath = DataPath + ".tmp";
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    data,
                    JsonOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, DataPath, true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
