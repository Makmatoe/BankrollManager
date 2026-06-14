using BankrollManager.Core.Models;
using BankrollManager.Core.Validation;

namespace BankrollManager.Tests;

[TestClass]
public sealed class ValidationTests
{
    [TestMethod]
    public void SpinAndGoldRequiresCoherentInsuranceAndPlayerCount()
    {
        var entry = ValidTournament(TournamentFormat.SpinAndGold);
        entry.SpinPlayerCount = 2;
        entry.InsuranceUsed = true;
        entry.InsuranceCost = 0m;

        var errors = EntryValidator.Validate(entry);

        AssertHasError(errors, "player count");
        AssertHasError(errors, "insurance cost");
    }

    [TestMethod]
    public void FlipAndGoRequiresExplicitStackCostAndWonFlipBeforeGoPhase()
    {
        var entry = ValidTournament(TournamentFormat.FlipAndGo);
        entry.FlipBuyInPerStack = 0m;
        entry.FlipStacksBought = 0;
        entry.GoPhaseReached = true;
        entry.FlipPhaseWon = false;

        var errors = EntryValidator.Validate(entry);

        AssertHasError(errors, "buy-in per stack");
        AssertHasError(errors, "stacks bought");
        AssertHasError(errors, "flip phase");
    }

    [TestMethod]
    public void FinishedFlipsDoNotRequireChronologicalFinishTime()
    {
        var entry = ValidTournament(TournamentFormat.Flip);
        entry.Category = TournamentCategory.FlipSatellite;
        entry.Date = new DateOnly(2026, 6, 10);
        entry.RegistrationTime = new TimeOnly(23, 30);
        entry.FinishedDate = new DateOnly(2026, 6, 9);
        entry.FinishedTime = new TimeOnly(8, 0);

        var errors = EntryValidator.Validate(entry);

        AssertDoesNotHaveError(errors, "Finished date/time cannot be before");
    }

    [TestMethod]
    public void SatelliteTicketsUseTicketFieldsAndRequireTicketValue()
    {
        var entry = ValidTournament(TournamentFormat.Satellite);
        entry.TicketWon = true;
        entry.TargetEventBuyIn = 0m;
        entry.TargetEventName = string.Empty;
        entry.CashPrize = 2m;

        var errors = EntryValidator.Validate(entry);

        AssertHasError(errors, "target buy-in is required");
        AssertHasError(errors, "cash prize fields");
    }

    [TestMethod]
    public void ActiveCashSessionCannotContainCloseResults()
    {
        var session = ValidCashSession(CashSessionStatus.Active);
        session.ClosedDate = session.Date;
        session.ClosedTime = new TimeOnly(13, 0);
        session.Cashout = 2m;
        session.Minutes = 20;

        var errors = EntryValidator.Validate(session);

        AssertHasError(errors, "close date/time");
        AssertHasError(errors, "cashout or prize");
        AssertHasError(errors, "final minutes or hands");
    }

    [TestMethod]
    public void FinishedCashSessionRequiresCloseDateAndCostForReturns()
    {
        var session = ValidCashSession(CashSessionStatus.Finished);
        session.ClosedDate = null;
        session.ClosedTime = null;
        session.StartStackBuyIn = 0m;
        session.Cashout = 3m;

        var errors = EntryValidator.Validate(session);

        AssertHasError(errors, "Closed date");
        AssertHasError(errors, "Closed time");
        AssertHasError(errors, "buy-in or reload cost");
    }

    [TestMethod]
    public void TournamentResultsRequireFinishedStatusAndPossiblePlacement()
    {
        var entry = ValidTournament(TournamentFormat.MTT);
        entry.Status = TournamentStatus.Registered;
        entry.CashPrize = 5m;
        entry.Placement = 11;
        entry.FieldSize = 10;

        var errors = EntryValidator.Validate(entry);

        AssertHasError(errors, "finished tournaments");
        AssertHasError(errors, "Placement cannot be greater");
    }

    private static TournamentEntry ValidTournament(TournamentFormat format)
    {
        return new TournamentEntry
        {
            Status = TournamentStatus.Finished,
            Date = new DateOnly(2026, 6, 10),
            FinishedDate = new DateOnly(2026, 6, 10),
            RegistrationTime = new TimeOnly(12, 0),
            FinishedTime = new TimeOnly(13, 0),
            Format = format,
            EventName = "Validation test",
            BuyIn = 1m,
            ActualBullets = 1,
            PlannedBullets = 1,
            SpinPlayerCount = format == TournamentFormat.SpinAndGold ? 3 : null,
            FlipBuyInPerStack = format == TournamentFormat.FlipAndGo ? 0.04m : 0m,
            FlipStacksBought = format == TournamentFormat.FlipAndGo ? 1 : 0,
            TargetEventBuyIn = IsSatelliteFormat(format) ? 1m : 0m,
            TargetEventName = IsSatelliteFormat(format) ? "Target event" : string.Empty
        };
    }

    private static CashSession ValidCashSession(CashSessionStatus status)
    {
        return new CashSession
        {
            Status = status,
            Date = new DateOnly(2026, 6, 10),
            SessionTime = new TimeOnly(12, 0),
            ClosedDate = status == CashSessionStatus.Finished ? new DateOnly(2026, 6, 10) : null,
            ClosedTime = status == CashSessionStatus.Finished ? new TimeOnly(13, 0) : null,
            Game = "Cash",
            StartStackBuyIn = 2m,
            BigBlindAmount = 0.02m,
            SmallBlindAmount = 0.01m
        };
    }

    private static bool IsSatelliteFormat(TournamentFormat format)
    {
        return format is TournamentFormat.Satellite
            or TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.WSOPExpress;
    }

    private static void AssertHasError(IEnumerable<string> errors, string expectedText)
    {
        Assert.IsTrue(
            errors.Any(error => error.Contains(expectedText, StringComparison.OrdinalIgnoreCase)),
            $"Expected validation error containing '{expectedText}'. Actual: {string.Join(" | ", errors)}");
    }

    private static void AssertDoesNotHaveError(IEnumerable<string> errors, string expectedText)
    {
        Assert.IsFalse(
            errors.Any(error => error.Contains(expectedText, StringComparison.OrdinalIgnoreCase)),
            $"Did not expect validation error containing '{expectedText}'. Actual: {string.Join(" | ", errors)}");
    }
}
