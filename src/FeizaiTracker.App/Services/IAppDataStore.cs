using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public interface IAppDataStore
{
    string DataPath { get; }

    Task<AppData> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppData data, CancellationToken cancellationToken = default);
}
