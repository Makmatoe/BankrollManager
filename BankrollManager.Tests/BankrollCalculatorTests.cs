using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class BankrollCalculatorTests
{
    [TestMethod]
    public void SeedDataTotalsMatchExpectedValues()
    {
        var data = SeedDataFactory.Create();

        AssertMoney(25.00m, BankrollCalculator.TotalDeposits(data));
        AssertMoney(-7.20m, BankrollCalculator.TournamentProfitLoss(data));
        AssertMoney(-14.22m, BankrollCalculator.CashProfitLoss(data));
        AssertMoney(-21.42m, BankrollCalculator.TotalPokerProfitLoss(data));
        AssertMoney(3.58m, BankrollCalculator.CurrentBankroll(data));
    }

    [TestMethod]
    public void CurrentBankrollIncludesLedgerAndPokerResults()
    {
        var data = new BankrollData
        {
            LedgerEntries =
            [
                new LedgerEntry { Type = LedgerType.Deposit, Amount = 50m },
                new LedgerEntry { Type = LedgerType.Withdrawal, Amount = 10m }
            ],
            TournamentEntries =
            [
                new TournamentEntry { BuyIn = 5m, ActualBullets = 1, CashPrize = 12m }
            ],
            CashSessions =
            [
                new CashSession { StartStackBuyIn = 4m, Cashout = 1m }
            ]
        };

        AssertMoney(44m, BankrollCalculator.CurrentBankroll(data));
    }

    [TestMethod]
    public void PlatformSummariesTrackWalletCashBalances()
    {
        var data = SeedDataFactory.Create();
        var platforms = BankrollCalculator.GetPlatformSummaries(data);
        var hollandCasino = platforms.Single(summary => summary.Name == Platform.HollandCasino.ToString());
        var unibet = platforms.Single(summary => summary.Name == Platform.Unibet.ToString());

        AssertMoney(15m, hollandCasino.Deposits);
        AssertMoney(-10.80m, hollandCasino.CashSessionProfitLoss);
        AssertMoney(-1m, hollandCasino.TournamentProfitLoss);
        AssertMoney(3.20m, hollandCasino.WalletCashBalance);
        AssertMoney(3.20m, hollandCasino.TotalPlatformExposure);
        AssertMoney(0.38m, unibet.WalletCashBalance);
        AssertMoney(BankrollCalculator.CurrentBankroll(data), platforms.Sum(summary => summary.WalletCashBalance));
    }

    [TestMethod]
    public void PlatformTransfersMoveWalletCashWithoutChangingBankroll()
    {
        var data = new BankrollData
        {
            LedgerEntries =
            [
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.Unibet, Amount = 10m },
                new LedgerEntry { Type = LedgerType.TransferOut, Platform = Platform.Unibet, Amount = 4m },
                new LedgerEntry { Type = LedgerType.TransferIn, Platform = Platform.HollandCasino, Amount = 4m }
            ],
            PlatformWallets =
            [
                new PlatformWallet
                {
                    Platform = Platform.Unibet,
                    ActualCashBalance = 5m,
                    LastUpdatedDate = new DateOnly(2026, 6, 9)
                }
            ]
        };

        data.EnsureDefaults();
        var platforms = BankrollCalculator.GetPlatformSummaries(data);
        var unibet = platforms.Single(summary => summary.Name == Platform.Unibet.ToString());
        var hollandCasino = platforms.Single(summary => summary.Name == Platform.HollandCasino.ToString());

        AssertMoney(10m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(6m, unibet.WalletCashBalance);
        AssertMoney(4m, hollandCasino.WalletCashBalance);
        AssertMoney(-1m, unibet.Difference ?? 0m);
        Assert.AreEqual(new DateOnly(2026, 6, 9), unibet.LastUpdatedDate);
    }

    [TestMethod]
    public void PlatformWalletAndSummaryListsUseAlphabeticOrder()
    {
        var data = new BankrollData
        {
            PlatformWallets =
            [
                new PlatformWallet { Platform = Platform.Unibet },
                new PlatformWallet { Platform = Platform.Other },
                new PlatformWallet { Platform = Platform.GGPoker },
                new PlatformWallet { Platform = Platform.HollandCasino }
            ],
            LedgerEntries =
            [
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.Unibet, Amount = 1m },
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.Other, Amount = 1m },
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.GGPoker, Amount = 1m },
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.HollandCasino, Amount = 1m }
            ]
        };

        data.EnsureDefaults();
        var walletNames = data.PlatformWallets.Select(wallet => wallet.Platform.ToString()).ToArray();
        var summaryNames = BankrollCalculator.GetPlatformSummaries(data).Select(summary => summary.Name).ToArray();

        CollectionAssert.AreEqual(
            new[] { "GGPoker", "HollandCasino", "Other", "Unibet" },
            walletNames);
        CollectionAssert.AreEqual(walletNames, summaryNames);
    }

    [TestMethod]
    public void TournamentNetProfitAndRoiAreCalculatedFromCostsAndReturns()
    {
        var entry = new TournamentEntry
        {
            BuyIn = 2m,
            ActualBullets = 2,
            AddOnsRebuys = 1m,
            BountyTicketValue = 1m,
            CashPrize = 8m
        };

        AssertMoney(5m, entry.TotalCost);
        AssertMoney(5m, entry.CashCost);
        AssertMoney(4m, entry.NetProfit);
        AssertMoney(0.8m, entry.ROI);
    }

    [TestMethod]
    public void DailyProfitLossAggregatesTournamentAndCashEntries()
    {
        var data = SeedDataFactory.Create();
        var june7 = BankrollCalculator.GetDailySummaries(data)
            .Single(summary => summary.Date == new DateOnly(2026, 6, 7));

        AssertMoney(-0.50m, june7.TournamentProfitLoss);
        AssertMoney(-0.80m, june7.CashProfitLoss);
        AssertMoney(-1.30m, june7.TotalProfitLoss);
        Assert.AreEqual(3, june7.NumberOfSessions);
    }

    [TestMethod]
    public void MonthlyProfitLossAggregatesExpectedSeedMonth()
    {
        var data = SeedDataFactory.Create();
        var june = BankrollCalculator.GetMonthlySummaries(data)
            .Single(summary => summary.Month == new DateOnly(2026, 6, 1));

        AssertMoney(25.00m, june.Deposits);
        AssertMoney(-7.20m, june.TournamentProfitLoss);
        AssertMoney(-14.22m, june.CashProfitLoss);
        AssertMoney(-21.42m, june.TotalPokerProfitLoss);
        Assert.AreEqual(8, june.NumberOfTournaments);
        Assert.AreEqual(4, june.NumberOfCashSessions);
    }

    [TestMethod]
    public void RiskPercentageUsesEventRiskAgainstAvailableBankroll()
    {
        AssertMoney(2.5m, BankrollCalculator.RiskPercentage(2.5m, 100m));
        AssertMoney(100m, BankrollCalculator.RiskPercentage(2.5m, 0m));
        AssertMoney(0m, BankrollCalculator.RiskPercentage(0m, 0m));
    }

    [TestMethod]
    public void TrackingFieldsUseRegistrationTimeWithinSameDay()
    {
        var date = new DateOnly(2026, 6, 9);
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 20m },
            CashSessions =
            [
                new CashSession
                {
                    Date = date,
                    StartStackBuyIn = 10m,
                    Cashout = 0m
                }
            ],
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = date,
                    RegistrationTime = new TimeOnly(12, 0),
                    Status = TournamentStatus.Registered,
                    EventName = "Later tournament",
                    BuyIn = 5m,
                    ActualBullets = 1
                }
            ]
        };

        BankrollCalculator.RecalculateTrackingFields(data);

        AssertMoney(20m, data.CashSessions[0].BankrollBefore);
        AssertMoney(10m, data.CashSessions[0].BankrollAfter);
        AssertMoney(10m, data.TournamentEntries[0].BankrollBefore);
        AssertMoney(5m, data.TournamentEntries[0].BankrollAfter);
        AssertMoney(50m, data.TournamentEntries[0].RiskPercentageOfBankrollAtRegistration);
    }

    [TestMethod]
    public void AuditTimelineShowsBankrollBeforeAndAfterInEventOrder()
    {
        var date = new DateOnly(2026, 6, 9);
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 20m },
            LedgerEntries =
            [
                new LedgerEntry
                {
                    Date = date,
                    Type = LedgerType.Deposit,
                    Description = "Top up",
                    Amount = 5m
                }
            ],
            CashSessions =
            [
                new CashSession
                {
                    Date = date,
                    SessionTime = new TimeOnly(10, 0),
                    StartStackBuyIn = 10m,
                    Cashout = 0m
                }
            ],
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = date,
                    RegistrationTime = new TimeOnly(12, 0),
                    Status = TournamentStatus.Registered,
                    EventName = "Later tournament",
                    BuyIn = 5m,
                    ActualBullets = 1
                }
            ]
        };

        BankrollCalculator.RecalculateTrackingFields(data);
        var timeline = BankrollCalculator.GetAuditTimeline(data);

        AssertMoney(20m, data.LedgerEntries[0].BankrollBefore);
        AssertMoney(25m, data.LedgerEntries[0].BankrollAfter);
        CollectionAssert.AreEqual(
            new[] { "Deposit", "Cash", "Tournament Buy-in" },
            timeline.Select(entry => entry.Type).ToArray());
        AssertMoney(20m, timeline[0].BankrollBefore);
        AssertMoney(25m, timeline[0].BankrollAfter);
        AssertMoney(25m, timeline[1].BankrollBefore);
        AssertMoney(15m, timeline[1].BankrollAfter);
        AssertMoney(15m, timeline[2].BankrollBefore);
        AssertMoney(10m, timeline[2].BankrollAfter);
    }

    [TestMethod]
    public void PendingTournamentCommitsCostBeforeResultIsKnown()
    {
        var date = new DateOnly(2026, 6, 9);
        var tournament = new TournamentEntry
        {
            Date = date,
            RegistrationTime = new TimeOnly(10, 0),
            Status = TournamentStatus.Registered,
            EventName = "Pending tournament",
            BuyIn = 5m,
            ActualBullets = 1,
            CashPrize = 50m
        };
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 20m },
            TournamentEntries = [tournament]
        };

        BankrollCalculator.RecalculateTrackingFields(data);

        AssertMoney(-5m, tournament.NetProfit);
        AssertMoney(15m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(20m, tournament.BankrollBefore);
        AssertMoney(15m, tournament.BankrollAfter);

        tournament.Status = TournamentStatus.Finished;
        tournament.FinishedDate = date;
        tournament.FinishedTime = new TimeOnly(12, 0);
        BankrollCalculator.RecalculateTrackingFields(data);

        AssertMoney(45m, tournament.NetProfit);
        AssertMoney(65m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(65m, tournament.BankrollAfter);
    }

    [TestMethod]
    public void SpinAndGoldInsuranceIncreasesTotalCost()
    {
        var entry = new TournamentEntry
        {
            Status = TournamentStatus.Finished,
            Format = TournamentFormat.SpinAndGold,
            EventName = "Spin & Gold",
            BuyIn = 1m,
            InsuranceUsed = true,
            InsuranceCost = 0.20m,
            PrizeWon = 2m
        };

        AssertMoney(1.20m, entry.TotalCost);
        AssertMoney(0.80m, entry.NetProfit);
    }

    [TestMethod]
    public void FlipAndGoStacksMultiplyTotalCost()
    {
        var entry = new TournamentEntry
        {
            Status = TournamentStatus.Finished,
            Format = TournamentFormat.FlipAndGo,
            EventName = "Flip & Go",
            FlipBuyInPerStack = 0.04m,
            FlipStacksBought = 3,
            PrizeWon = 0.40m
        };

        AssertMoney(0.12m, entry.TotalCost);
        AssertMoney(0.28m, entry.NetProfit);
    }
}

