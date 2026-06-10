using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class CashWorkflowTests
{
    [TestMethod]
    public void ActiveCashSessionTracksWalletAndTableCashUntilClosed()
    {
        var session = new CashSession
        {
            Date = new DateOnly(2026, 6, 9),
            Status = CashSessionStatus.Active,
            Platform = Platform.HollandCasino,
            Game = "Cash",
            StartStackBuyIn = 2m
        };
        var data = new BankrollData
        {
            LedgerEntries =
            [
                new LedgerEntry { Type = LedgerType.Deposit, Platform = Platform.HollandCasino, Amount = 10m }
            ],
            CashSessions = [session]
        };

        BankrollCalculator.RecalculateTrackingFields(data);
        var activeSummary = BankrollCalculator.GetPlatformSummaries(data)
            .Single(summary => summary.Name == Platform.HollandCasino.ToString());

        AssertMoney(0m, BankrollCalculator.CashProfitLoss(data));
        AssertMoney(10m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(8m, activeSummary.WalletCashBalance);
        AssertMoney(2m, activeSummary.ActiveTableCash);
        AssertMoney(10m, activeSummary.TotalPlatformExposure);

        session.Status = CashSessionStatus.Finished;
        session.ClosedDate = new DateOnly(2026, 6, 9);
        session.Cashout = 3.10m;

        BankrollCalculator.RecalculateTrackingFields(data);
        var closedSummary = BankrollCalculator.GetPlatformSummaries(data)
            .Single(summary => summary.Name == Platform.HollandCasino.ToString());

        AssertMoney(1.10m, BankrollCalculator.CashProfitLoss(data));
        AssertMoney(11.10m, BankrollCalculator.CurrentBankroll(data));
        AssertMoney(11.10m, closedSummary.WalletCashBalance);
        AssertMoney(0m, closedSummary.ActiveTableCash);
        AssertMoney(11.10m, closedSummary.TotalPlatformExposure);
    }

    [TestMethod]
    public void CashSessionWorkflowOwnsActiveAndClosedTransitions()
    {
        var started = CashSessionWorkflowService.CreateActiveDraft(
            new DateTime(2026, 6, 9, 12, 34, 0),
            Platform.HollandCasino);

        Assert.AreEqual(CashSessionStatus.Active, started.Status);
        Assert.AreEqual(new DateOnly(2026, 6, 9), started.Date);
        Assert.AreEqual(new TimeOnly(12, 34), started.SessionTime);
        Assert.IsNull(started.ClosedDate);
        Assert.IsNull(started.ClosedTime);

        started.StartStackBuyIn = 2m;
        CashSessionWorkflowService.MarkClosed(
            started,
            new CashSessionCloseDetails(
                new DateOnly(2026, 6, 9),
                new TimeOnly(13, 20),
                1m,
                4.50m,
                46,
                120,
                "Closed cleanly"));

        Assert.AreEqual(CashSessionStatus.Finished, started.Status);
        AssertMoney(3m, started.SessionCost);
        AssertMoney(1.50m, started.NetProfit);
        Assert.AreEqual(46, started.Minutes);
        Assert.AreEqual(120, started.Hands);
    }

    [TestMethod]
    public void CashSessionWorkflowAutofillsMinutesFromTrackedTimes()
    {
        var started = CashSessionWorkflowService.CreateActiveDraft(
            new DateTime(2026, 6, 9, 23, 45, 0),
            Platform.HollandCasino);
        started.StartStackBuyIn = 2m;

        CashSessionWorkflowService.MarkClosed(
            started,
            new CashSessionCloseDetails(
                new DateOnly(2026, 6, 10),
                new TimeOnly(0, 15),
                0m,
                2.50m,
                null,
                null,
                "Closed after midnight"));

        Assert.AreEqual(30, started.Minutes);
    }

    [TestMethod]
    public void CashSessionNetProfitAndBbMetricsAreCalculated()
    {
        var entry = new CashSession
        {
            BigBlindAmount = 0.10m,
            StartStackBuyIn = 2m,
            Reloads = 1m,
            Cashout = 4.50m,
            Hands = 100
        };

        AssertMoney(3m, entry.SessionCost);
        AssertMoney(1.50m, entry.NetProfit);
        AssertMoney(15m, entry.BBWon);
        AssertMoney(15m, entry.BBPer100);
    }

    [TestMethod]
    public void GgCashExtrasAreIncludedInClosedSessionProfit()
    {
        var session = new CashSession
        {
            Status = CashSessionStatus.Finished,
            Format = CashFormat.RushAndCashHoldem,
            StartStackBuyIn = 10m,
            Reloads = 2m,
            Cashout = 13m,
            CashDropWon = 0.50m,
            JackpotFortunePrizeWon = 1.25m
        };

        AssertMoney(2.75m, session.NetProfit);
    }
}

