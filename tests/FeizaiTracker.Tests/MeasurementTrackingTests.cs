using FeizaiTracker.App.Models;
using FeizaiTracker.App.Services;
using FeizaiTracker.App.ViewModels;

namespace FeizaiTracker.Tests;

public sealed class MeasurementTrackingTests
{
    [Fact]
    public async Task ViewModel_BuildsWeightTrendAndOffersAllChartTypes()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"FeizaiTracker.Tests.{Guid.NewGuid():N}",
            "data.json");
        var state = new AppState(new JsonAppDataStore(path));
        await state.InitializeAsync();
        await state.UpsertMeasurementEntryAsync(new MeasurementEntry
        {
            MetricId = MeasurementDefinition.WeightId,
            LoggedAt = DateTime.Today.AddDays(-1),
            Value = 82.4m
        });
        await state.UpsertMeasurementEntryAsync(new MeasurementEntry
        {
            MetricId = MeasurementDefinition.WeightId,
            LoggedAt = DateTime.Today,
            Value = 81.9m
        });
        var viewModel = new MeasurementsViewModel(state);

        viewModel.Initialize();

        Assert.Equal("Weight", viewModel.SelectedMetric?.Name);
        Assert.Equal("81.9 kg", viewModel.LatestValueLabel);
        Assert.Equal("-0.5 kg", viewModel.ChangeLabel);
        Assert.Equal(2, viewModel.ChartPoints.Count);
        Assert.Equal(
            [
                MeasurementChartType.Line,
                MeasurementChartType.Area,
                MeasurementChartType.Bar
            ],
            viewModel.ChartTypes);

        File.Delete(path);
        Directory.Delete(Path.GetDirectoryName(path)!);
    }

    [Fact]
    public async Task State_UpsertsDailyValuesAndCascadesCustomMetricDeletion()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"FeizaiTracker.Tests.{Guid.NewGuid():N}",
            "data.json");
        var state = new AppState(new JsonAppDataStore(path));
        await state.InitializeAsync();

        var weight = Assert.Single(state.MeasurementDefinitions);
        Assert.Equal(MeasurementDefinition.WeightId, weight.Id);
        await state.UpsertMeasurementEntryAsync(new MeasurementEntry
        {
            MetricId = weight.Id,
            LoggedAt = new DateTime(2026, 10, 17),
            Value = 82.4m
        });
        var firstId = Assert.Single(state.MeasurementEntries).Id;
        await state.UpsertMeasurementEntryAsync(new MeasurementEntry
        {
            MetricId = weight.Id,
            LoggedAt = new DateTime(2026, 10, 17, 18, 30, 0),
            Value = 81.9m
        });

        var updatedWeight = Assert.Single(state.MeasurementEntries);
        Assert.Equal(firstId, updatedWeight.Id);
        Assert.Equal(81.9m, updatedWeight.Value);
        Assert.Equal(new DateTime(2026, 10, 17), updatedWeight.LoggedAt);

        var biceps = new MeasurementDefinition
        {
            Id = "custom:biceps",
            Name = "Biceps",
            Unit = "cm"
        };
        await state.AddMeasurementDefinitionAsync(biceps);
        await state.UpsertMeasurementEntryAsync(new MeasurementEntry
        {
            MetricId = biceps.Id,
            LoggedAt = new DateTime(2026, 10, 18),
            Value = 36.25m
        });

        var reloaded = new AppState(new JsonAppDataStore(path));
        await reloaded.InitializeAsync();
        Assert.Equal(2, reloaded.MeasurementDefinitions.Count);
        Assert.Equal(2, reloaded.MeasurementEntries.Count);

        await reloaded.RemoveMeasurementDefinitionAsync(biceps.Id);

        Assert.DoesNotContain(
            reloaded.MeasurementDefinitions,
            metric => metric.Id == biceps.Id);
        Assert.DoesNotContain(
            reloaded.MeasurementEntries,
            entry => entry.MetricId == biceps.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => reloaded.RemoveMeasurementDefinitionAsync(MeasurementDefinition.WeightId));

        File.Delete(path);
        Directory.Delete(Path.GetDirectoryName(path)!);
    }
}
