using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class TournamentEvCalculatorTests
{
    [TestMethod]
    public void TicketsModeReportsPositiveEvBeforeBreakevenEntryCount()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 0.04m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 5,
            TicketValue = 0.40m,
            CurrentEntries = 40
        });

        AssertMoney(2.00m, result.TotalPrizeValue);
        AssertMoney(0.05m, result.GrossEv);
        AssertMoney(0.01m, result.NetEv);
        AssertMoney(0.25m, result.Roi);
        AssertMoney(50m, result.ExactBreakEvenEntries);
        Assert.AreEqual(49L, result.MaxPositiveEntries);
        Assert.AreEqual(51L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Positive, result.Status);
    }

    [TestMethod]
    public void TicketsModeReportsBreakevenAtExactEntryCount()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 0.04m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 5,
            TicketValue = 0.40m,
            CurrentEntries = 50
        });

        AssertMoney(0.04m, result.GrossEv);
        AssertMoney(0m, result.NetEv);
        AssertMoney(0m, result.Roi);
        AssertMoney(50m, result.ExactBreakEvenEntries);
        Assert.AreEqual(49L, result.MaxPositiveEntries);
        Assert.AreEqual(51L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Breakeven, result.Status);
    }

    [TestMethod]
    public void TicketsModeReportsNegativeEvAfterBreakevenEntryCount()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 2m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 6,
            TicketValue = 10m,
            CurrentEntries = 31
        });

        AssertMoney(60m, result.TotalPrizeValue);
        Assert.IsLessThan(0m, result.NetEv);
        AssertMoney(30m, result.ExactBreakEvenEntries);
        Assert.AreEqual(29L, result.MaxPositiveEntries);
        Assert.AreEqual(31L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Negative, result.Status);
    }

    [TestMethod]
    public void TicketDiscountReducesTotalPrizeValueForUser()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 2m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 6,
            TicketValue = 10m,
            TicketValueDiscountPercent = 50m,
            CurrentEntries = 20
        });

        AssertMoney(30m, result.TotalPrizeValue);
        AssertMoney(1.50m, result.GrossEv);
        AssertMoney(-0.50m, result.NetEv);
        AssertMoney(-0.25m, result.Roi);
        AssertMoney(15m, result.ExactBreakEvenEntries);
        Assert.AreEqual(14L, result.MaxPositiveEntries);
        Assert.AreEqual(16L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Negative, result.Status);
    }

    [TestMethod]
    public void TicketDiscountIsClampedToTicketFaceValue()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 2m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 1,
            TicketValue = 10m,
            TicketValueDiscountPercent = 125m,
            CurrentEntries = 5
        });

        AssertMoney(10m, result.TotalPrizeValue);
        AssertMoney(2m, result.GrossEv);
        AssertMoney(0m, result.NetEv);
        Assert.AreEqual(TournamentEvStatus.Breakeven, result.Status);
    }

    [TestMethod]
    public void ZeroPrizeDoesNotReportNegativePositiveThreshold()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 1m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 0,
            TicketValue = 10m,
            CurrentEntries = 1
        });

        AssertMoney(0m, result.TotalPrizeValue);
        AssertMoney(-1m, result.NetEv);
        AssertMoney(0m, result.ExactBreakEvenEntries);
        Assert.AreEqual(0L, result.MaxPositiveEntries);
        Assert.AreEqual(1L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Negative, result.Status);
    }

    [TestMethod]
    public void ImpossibleNegativeInputsAreTreatedAsZero()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = -1m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = -2,
            TicketValue = -10m,
            TicketValueDiscountPercent = -50m,
            CurrentEntries = -5
        });

        AssertMoney(0m, result.TotalPrizeValue);
        AssertMoney(0m, result.GrossEv);
        AssertMoney(0m, result.NetEv);
        AssertMoney(0m, result.Roi);
        Assert.AreEqual(0L, result.MaxPositiveEntries);
        Assert.AreEqual(0L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Breakeven, result.Status);
    }

    [TestMethod]
    public void ManualPrizePoolModeUsesManualTotalPrizeValue()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 2m,
            PrizeType = TournamentEvPrizeType.CashPrizePool,
            NumberOfTickets = 1,
            TicketValue = 1m,
            TicketValueDiscountPercent = 10m,
            ManualPrizeValue = 60m,
            CurrentEntries = 20
        });

        AssertMoney(60m, result.TotalPrizeValue);
        AssertMoney(3m, result.GrossEv);
        AssertMoney(1m, result.NetEv);
        AssertMoney(0.50m, result.Roi);
        AssertMoney(30m, result.ExactBreakEvenEntries);
        Assert.AreEqual(29L, result.MaxPositiveEntries);
        Assert.AreEqual(31L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Positive, result.Status);
    }
}
