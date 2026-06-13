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
        AssertMoney(0.40m, result.MaxSinglePrizeValue);
        AssertMoney(0.05m, result.UncappedGrossEv);
        AssertMoney(0.05m, result.GrossEv);
        AssertMoney(0.01m, result.NetEv);
        AssertMoney(0.25m, result.Roi);
        AssertMoney(50m, result.ExactBreakEvenEntries);
        Assert.IsTrue(result.CanBreakEven);
        Assert.AreEqual(49L, result.MaxPositiveEntries);
        Assert.AreEqual(51L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Positive, result.Status);
    }

    [TestMethod]
    public void TicketsModeCapsCurrentEvAtSingleTicketValue()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 0.40m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 5,
            TicketValue = 0.40m,
            CurrentEntries = 1
        });

        AssertMoney(2.00m, result.TotalPrizeValue);
        AssertMoney(0.40m, result.MaxSinglePrizeValue);
        AssertMoney(2.00m, result.UncappedGrossEv);
        AssertMoney(0.40m, result.GrossEv);
        AssertMoney(0m, result.NetEv);
        AssertMoney(0m, result.Roi);
        AssertMoney(5m, result.ExactBreakEvenEntries);
        Assert.IsTrue(result.CanBreakEven);
        Assert.AreEqual(0L, result.MaxPositiveEntries);
        Assert.AreEqual(6L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Breakeven, result.Status);
    }

    [TestMethod]
    public void TicketsModeCannotBreakEvenWhenTicketValueIsBelowBuyIn()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 0.50m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 5,
            TicketValue = 0.40m,
            CurrentEntries = 1
        });

        AssertMoney(2.00m, result.TotalPrizeValue);
        AssertMoney(0.40m, result.MaxSinglePrizeValue);
        AssertMoney(2.00m, result.UncappedGrossEv);
        AssertMoney(0.40m, result.GrossEv);
        AssertMoney(-0.10m, result.NetEv);
        AssertMoney(-0.20m, result.Roi);
        AssertMoney(0m, result.ExactBreakEvenEntries);
        Assert.IsFalse(result.CanBreakEven);
        Assert.AreEqual(0L, result.MaxPositiveEntries);
        Assert.AreEqual(1L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Negative, result.Status);
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
        Assert.IsTrue(result.CanBreakEven);
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
        Assert.IsTrue(result.CanBreakEven);
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
        AssertMoney(5m, result.MaxSinglePrizeValue);
        AssertMoney(1.50m, result.UncappedGrossEv);
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
        AssertMoney(10m, result.MaxSinglePrizeValue);
        AssertMoney(2m, result.UncappedGrossEv);
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
        Assert.IsFalse(result.CanBreakEven);
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
        AssertMoney(0m, result.MaxSinglePrizeValue);
        AssertMoney(0m, result.UncappedGrossEv);
        AssertMoney(0m, result.GrossEv);
        AssertMoney(0m, result.NetEv);
        AssertMoney(0m, result.Roi);
        Assert.IsTrue(result.CanBreakEven);
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
        AssertMoney(60m, result.MaxSinglePrizeValue);
        AssertMoney(3m, result.UncappedGrossEv);
        AssertMoney(3m, result.GrossEv);
        AssertMoney(1m, result.NetEv);
        AssertMoney(0.50m, result.Roi);
        AssertMoney(30m, result.ExactBreakEvenEntries);
        Assert.AreEqual(29L, result.MaxPositiveEntries);
        Assert.AreEqual(31L, result.NegativeEvStartsAt);
        Assert.AreEqual(TournamentEvStatus.Positive, result.Status);
    }

    [TestMethod]
    public void FlatTicketSatelliteVarianceUsesWinChanceAndTicketValue()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 1m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 20,
            TicketValue = 10m,
            CurrentEntries = 100,
            TournamentType = TournamentEvTournamentType.FlatTicketSatellite,
            TotalEntries = 100,
            PaidPlaces = 20,
            SampleSize = 100
        });

        AssertMoney(1m, result.Variance.EvPerTournament);
        AssertMoney(1m, result.Variance.Roi);
        AssertMoney(0.20m, result.Variance.WinOrCashProbability);
        AssertMoney(4m, result.Variance.StandardDeviation);
        AssertMoney(4m, result.Variance.StandardDeviationInBuyIns);
        AssertMoney(100m, result.Variance.ExpectedProfitAfterSample);
        AssertMoney(40m, result.Variance.StandardDeviationAfterSample);
        Assert.AreEqual(TournamentEvVarianceRating.High, result.Variance.Rating);
    }

    [TestMethod]
    public void BreakevenSatelliteShowsZeroLongRunEv()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 2m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 10,
            TicketValue = 10m,
            CurrentEntries = 50,
            TournamentType = TournamentEvTournamentType.FlatTicketSatellite,
            TotalEntries = 50,
            PaidPlaces = 10,
            SampleSize = 100
        });

        AssertMoney(0m, result.Variance.EvPerTournament);
        AssertMoney(0m, result.Variance.Roi);
        AssertMoney(0m, result.Variance.ExpectedProfitAfterSample);
        Assert.IsGreaterThan(0.5m, result.Variance.ChanceNotAheadAfterSample);
        Assert.IsTrue(result.Variance.ChanceNotAheadIsExact);
        Assert.AreEqual(TournamentEvStatus.Breakeven, result.Status);
    }

    [TestMethod]
    public void StrongOverlaySatelliteHasPositiveEvAndLowChanceOfStillBeingDown()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 1m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 20,
            TicketValue = 10m,
            CurrentEntries = 100,
            TournamentType = TournamentEvTournamentType.FlatTicketSatellite,
            TotalEntries = 100,
            PaidPlaces = 20,
            SampleSize = 100
        });

        AssertMoney(1m, result.Variance.EvPerTournament);
        AssertMoney(100m, result.Variance.ExpectedProfitAfterSample);
        Assert.IsLessThan(0.02m, result.Variance.ChanceNotAheadAfterSample);
        Assert.AreEqual(TournamentEvStatus.Positive, result.Status);
    }

    [TestMethod]
    public void NormalCustomPayoutTournamentUsesFinishPayoutDistribution()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 10m,
            PrizeType = TournamentEvPrizeType.CashPrizePool,
            ManualPrizeValue = 100m,
            CurrentEntries = 10,
            TournamentType = TournamentEvTournamentType.NormalMtt,
            TotalEntries = 10,
            PaidPlaces = 3,
            Payouts = [50m, 30m, 20m],
            SampleSize = 100
        });

        AssertMoney(0m, result.Variance.EvPerTournament);
        AssertMoney(0.30m, result.Variance.WinOrCashProbability);
        AssertMoney(16.73m, result.Variance.StandardDeviation);
        AssertMoney(1.67m, result.Variance.StandardDeviationInBuyIns);
        Assert.AreEqual(TournamentEvVarianceRating.Medium, result.Variance.Rating);
        Assert.IsFalse(result.Variance.ChanceNotAheadIsExact);
    }

    [TestMethod]
    public void WinnerTakeAllTournamentReportsExtremeVariance()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 10m,
            PrizeType = TournamentEvPrizeType.CashPrizePool,
            ManualPrizeValue = 1000m,
            CurrentEntries = 100,
            TournamentType = TournamentEvTournamentType.WinnerTakeAll,
            TotalEntries = 100,
            PaidPlaces = 1,
            SampleSize = 100
        });

        AssertMoney(0m, result.Variance.EvPerTournament);
        AssertMoney(0.01m, result.Variance.WinOrCashProbability);
        AssertMoney(99.50m, result.Variance.StandardDeviation);
        AssertMoney(9.95m, result.Variance.StandardDeviationInBuyIns);
        Assert.AreEqual(TournamentEvVarianceRating.Extreme, result.Variance.Rating);
    }

    [TestMethod]
    public void ChanceOfBeingDownAfterSampleUsesExactBinomialForSatellites()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 1m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 20,
            TicketValue = 10m,
            CurrentEntries = 100,
            TournamentType = TournamentEvTournamentType.FlatTicketSatellite,
            TotalEntries = 100,
            PaidPlaces = 20,
            SampleSize = 100
        });

        Assert.IsTrue(result.Variance.ChanceNotAheadIsExact);
        AssertMoney(0.01m, result.Variance.ChanceNotAheadAfterSample);
    }

    [TestMethod]
    public void StandardDeviationInBuyInsDividesStandardDeviationByBuyIn()
    {
        var result = TournamentEvCalculator.Evaluate(new TournamentEvRequest
        {
            BuyIn = 1m,
            PrizeType = TournamentEvPrizeType.Tickets,
            NumberOfTickets = 20,
            TicketValue = 10m,
            CurrentEntries = 100,
            TournamentType = TournamentEvTournamentType.FlatTicketSatellite,
            TotalEntries = 100,
            PaidPlaces = 20,
            SampleSize = 100
        });

        AssertMoney(4m, result.Variance.StandardDeviation);
        AssertMoney(4m, result.Variance.StandardDeviationInBuyIns);
    }
}
