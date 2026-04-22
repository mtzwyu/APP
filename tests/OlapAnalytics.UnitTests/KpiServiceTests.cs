using OlapAnalytics.Application.Services;

namespace OlapAnalytics.UnitTests;

/// <summary>Tests for KpiService static calculation methods.</summary>
public class KpiServiceTests
{
    [Theory]
    [InlineData(110, 100, 10.00)]      // 10% growth
    [InlineData(100, 110, -9.09)]      // ~9.09% decline
    [InlineData(100, 0,   100.00)]     // Previous is 0 → 100%
    [InlineData(0,   100, -100.00)]    // Current is 0 → -100%
    [InlineData(100, 100, 0.00)]       // No change
    public void CalculateGrowthRate_ReturnsCorrectPercentage(
        decimal current, decimal previous, decimal expected)
    {
        var result = KpiService.CalculateGrowthRate(current, previous);
        Assert.Equal(expected, result, precision: 2);
    }

    [Fact]
    public void CalculateGrowthRate_BothZero_ReturnsZero()
    {
        var result = KpiService.CalculateGrowthRate(0, 0);
        Assert.Equal(0, result);
    }
}
