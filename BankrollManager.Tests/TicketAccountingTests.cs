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

        AssertMoney(0m, data.TournamentEntries[1].CashCost);
        AssertMoney(8m, data.TournamentEntries[1].NetProfit);
        AssertMoney(0m, BankrollCalculator.TicketBalance(data));
        AssertMoney(17m, BankrollCalculator.CurrentBankroll(data));
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
}

