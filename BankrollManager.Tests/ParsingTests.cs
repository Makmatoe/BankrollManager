using System.Globalization;
using BankrollManager.Core.Formatting;

namespace BankrollManager.Tests;

[TestClass]
public sealed class ParsingTests
{
    [TestMethod]
    public void MoneyParserHandlesUiAndCsvCurrencyText()
    {
        Assert.IsTrue(MoneyParser.TryParse("\u20ac13,90", out var euroComma, "\u20ac", CultureInfo.InvariantCulture));
        Assert.AreEqual(13.90m, euroComma);

        Assert.IsTrue(MoneyParser.TryParse("-EUR7.20", out var euroDot, "\u20ac", CultureInfo.InvariantCulture));
        Assert.AreEqual(-7.20m, euroDot);

        Assert.IsTrue(MoneyParser.TryParse("1.2345", out var preciseDecimal, "\u20ac", CultureInfo.InvariantCulture));
        Assert.AreEqual(1.2345m, preciseDecimal);
    }

    [TestMethod]
    public void MoneyFormatterKeepsSignBeforeCurrencySymbol()
    {
        Assert.AreEqual("-\u20ac7.20", MoneyFormatter.Format(-7.20m, "\u20ac", CultureInfo.InvariantCulture));
        Assert.AreEqual("7.20", MoneyFormatter.Format(7.20m, culture: CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void DateParserAcceptsCsvAndPastedDateFormats()
    {
        Assert.AreEqual(
            new DateOnly(2026, 6, 10),
            BankrollDateParser.ParseDateOrDefault("2026-06-10", DateOnly.MinValue, CultureInfo.InvariantCulture));
        Assert.AreEqual(
            new DateOnly(2026, 6, 10),
            BankrollDateParser.ParseDateOrDefault("10-06-2026", DateOnly.MinValue, CultureInfo.InvariantCulture));
        Assert.AreEqual(new TimeOnly(9, 5), BankrollDateParser.ParseNullableTime("9:05", CultureInfo.InvariantCulture));
    }
}
