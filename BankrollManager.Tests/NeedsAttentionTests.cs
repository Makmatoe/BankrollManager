using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class NeedsAttentionTests
{
    [TestMethod]
    public void NeedsAttentionItemsComeFromCoreGoalRules()
    {
        var today = new DateOnly(2026, 6, 9);
        var data = new BankrollData
        {
            Settings = new BankrollSettings { ProtectModeBelowBankroll = 0m },
            LedgerEntries =
            [
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.HollandCasino, Amount = 10m }
            ],
            PlatformWallets =
            [
                new PlatformWallet { Platform = Platform.HollandCasino, ActualCashBalance = 7m }
            ],
            CashSessions =
            [
                new CashSession
                {
                    Date = today,
                    Status = CashSessionStatus.Active,
                    Platform = Platform.HollandCasino,
                    Game = "Cash",
                    StartStackBuyIn = 2m
                }
            ],
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = today,
                    Status = TournamentStatus.Registered,
                    EventName = "Pending MTT",
                    BuyIn = 1m,
                    ActualBullets = 1
                }
            ]
        };

        var items = NeedsAttentionService.GetItems(data, today);

        Assert.IsTrue(items.Any(item => item.TargetType == AttentionTargetType.Cash && item.Action == "Close cash"));
        Assert.IsTrue(items.Any(item => item.TargetType == AttentionTargetType.Wallet && item.Severity == AttentionSeverity.High));
        Assert.IsTrue(items.Any(item => item.TargetType == AttentionTargetType.Tournament && item.Action == "Review"));
    }

    [TestMethod]
    public void NeedsAttentionReturnsClearItemWhenNothingRequiresAction()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m
            }
        };

        var items = NeedsAttentionService.GetItems(data, new DateOnly(2026, 6, 9));

        var item = items.Single();
        Assert.AreEqual(AttentionSeverity.Clear, item.Severity);
        Assert.AreEqual(AttentionTargetType.None, item.TargetType);
    }

    [TestMethod]
    public void NeedsAttentionIgnoresAcceptedWalletDifference()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m
            },
            LedgerEntries =
            [
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.HollandCasino, Amount = 10m }
            ],
            PlatformWallets =
            [
                new PlatformWallet
                {
                    Platform = Platform.HollandCasino,
                    ActualCashBalance = 9.57m,
                    AcceptedCashDifference = -0.43m
                }
            ]
        };

        var items = NeedsAttentionService.GetItems(data, new DateOnly(2026, 6, 12));

        Assert.IsFalse(items.Any(item => item.TargetType == AttentionTargetType.Wallet));
    }
}

