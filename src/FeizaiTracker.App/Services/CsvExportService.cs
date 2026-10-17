using System.Globalization;
using System.Text;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public static class CsvExportService
{
    public static async Task ExportAsync(
        string path,
        IEnumerable<FoodLogEntry> entries,
        IEnumerable<WaterLogEntry>? waterEntries = null,
        IEnumerable<MeasurementDefinition>? measurementDefinitions = null,
        IEnumerable<MeasurementEntry>? measurementEntries = null,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "Type,Date,Time,Meal,Food,Brand,Amount,Unit,Calories,Protein (g),Carbohydrates (g),Fat (g),Water (mL),Metric,Measurement Value,Measurement Unit");

        var foodRows = entries.Select(entry => new ExportRow(
            entry.LoggedAt,
            string.Join(',',
                Escape("Food"),
                Escape(entry.LoggedAt.ToString("yyyy-MM-dd")),
                Escape(entry.LoggedAt.ToString("HH:mm")),
                Escape(entry.Meal.ToString()),
                Escape(entry.FoodName),
                Escape(entry.Brand),
                Format(entry.Amount),
                Escape(entry.MeasureUnit),
                Format(entry.Calories),
                Format(entry.Protein),
                Format(entry.Carbohydrates),
                Format(entry.Fat),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty)));
        var waterRows = (waterEntries ?? Enumerable.Empty<WaterLogEntry>())
            .Select(entry => new ExportRow(
                entry.LoggedAt,
                string.Join(',',
                    Escape("Water"),
                    Escape(entry.LoggedAt.ToString("yyyy-MM-dd")),
                    Escape(entry.LoggedAt.ToString("HH:mm")),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    entry.AmountMillilitres.ToString(CultureInfo.InvariantCulture),
                    Escape("mL"),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    entry.AmountMillilitres.ToString(CultureInfo.InvariantCulture),
                    string.Empty,
                    string.Empty,
                    string.Empty)));
        var metricLookup = (measurementDefinitions ??
                Enumerable.Empty<MeasurementDefinition>())
            .GroupBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
        var measurementRows = (measurementEntries ?? Enumerable.Empty<MeasurementEntry>())
            .Select(entry =>
            {
                metricLookup.TryGetValue(entry.MetricId, out var definition);
                return new ExportRow(
                    entry.LoggedAt,
                    string.Join(',',
                        Escape("Measurement"),
                        Escape(entry.LoggedAt.ToString("yyyy-MM-dd")),
                        Escape(entry.LoggedAt.ToString("HH:mm")),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        Escape(definition?.Name ?? entry.MetricId),
                        FormatMeasurement(entry.Value),
                        Escape(definition?.Unit ?? string.Empty)));
            });

        foreach (var row in foodRows
            .Concat(waterRows)
            .Concat(measurementRows)
            .OrderBy(row => row.LoggedAt))
        {
            builder.AppendLine(row.Value);
        }

        await File.WriteAllTextAsync(path, builder.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static string Format(decimal value) =>
        value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string FormatMeasurement(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";

    private sealed record ExportRow(DateTime LoggedAt, string Value);
}
