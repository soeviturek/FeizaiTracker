using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Services;

public sealed class OpenFoodFactsService
{
    private const string Fields =
        "code,product_name,brands,nutriments,serving_size,stores,countries_tags,image_front_small_url";

    private readonly HttpClient _httpClient;

    public OpenFoodFactsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<FoodItem>> SearchAsync(
        string query,
        string? retailer,
        CancellationToken cancellationToken = default)
    {
        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length < 2)
        {
            return [];
        }

        SearchResponse? response;
        if (trimmedQuery.All(char.IsDigit) && trimmedQuery.Length >= 8)
        {
            var productUrl =
                $"api/v2/product/{Uri.EscapeDataString(trimmedQuery)}?fields={Fields}";
            var productResponse = await _httpClient.GetFromJsonAsync<ProductResponse>(
                productUrl,
                cancellationToken);
            response = new SearchResponse
            {
                Products = productResponse?.Product is null
                    ? []
                    : [productResponse.Product]
            };
        }
        else
        {
            var queryParts = new List<string>
            {
                "action=process",
                "json=1",
                "page_size=30",
                "search_simple=1",
                $"search_terms={Uri.EscapeDataString(trimmedQuery)}",
                "tagtype_0=countries",
                "tag_contains_0=contains",
                "tag_0=australia",
                $"fields={Fields}"
            };

            if (!string.IsNullOrWhiteSpace(retailer) &&
                !string.Equals(retailer, "All retailers", StringComparison.OrdinalIgnoreCase))
            {
                queryParts.Add("tagtype_1=stores");
                queryParts.Add("tag_contains_1=contains");
                queryParts.Add($"tag_1={Uri.EscapeDataString(retailer)}");
            }

            response = await _httpClient.GetFromJsonAsync<SearchResponse>(
                "cgi/search.pl?" + string.Join('&', queryParts),
                cancellationToken);
        }

        return response?.Products
            .Select(Map)
            .Where(food => food is not null)
            .Cast<FoodItem>()
            .DistinctBy(food => food.Id)
            .Take(24)
            .ToList() ?? [];
    }

    private static FoodItem? Map(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Code) ||
            string.IsNullOrWhiteSpace(product.ProductName) ||
            product.Nutriments is null)
        {
            return null;
        }

        var calories = product.Nutriments.EnergyKcal100g;
        if (calories is null && product.Nutriments.EnergyKj100g is not null)
        {
            calories = (double)NutritionMath.KilojoulesToKilocalories(
                (decimal)product.Nutriments.EnergyKj100g.Value);
        }

        if (calories is null or < 0 or > 1000)
        {
            return null;
        }

        var servingAmount = InputParser.ParseServingAmount(product.ServingSize);
        var unit = product.ServingSize?.Contains("ml", StringComparison.OrdinalIgnoreCase) == true
            ? "mL"
            : "g";

        return new FoodItem
        {
            Id = $"off:{product.Code}",
            Name = product.ProductName.Trim(),
            Brand = product.Brands?.Trim() ?? string.Empty,
            Category = "Supermarket product",
            Stores = product.Stores?.Trim() ?? string.Empty,
            Barcode = product.Code,
            CaloriesPer100 = Math.Round((decimal)calories.Value, 1),
            ProteinPer100 = ToDecimal(product.Nutriments.Protein100g),
            CarbohydratesPer100 = ToDecimal(product.Nutriments.Carbohydrates100g),
            FatPer100 = ToDecimal(product.Nutriments.Fat100g),
            DefaultServingAmount = servingAmount,
            MeasureUnit = unit,
            ServingDescription = string.IsNullOrWhiteSpace(product.ServingSize)
                ? $"100 {unit}"
                : product.ServingSize,
            Source = "Open Food Facts",
            SourceUrl = $"https://world.openfoodfacts.org/product/{product.Code}",
            ImageUrl = product.ImageUrl?.Trim() ?? string.Empty
        };
    }

    private static decimal ToDecimal(double? value) =>
        value is null ? 0 : Math.Round((decimal)value.Value, 1);

    private sealed class SearchResponse
    {
        [JsonPropertyName("products")]
        public List<Product> Products { get; set; } = [];
    }

    private sealed class ProductResponse
    {
        [JsonPropertyName("product")]
        public Product? Product { get; set; }
    }

    private sealed class Product
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("serving_size")]
        public string? ServingSize { get; set; }

        [JsonPropertyName("stores")]
        public string? Stores { get; set; }

        [JsonPropertyName("image_front_small_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("nutriments")]
        public Nutriments? Nutriments { get; set; }
    }

    private sealed class Nutriments
    {
        [JsonPropertyName("energy-kcal_100g")]
        public double? EnergyKcal100g { get; set; }

        [JsonPropertyName("energy-kj_100g")]
        public double? EnergyKj100g { get; set; }

        [JsonPropertyName("proteins_100g")]
        public double? Protein100g { get; set; }

        [JsonPropertyName("carbohydrates_100g")]
        public double? Carbohydrates100g { get; set; }

        [JsonPropertyName("fat_100g")]
        public double? Fat100g { get; set; }
    }
}
