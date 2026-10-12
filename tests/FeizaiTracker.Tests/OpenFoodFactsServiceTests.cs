using System.Net;
using FeizaiTracker.App.Services;

namespace FeizaiTracker.Tests;

public sealed class OpenFoodFactsServiceTests
{
    [Fact]
    public async Task Search_MapsUsableAustralianProductAndAppliesRetailerFilter()
    {
        const string json =
            """
            {
              "products": [
                {
                  "code": "9300652010794",
                  "product_name": "Weet-Bix",
                  "brands": "Sanitarium",
                  "serving_size": "33 g (2 biscuits)",
                  "stores": "Coles, Woolworths",
                  "nutriments": {
                    "energy-kcal_100g": 353.4,
                    "proteins_100g": 12.4,
                    "carbohydrates_100g": 65.9,
                    "fat_100g": 1.3
                  }
                },
                {
                  "code": "invalid",
                  "product_name": "",
                  "nutriments": {}
                }
              ]
            }
            """;
        var handler = new StubHandler(json);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/")
        };
        var service = new OpenFoodFactsService(client);

        var results = await service.SearchAsync("weet-bix", "Coles");

        var result = Assert.Single(results);
        Assert.Equal("Weet-Bix", result.Name);
        Assert.Equal(33m, result.DefaultServingAmount);
        Assert.Equal(116.6m, result.DefaultServingCalories);
        Assert.Equal("Open Food Facts", result.Source);
        Assert.Contains("tag_0=australia", handler.RequestUri);
        Assert.Contains("tag_1=Coles", handler.RequestUri);
    }

    [Fact]
    public async Task Search_ConvertsKilojoulesWhenCaloriesAreMissing()
    {
        const string json =
            """
            {
              "products": [
                {
                  "code": "1234567890123",
                  "product_name": "Test product",
                  "serving_size": "100 g",
                  "nutriments": {
                    "energy-kj_100g": 418.4
                  }
                }
              ]
            }
            """;
        var handler = new StubHandler(json);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/")
        };
        var service = new OpenFoodFactsService(client);

        var result = Assert.Single(await service.SearchAsync("test", null));

        Assert.Equal(100m, result.CaloriesPer100);
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        public string RequestUri { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString() ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        }
    }
}
