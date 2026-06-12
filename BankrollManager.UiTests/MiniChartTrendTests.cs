using BankrollManager.App.Controls;

namespace BankrollManager.UiTests;

[TestClass]
public sealed class MiniChartTrendTests
{
    [TestMethod]
    public void CalculateTrendReturnsLinearRegressionForChartPoints()
    {
        var trend = MiniChart.CalculateTrend(
        [
            new MiniChartPoint("A", 1m),
            new MiniChartPoint("B", 3m),
            new MiniChartPoint("C", 5m)
        ]);

        Assert.IsNotNull(trend);
        Assert.AreEqual(1m, trend.Value.StartValue);
        Assert.AreEqual(5m, trend.Value.EndValue);
        Assert.AreEqual(2m, trend.Value.Slope);
    }

    [TestMethod]
    public void CalculateTrendRequiresAtLeastThreePoints()
    {
        var trend = MiniChart.CalculateTrend(
        [
            new MiniChartPoint("A", 1m),
            new MiniChartPoint("B", 3m)
        ]);

        Assert.IsNull(trend);
    }
}
