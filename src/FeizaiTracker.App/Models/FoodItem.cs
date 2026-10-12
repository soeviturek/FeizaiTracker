namespace FeizaiTracker.App.Models;

public sealed class FoodItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Category { get; set; } = "Other";

    public string Stores { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal CaloriesPer100 { get; set; }

    public decimal ProteinPer100 { get; set; }

    public decimal CarbohydratesPer100 { get; set; }

    public decimal FatPer100 { get; set; }

    public decimal DefaultServingAmount { get; set; } = 100;

    public string MeasureUnit { get; set; } = "g";

    public string ServingDescription { get; set; } = "100 g";

    public string Source { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public decimal DefaultServingCalories =>
        NutritionMath.ForAmount(CaloriesPer100, DefaultServingAmount);

    public decimal DefaultServingKilojoules =>
        NutritionMath.KilocaloriesToKilojoules(DefaultServingCalories);

    public string DisplayBrand => string.IsNullOrWhiteSpace(Brand) ? Category : Brand;

    public string NutritionSummary =>
        $"{CaloriesPer100:0} kcal / 100 {MeasureUnit}  |  P {ProteinPer100:0.#}g  C {CarbohydratesPer100:0.#}g  F {FatPer100:0.#}g";
}
