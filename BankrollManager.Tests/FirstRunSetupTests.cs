using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.Tests;

[TestClass]
public sealed class FirstRunSetupTests
{
    [TestMethod]
    public void ShouldPromptForCleanUnconfiguredData()
    {
        var data = new BankrollData();

        Assert.IsTrue(FirstRunSetupService.ShouldPrompt(data));
    }

    [TestMethod]
    public void ShouldNotPromptAfterSetupOrWhenDataExists()
    {
        var completed = new BankrollData();
        FirstRunSetupService.Skip(completed);

        var existing = new BankrollData
        {
            LedgerEntries =
            [
                new LedgerEntry
                {
                    Type = LedgerType.Deposit,
                    Platform = Platform.Unibet,
                    Amount = 10m
                }
            ]
        };

        Assert.IsFalse(FirstRunSetupService.ShouldPrompt(completed));
        Assert.IsFalse(FirstRunSetupService.ShouldPrompt(existing));
    }

    [TestMethod]
    public void ApplySetupStoresSettingsDepositAndWalletBalances()
    {
        var data = new BankrollData();
        var setupDate = new DateOnly(2026, 6, 10);
        var options = new FirstRunSetupOptions
        {
            CurrencySymbol = "$",
            EnabledPlatforms = [Platform.Unibet, Platform.GGPoker],
            DefaultPlatform = Platform.GGPoker,
            FundingMode = FirstRunFundingMode.DepositEntry,
            FundingAmount = 25m,
            DepositPlatform = Platform.GGPoker,
            SetupDate = setupDate,
            PlatformBalances =
            {
                [Platform.GGPoker] = 25m,
                [Platform.Unibet] = 0m,
                [Platform.HollandCasino] = 99m
            }
        };

        FirstRunSetupService.Apply(data, options);

        Assert.IsTrue(data.Settings.FirstRunSetupCompleted);
        Assert.AreEqual("$", data.Settings.CurrencySymbol);
        CollectionAssert.AreEqual(new[] { Platform.Unibet, Platform.GGPoker }, data.Settings.EnabledPlatforms);
        Assert.AreEqual(Platform.GGPoker, data.Settings.DefaultPlatform);
        Assert.AreEqual(0m, data.Settings.StartingBankroll);
        Assert.AreEqual(25m, BankrollCalculator.CurrentBankroll(data));
        var ledgerEntry = data.LedgerEntries.Single();
        Assert.AreEqual(LedgerType.Deposit, ledgerEntry.Type);
        Assert.AreEqual(Platform.GGPoker, ledgerEntry.Platform);
        Assert.AreEqual(25m, ledgerEntry.Amount);

        var ggWallet = data.PlatformWallets.First(wallet => wallet.Platform == Platform.GGPoker);
        var unibetWallet = data.PlatformWallets.First(wallet => wallet.Platform == Platform.Unibet);
        var disabledWallet = data.PlatformWallets.First(wallet => wallet.Platform == Platform.HollandCasino);
        Assert.AreEqual(25m, ggWallet.ActualCashBalance);
        Assert.AreEqual(0m, unibetWallet.ActualCashBalance);
        Assert.IsNull(disabledWallet.ActualCashBalance);
        Assert.AreEqual(setupDate, ggWallet.LastUpdatedDate);
    }

    [TestMethod]
    public void ApplySetupCanUseStartingBankrollWithoutLedgerEntry()
    {
        var data = new BankrollData();

        FirstRunSetupService.Apply(data, new FirstRunSetupOptions
        {
            EnabledPlatforms = [Platform.HollandCasino],
            DefaultPlatform = Platform.HollandCasino,
            FundingMode = FirstRunFundingMode.StartingBankroll,
            FundingAmount = 40m,
            SetupDate = new DateOnly(2026, 6, 10)
        });

        Assert.AreEqual(40m, data.Settings.StartingBankroll);
        Assert.IsFalse(data.LedgerEntries.Any());
        Assert.AreEqual(40m, BankrollCalculator.CurrentBankroll(data));
    }
}
