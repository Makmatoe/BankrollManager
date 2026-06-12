using System.Globalization;
using BankrollManager.App.Forms;
using BankrollManager.Core.Models;

namespace BankrollManager.UiTests;

[TestClass]
public sealed class CashSessionDialogSupportTests
{
    [TestMethod]
    public void FormatStakesUsesCurrencySymbolAndBlindAmounts()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var settings = new BankrollSettings { CurrencySymbol = "$" };

            var stakes = CashSessionDialogSupport.FormatStakes(0.05m, 0.10m, settings);

            Assert.AreEqual("$0.05/$0.10", stakes);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void FormatStakesWaitsForBothBlinds()
    {
        var settings = new BankrollSettings { CurrencySymbol = "$" };

        Assert.AreEqual(string.Empty, CashSessionDialogSupport.FormatStakes(0m, 0.10m, settings));
        Assert.AreEqual(string.Empty, CashSessionDialogSupport.FormatStakes(0.05m, 0m, settings));
    }
}
