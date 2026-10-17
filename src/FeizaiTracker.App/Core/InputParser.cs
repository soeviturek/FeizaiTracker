using System.Globalization;
using System.Text.RegularExpressions;

namespace FeizaiTracker.App;

public static partial class InputParser
{
    public static bool TryDecimal(string? input, out decimal value)
    {
        var styles = NumberStyles.Number;
        return decimal.TryParse(input, styles, CultureInfo.CurrentCulture, out value) ||
            decimal.TryParse(input, styles, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryPositiveDecimal(string? input, out decimal value)
    {
        return TryDecimal(input, out value) && value > 0;
    }

    public static decimal ParseServingAmount(string? servingSize, decimal fallback = 100)
    {
        if (string.IsNullOrWhiteSpace(servingSize))
        {
            return fallback;
        }

        var match = ServingAmountRegex().Match(servingSize);
        if (!match.Success ||
            !decimal.TryParse(
                match.Groups[1].Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount) ||
            amount <= 0)
        {
            return fallback;
        }

        return amount;
    }

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(?:g|ml)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ServingAmountRegex();
}
