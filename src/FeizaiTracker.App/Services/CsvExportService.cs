using System.Globalization;
using System.Text;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public static class CsvExportService
{
    public static async Task ExportAsync(
        string path,
        IEnumerable<FoodLogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "Date,Time,Meal,Food,Brand,Amount,Unit,Calories,Protein (g),Carbohydrates (g),Fat (g)");

        foreach (var entry in entries.OrderBy(item => item.LoggedAt))
        {
            builder.AppendLine(string.Join(',',
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
                Format(entry.Fat)));
        }

        await File.WriteAllTextAsync(path, builder.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static string Format(decimal value) =>
        value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";
}
