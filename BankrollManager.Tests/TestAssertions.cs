namespace BankrollManager.Tests;

internal static class TestAssertions
{
    public static void AssertMoney(decimal expected, decimal actual)
    {
        Assert.AreEqual(expected, decimal.Round(actual, 2));
    }
}
