using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class TicketAccountingTests
{
    [TestMethod]
    public void TicketWinsAndTicketBuyInsAreTrackedOutsideCashBankroll()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 10m },
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 9),
                    Status = TournamentStatus.Finished,
                    EventName = "Ticket satellite",
                    BuyIn = 1m,
                    ActualBullets = 1,
                    TicketValueWon = 5m
                },
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 10),
                    Status = TournamentStatus.Finished,
                    EventName = "Ticket target",
                    BuyIn = 5m,
                    ActualBullets = 1,
                    TicketBuyInValue = 5m,
                    CashPrize = 8m
                }
            ]
        };

        BankrollCalculator.RecalculateTrackingFields(data);
        var dailySummaries = BankrollCalculator.GetDailySummaries(data);
        var satelliteDay = dailySummaries.Single(summary => summary.Date == new DateOnly(2026, 6, 9));
        var targetDay = dailySummaries.Single(summary => summary.Date == new DateOnly(2026, 6, 10));

        AssertMoney(0m, data.TournamentEntries[1].CashCost);
        AssertMoney(8m, data.TournamentEntries[1].NetProfit);
        AssertMoney(0m, BankrollCalculator.TicketBalance(data));
        AssertMoney(17m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(17m, BankrollCalculator.CurrentBankrollValue(data));
        AssertMoney(7m, BankrollCalculator.TotalValueProfitLoss(data));
        AssertMoney(4m, satelliteDay.TotalValueProfitLoss);
        AssertMoney(-5m, targetDay.TicketProfitLoss);
        AssertMoney(3m, targetDay.TotalValueProfitLoss);
        AssertMoney(17m, targetDay.RunningLifetimeBankrollValue);
    }

    [TestMethod]
    public void TicketWinsIncreaseOverallValueWithoutIncreasingCashBankroll()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 10m },
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 9),
                    Status = TournamentStatus.Finished,
                    EventName = "Ticket satellite",
                    BuyIn = 1m,
                    ActualBullets = 1,
                    TicketValueWon = 5m
                }
            ]
        };

        BankrollCalculator.RecalculateTrackingFields(data);
        var day = BankrollCalculator.GetDailySummaries(data).Single();
        var dashboard = BankrollCalculator.GetDashboardSummary(data, new DateOnly(2026, 6, 9));

        AssertMoney(9m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(5m, BankrollCalculator.TicketBalance(data));
        AssertMoney(14m, BankrollCalculator.CurrentBankrollValue(data));
        AssertMoney(-1m, day.TournamentProfitLoss);
        AssertMoney(5m, day.TicketProfitLoss);
        AssertMoney(4m, day.TotalValueProfitLoss);
        AssertMoney(9m, day.RunningLifetimeBankroll);
        AssertMoney(14m, day.RunningLifetimeBankrollValue);
        AssertMoney(14m, dashboard.CurrentBankrollValue);
        AssertMoney(4m, dashboard.TodayValueProfitLoss);
    }

    [TestMethod]
    public void TicketValueFlowsThroughRunningBankrollOnSettlementDate()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 10m },
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 9),
                    RegistrationTime = new TimeOnly(20, 0),
                    Status = TournamentStatus.Finished,
                    FinishedDate = new DateOnly(2026, 6, 10),
                    FinishedTime = new TimeOnly(0, 15),
                    EventName = "Late satellite",
                    BuyIn = 1m,
                    ActualBullets = 1,
                    TicketValueWon = 5m
                }
            ]
        };

        var running = BankrollCalculator.GetRunningBankroll(data);
        var summaries = BankrollCalculator.GetDailySummaries(data);
        var registrationDay = summaries.Single(summary => summary.Date == new DateOnly(2026, 6, 9));
        var settlementDay = summaries.Single(summary => summary.Date == new DateOnly(2026, 6, 10));

        Assert.HasCount(2, running);
        AssertMoney(-1m, running[0].Amount);
        AssertMoney(0m, running[0].TicketAmount);
        AssertMoney(9m, running[0].BankrollValue);
        AssertMoney(0m, running[1].Amount);
        AssertMoney(5m, running[1].TicketAmount);
        AssertMoney(5m, running[1].TicketBalance);
        AssertMoney(14m, running[1].BankrollValue);

        AssertMoney(-1m, registrationDay.TotalValueProfitLoss);
        AssertMoney(0m, registrationDay.TicketProfitLoss);
        AssertMoney(5m, settlementDay.TotalValueProfitLoss);
        AssertMoney(5m, settlementDay.TicketProfitLoss);
        AssertMoney(14m, settlementDay.RunningLifetimeBankrollValue);
    }

    [TestMethod]
    public void TicketBuyInsDebitSelectedSourcePlatform()
    {
        var data = new BankrollData
        {
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Platform = Platform.GGPoker,
                    Status = TournamentStatus.Finished,
                    EventName = "Ticket satellite",
                    TicketValueWon = 5m
                },
                new TournamentEntry
                {
                    Platform = Platform.Unibet,
                    Status = TournamentStatus.Finished,
                    EventName = "Ticket target",
                    BuyIn = 5m,
                    ActualBullets = 1,
                    TicketBuyInValue = 5m,
                    TicketBuyInPlatform = Platform.GGPoker
                }
            ]
        };

        var platformSummaries = BankrollCalculator.GetPlatformSummaries(data);
        var ggPoker = platformSummaries.Single(summary => summary.Name == Platform.GGPoker.ToString());
        var unibet = platformSummaries.Single(summary => summary.Name == Platform.Unibet.ToString());

        AssertMoney(0m, BankrollCalculator.TicketBalance(data));
        AssertMoney(0m, BankrollCalculator.TicketBalance(data, Platform.GGPoker));
        AssertMoney(0m, ggPoker.TicketBalance);
        AssertMoney(0m, unibet.TicketBalance);
    }

    [TestMethod]
    public void LegacyTicketBuyInsUseTournamentPlatformAsSource()
    {
        var data = new BankrollData
        {
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Platform = Platform.Unibet,
                    Status = TournamentStatus.Finished,
                    EventName = "Old ticket target",
                    BuyIn = 5m,
                    ActualBullets = 1,
                    TicketBuyInValue = 5m
                }
            ]
        };

        AssertMoney(-5m, BankrollCalculator.TicketBalance(data, Platform.Unibet));
        AssertMoney(0m, BankrollCalculator.TicketBalance(data, Platform.GGPoker));
    }

    [TestMethod]
    public void TicketSatelliteShowsCashLossButPositiveTotalValue()
    {
        var entry = new TournamentEntry
        {
            Status = TournamentStatus.Finished,
            BuyIn = 0.04m,
            ActualBullets = 1,
            TicketValueWon = 0.40m,
            Placement = 1,
            ITM = true
        };

        AssertMoney(0.04m, entry.CashCost);
        AssertMoney(-0.04m, entry.NetProfit);
        AssertMoney(-0.04m, entry.CashProfitLoss);
        AssertMoney(0.40m, entry.TicketBalanceImpact);
        AssertMoney(0.36m, entry.TotalValueProfitLoss);
        AssertMoney(-1m, entry.ROI);
        AssertMoney(9m, entry.ValueROI);
    }

    [TestMethod]
    public void MysteryBountyTracksCashBountyTicketsAndDollarBalancesSeparately()
    {
        var entry = new TournamentEntry
        {
            Status = TournamentStatus.Finished,
            Format = TournamentFormat.MysteryBounty,
            EventName = "Mystery Bounty",
            BuyIn = 1m,
            ActualBullets = 1,
            RegularCashPrize = 2m,
            MysteryBountyPrize = 0.50m,
            TicketValueWon = 3m,
            TournamentDollarsWon = 1m,
            CashDollarsWon = 0.25m
        };

        AssertMoney(2.75m, entry.NetProfit);
        AssertMoney(3m, entry.TicketBalanceImpact);
        AssertMoney(5.75m, entry.TotalValueProfitLoss);
    }

    [TestMethod]
    public void SatelliteTicketValueIsNotCashUntilRealized()
    {
        var entry = new TournamentEntry
        {
            Status = TournamentStatus.Finished,
            Format = TournamentFormat.TargetStackSatellite,
            EventName = "Target Stack",
            BuyIn = 1m,
            ActualBullets = 1,
            TargetEventBuyIn = 10m,
            TicketWon = true
        };

        AssertMoney(-1m, entry.CashProfitLoss);
        AssertMoney(10m, entry.TicketBalanceImpact);
        AssertMoney(9m, entry.TotalValueProfitLoss);

        entry.TicketConvertedRealized = true;

        AssertMoney(9m, entry.CashProfitLoss);
        AssertMoney(0m, entry.TicketBalanceImpact);
        AssertMoney(9m, entry.TotalValueProfitLoss);
    }

    [TestMethod]
    public void ReleaseScaleTicketBalanceMatchesPlatformBreakdown()
    {
        var data = ReleaseScaleDataFactory.Create();
        var realizedSatellite = data.TournamentEntries.First(entry => entry.TicketConvertedRealized);

        var platformTicketBalance = Enum.GetValues<Platform>()
            .Sum(platform => BankrollCalculator.TicketBalance(data, platform));

        AssertMoney(decimal.Round(BankrollCalculator.TicketBalance(data), 2), platformTicketBalance);
        AssertMoney(0m, realizedSatellite.TicketReturnAmount);
        AssertMoney(realizedSatellite.EffectiveTicketValueWon - realizedSatellite.CashCost, realizedSatellite.CashProfitLoss);
    }
}
