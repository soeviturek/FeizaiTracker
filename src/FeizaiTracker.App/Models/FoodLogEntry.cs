namespace FeizaiTracker.App.Models;

public sealed class FoodLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FoodId { get; set; } = string.Empty;

    public string FoodName { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public MealType Meal { get; set; }

    public DateTime LoggedAt { get; set; } = DateTime.Now;

    public decimal Amount { get; set; }

    public string MeasureUnit { get; set; } = "g";

    public decimal Calories { get; set; }

    public decimal Protein { get; set; }

    public decimal Carbohydrates { get; set; }

    public decimal Fat { get; set; }

    public string AmountLabel => Amount <= 0
        ? string.Empty
        : $"{Amount:0.#} {MeasureUnit}";
}
