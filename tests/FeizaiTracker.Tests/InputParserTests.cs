using FeizaiTracker.App;

namespace FeizaiTracker.Tests;

public sealed class InputParserTests
{
    [Theory]
    [InlineData("33 g (2 biscuits)", 33)]
    [InlineData("250ml", 250)]
    [InlineData("1.5 g", 1.5)]
    [InlineData(null, 100)]
    [InlineData("one serve", 100)]
    public void ParseServingAmount_ReadsMetricAmount(string? input, decimal expected)
    {
        Assert.Equal(expected, InputParser.ParseServingAmount(input));
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData("0.5", true)]
    [InlineData("0", false)]
    [InlineData("-2", false)]
    [InlineData("food", false)]
    public void TryPositiveDecimal_ValidatesInput(string input, bool expected)
    {
        Assert.Equal(expected, InputParser.TryPositiveDecimal(input, out _));
    }
}
