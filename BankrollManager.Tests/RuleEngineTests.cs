using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class RuleEngineTests
{
    [TestMethod]
    public void RuleEngineBlocksWhenDailyRiskCapWouldBeExceeded()
    {
        var today = new DateOnly(2026, 6, 9);
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 100m,
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m,
                DailyRiskCapPercent = 5m,
                ActiveExposureCapPercent = 100m
            },
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = today,
                    Category = TournamentCategory.MainGrind,
                    BuyIn = 4m,
                    ActualBullets = 1
                }
            ]
        };

        var result = RuleEngine.Evaluate(
            data,
            new DecisionRequest
            {
                Category = TournamentCategory.MainGrind,
                Format = TournamentFormat.MTT,
                BuyIn = 2m,
                PlannedBullets = 1
            },
            today);

        Assert.AreEqual(DecisionLabel.Pass, result.Label);
        StringAssert.Contains(result.Explanation, "Daily risk");
        Assert.IsTrue(result.Thresholds.Any(threshold => threshold.Contains("Daily risk", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RuleEngineReturnsReviewWhenPlayIsNearRiskCap()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 100m,
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m,
                DailyRiskCapPercent = 100m,
                ActiveExposureCapPercent = 100m,
                ReviewRiskCapUsagePercent = 80m
            }
        };

        var result = RuleEngine.Evaluate(
            data,
            new DecisionRequest
            {
                Category = TournamentCategory.MainGrind,
                Format = TournamentFormat.MTT,
                BuyIn = 2.00m,
                PlannedBullets = 1
            },
            new DateOnly(2026, 6, 9));

        Assert.AreEqual(DecisionLabel.Review, result.Label);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("normal cap", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RuleEngineBlocksShotBelowCategoryMinBankroll()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 50m,
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m,
                DailyRiskCapPercent = 100m,
                ActiveExposureCapPercent = 100m
            }
        };

        var result = RuleEngine.Evaluate(
            data,
            new DecisionRequest
            {
                Category = TournamentCategory.TowerShot,
                Format = TournamentFormat.Tower,
                BuyIn = 3m,
                PlannedBullets = 1
            },
            new DateOnly(2026, 6, 9));

        Assert.AreEqual(DecisionLabel.Pass, result.Label);
        StringAssert.Contains(result.Explanation, "requires at least");
    }

    [TestMethod]
    public void SettingsUpgradeAppliesGuideThresholdDefaultsForOlderProfiles()
    {
        var settings = new BankrollSettings
        {
            RuleProfileVersion = 0,
            ReviewRiskCapUsagePercent = 0m,
            BudgetWarningPercent = 0m,
            DailyRiskCapPercent = 0m,
            ActiveExposureCapPercent = 0m,
            StopLossWarningPercent = 0m,
            CashReloadWarningPercent = 0m
        };

        settings.EnsureDefaults();

        Assert.AreEqual(3, settings.RuleProfileVersion);
        AssertMoney(80m, settings.ReviewRiskCapUsagePercent);
        AssertMoney(20m, settings.BudgetWarningPercent);
        AssertMoney(12m, settings.DailyRiskCapPercent);
        AssertMoney(18m, settings.ActiveExposureCapPercent);
        AssertMoney(70m, settings.StopLossWarningPercent);
        AssertMoney(100m, settings.CashReloadWarningPercent);
    }

    [TestMethod]
    public void SettingsPreserveDisabledOptionalCapsForCurrentProfiles()
    {
        var settings = new BankrollSettings
        {
            RuleProfileVersion = 3,
            ReviewRiskCapUsagePercent = 100m,
            BudgetWarningPercent = 0m,
            DailyRiskCapPercent = 0m,
            ActiveExposureCapPercent = 0m,
            StopLossWarningPercent = 0m,
            CashReloadWarningPercent = 0m
        };

        settings.EnsureDefaults();

        AssertMoney(0m, settings.BudgetWarningPercent);
        AssertMoney(0m, settings.DailyRiskCapPercent);
        AssertMoney(0m, settings.ActiveExposureCapPercent);
        AssertMoney(0m, settings.StopLossWarningPercent);
        AssertMoney(0m, settings.CashReloadWarningPercent);
    }

    [TestMethod]
    public void RuleEngineUsesCategoryMaxRiskInEffectiveCap()
    {
        var settings = new BankrollSettings
        {
            StartingBankroll = 100m,
            ProtectModeBelowBankroll = 0m,
            DailyStopLossAmount = 0m,
            MonthlyPokerStopLossPercent = 0m,
            BudgetWarningPercent = 0m,
            DailyRiskCapPercent = 100m,
            ActiveExposureCapPercent = 100m,
            ReviewRiskCapUsagePercent = 80m
        };
        settings.GetRule(TournamentCategory.MainGrind).MaxRiskPercent = 1m;

        var result = RuleEngine.Evaluate(
            new BankrollData { Settings = settings },
            new DecisionRequest
            {
                Category = TournamentCategory.MainGrind,
                Format = TournamentFormat.MTT,
                BuyIn = 0.90m,
                PlannedBullets = 1
            },
            new DateOnly(2026, 6, 9));

        Assert.AreEqual(DecisionLabel.Review, result.Label);
        Assert.IsTrue(result.Thresholds.Any(threshold => threshold.Contains("normal cap", StringComparison.Ordinal)
            && threshold.Contains("1", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RuleEngineDisplaysDisabledOptionalCapsCleanly()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 100m,
                RuleProfileVersion = 3,
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m,
                BudgetWarningPercent = 0m,
                DailyRiskCapPercent = 0m,
                ActiveExposureCapPercent = 0m,
                StopLossWarningPercent = 0m,
                CashReloadWarningPercent = 0m
            }
        };

        var result = RuleEngine.Evaluate(
            data,
            new DecisionRequest
            {
                IsCashSession = true,
                CashBuyIn = 1m,
                CashReloads = 1m
            },
            new DateOnly(2026, 6, 9));

        Assert.AreEqual(DecisionLabel.PlayOk, result.Label);
        Assert.IsTrue(result.Thresholds.Any(threshold => threshold.Contains("Daily risk", StringComparison.Ordinal)
            && threshold.Contains("disabled", StringComparison.Ordinal)));
        Assert.IsFalse(result.Thresholds.Any(threshold => threshold.Contains("Reloads:", StringComparison.Ordinal)));
        Assert.IsFalse(result.Warnings.Any(warning => warning.Contains("Reload", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void StopLossTriggersForSeedDataOnJuneNinth()
    {
        var data = SeedDataFactory.Create();
        var status = StopLossService.GetStatus(data, new DateOnly(2026, 6, 9));

        Assert.IsTrue(status.DailyStopLossHit);
        Assert.IsTrue(status.MonthlyStopLossHit);
        Assert.IsTrue(status.ProtectModeActive);
        Assert.IsTrue(status.BreakRequired);
        Assert.AreEqual("TAKE BREAK", status.StatusText);
    }

    [TestMethod]
    public void SessionLockOnlyAppliesOnLockedDate()
    {
        var settings = new BankrollSettings { StartingBankroll = 100m };
        settings.LockSessionFor(new DateOnly(2026, 6, 9));
        var data = new BankrollData { Settings = settings };

        var lockedToday = StopLossService.GetStatus(data, new DateOnly(2026, 6, 9));
        var unlockedTomorrow = StopLossService.GetStatus(data, new DateOnly(2026, 6, 10));

        Assert.IsTrue(lockedToday.SessionLocked);
        Assert.IsTrue(lockedToday.BreakRequired);
        Assert.IsFalse(unlockedTomorrow.SessionLocked);
        Assert.IsFalse(unlockedTomorrow.BreakRequired);
        Assert.IsFalse(settings.SessionLockedForToday);
        Assert.IsNull(settings.SessionLockedDate);
    }

    [TestMethod]
    public void CooldownExpiresAfterCooldownDate()
    {
        var settings = new BankrollSettings { StartingBankroll = 100m };
        settings.SetCooldownUntil(new DateOnly(2026, 6, 10));
        var data = new BankrollData { Settings = settings };

        var activeOnEndDate = StopLossService.GetStatus(data, new DateOnly(2026, 6, 10));
        var expiredAfterEndDate = StopLossService.GetStatus(data, new DateOnly(2026, 6, 11));

        Assert.IsTrue(activeOnEndDate.SessionLocked);
        Assert.IsTrue(activeOnEndDate.BreakRequired);
        Assert.IsFalse(expiredAfterEndDate.SessionLocked);
        Assert.IsFalse(expiredAfterEndDate.BreakRequired);
        Assert.IsNull(settings.CooldownUntilDate);
    }

    [TestMethod]
    public void AllInOrFoldUsesStricterCashCapAndWarning()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 100m }
        };

        var result = RuleEngine.Evaluate(
            data,
            new DecisionRequest
            {
                IsCashSession = true,
                Platform = Platform.GGPoker,
                CashFormat = CashFormat.AllInOrFoldHoldem,
                CashBuyIn = 4m
            },
            new DateOnly(2026, 6, 9));

        Assert.AreEqual(DecisionLabel.Pass, result.Label);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("All-In or Fold", StringComparison.Ordinal)));
    }
}

