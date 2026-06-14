using BankrollManager.Core.Models;

namespace BankrollManager.Core.Validation;

public static class EntryValidator
{
    public static List<string> Validate(LedgerEntry entry)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(entry.Description))
        {
            errors.Add("Description is required.");
        }

        if (entry.Amount == 0m)
        {
            errors.Add("Amount cannot be zero.");
        }

        return errors;
    }

    public static List<string> Validate(TournamentEntry entry)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(entry.EventName))
        {
            errors.Add("Tournament/Event name is required.");
        }

        if (entry.BuyIn < 0m)
        {
            errors.Add("Buy-in cannot be negative.");
        }

        if (entry.FeeRake < 0m)
        {
            errors.Add("Fee/Rake cannot be negative.");
        }

        if (entry.PlannedBullets < 0)
        {
            errors.Add("Planned bullets cannot be negative.");
        }

        if (entry.ActualBullets < 0)
        {
            errors.Add("Actual bullets cannot be negative.");
        }

        if (entry.AddOnsRebuys < 0m)
        {
            errors.Add("Add-ons/Rebuys cannot be negative.");
        }

        if (entry.CashPrize < 0m)
        {
            errors.Add("Cash prize cannot be negative.");
        }

        if (entry.BountyTicketValue < 0m)
        {
            errors.Add("Bounty cash cannot be negative.");
        }

        if (entry.TicketBuyInValue < 0m)
        {
            errors.Add("Ticket buy-in cannot be negative.");
        }

        if (entry.TicketValueWon < 0m)
        {
            errors.Add("Ticket value won cannot be negative.");
        }

        if (entry.TournamentDollarsWon < 0m
            || entry.CashDollarsWon < 0m
            || entry.RegularCashPrize < 0m
            || entry.MysteryBountyPrize < 0m
            || entry.BountyPrize < 0m
            || entry.InsuranceCost < 0m
            || entry.MultiplierHit < 0m
            || entry.PrizeWon < 0m
            || entry.FlipBuyInPerStack < 0m
            || entry.TargetEventBuyIn < 0m
            || entry.TicketUsedValue < 0m)
        {
            errors.Add("GGPoker value fields cannot be negative.");
        }

        if (entry.FlipStacksBought < 0)
        {
            errors.Add("Flip stacks bought cannot be negative.");
        }

        if (entry.Knockouts is < 0 || entry.KnockoutsAfterBountyPhase is < 0)
        {
            errors.Add("Knockout counts cannot be negative.");
        }

        if (entry.Placement is <= 0)
        {
            errors.Add("Placement must be greater than zero when provided.");
        }

        if (entry.FieldSize is <= 0)
        {
            errors.Add("Field size must be greater than zero when provided.");
        }

        if (entry.Placement is { } placement
            && entry.FieldSize is { } fieldSize
            && placement > fieldSize)
        {
            errors.Add("Placement cannot be greater than field size.");
        }

        if (entry.TicketBuyInValue > entry.TotalCost)
        {
            errors.Add("Ticket buy-in cannot exceed total tournament cost.");
        }

        if (entry.Status != TournamentStatus.Finished && HasFinishedResult(entry))
        {
            errors.Add("Prize, placement, and ticket result fields can only be set on finished tournaments.");
        }

        if (entry.Status == TournamentStatus.Finished
            && entry.TotalCost <= 0m
            && !entry.IsPromoFreebieTicketEvent
            && HasPrizeValue(entry))
        {
            errors.Add("Zero-cost prize entries must be marked as promo/freebie/ticket events.");
        }

        ValidateSpinAndGold(entry, errors);
        ValidateFlipAndGo(entry, errors);
        ValidateSatelliteTicket(entry, errors);

        if (entry.Status == TournamentStatus.Finished
            && !IsFlipTournament(entry)
            && entry.FinishedDate is { } finishedDate
            && entry.RegistrationTime is { } registrationTime
            && entry.FinishedTime is { } finishedTime
            && finishedDate.ToDateTime(finishedTime) < entry.Date.ToDateTime(registrationTime))
        {
            errors.Add("Finished date/time cannot be before registration date/time.");
        }

        return errors;
    }

    public static List<string> Validate(CashSession entry)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(entry.Game))
        {
            errors.Add("Game is required.");
        }

        if (entry.BigBlindAmount < 0m)
        {
            errors.Add("Big blind cannot be negative.");
        }

        if (entry.SmallBlindAmount < 0m)
        {
            errors.Add("Small blind cannot be negative.");
        }

        if (entry.StartStackBuyIn < 0m)
        {
            errors.Add("Start stack/buy-in cannot be negative.");
        }

        if (entry.Reloads < 0m)
        {
            errors.Add("Reloads cannot be negative.");
        }

        if (entry.ReloadCap < 0m)
        {
            errors.Add("Reload cap cannot be negative.");
        }

        if (entry.Cashout < 0m)
        {
            errors.Add("Cashout cannot be negative.");
        }

        if (entry.CashDropWon < 0m || entry.JackpotFortunePrizeWon < 0m)
        {
            errors.Add("GGPoker cash prizes cannot be negative.");
        }

        if (entry.Status == CashSessionStatus.Active && entry.StartStackBuyIn <= 0m)
        {
            errors.Add("Start stack/buy-in must be greater than zero for active sessions.");
        }

        if (entry.ReloadCap > 0m && entry.Reloads > entry.ReloadCap)
        {
            errors.Add("Reloads cannot exceed reload cap.");
        }

        if (entry.SmallBlindAmount > 0m
            && entry.BigBlindAmount > 0m
            && entry.SmallBlindAmount > entry.BigBlindAmount)
        {
            errors.Add("Small blind cannot be greater than big blind.");
        }

        if (entry.Status == CashSessionStatus.Active)
        {
            if (entry.ClosedDate is not null || entry.ClosedTime is not null)
            {
                errors.Add("Active cash sessions cannot have close date/time.");
            }

            if (entry.Cashout > 0m || entry.CashDropWon > 0m || entry.JackpotFortunePrizeWon > 0m)
            {
                errors.Add("Active cash sessions cannot have cashout or prize results.");
            }

            if (entry.Minutes is not null || entry.Hands is not null)
            {
                errors.Add("Active cash sessions should not have final minutes or hands recorded.");
            }
        }

        if (entry.Status == CashSessionStatus.Finished)
        {
            if (entry.ClosedDate is null)
            {
                errors.Add("Closed date is required for finished cash sessions.");
            }

            if (entry.ClosedTime is null)
            {
                errors.Add("Closed time is required for finished cash sessions.");
            }

            if (entry.SessionCost <= 0m
                && (entry.Cashout > 0m || entry.CashDropWon > 0m || entry.JackpotFortunePrizeWon > 0m))
            {
                errors.Add("Finished cash sessions with returns must have buy-in or reload cost recorded.");
            }
        }

        if (entry.Minutes < 0)
        {
            errors.Add("Minutes cannot be negative.");
        }

        if (entry.Hands < 0)
        {
            errors.Add("Hands cannot be negative.");
        }

        if (entry.Status == CashSessionStatus.Finished
            && entry.ClosedDate is { } closedDate
            && entry.SessionTime is { } sessionTime
            && entry.ClosedTime is { } closedTime
            && closedDate.ToDateTime(closedTime) < entry.Date.ToDateTime(sessionTime))
        {
            errors.Add("Closed date/time cannot be before session start date/time.");
        }

        return errors;
    }

    private static void ValidateSpinAndGold(TournamentEntry entry, List<string> errors)
    {
        if (entry.Format != TournamentFormat.SpinAndGold)
        {
            return;
        }

        if (entry.BuyIn <= 0m && !entry.IsPromoFreebieTicketEvent)
        {
            errors.Add("Spin & Gold buy-in must be greater than zero unless marked as promo/freebie/ticket.");
        }

        if (entry.SpinPlayerCount is not (3 or 6))
        {
            errors.Add("Spin & Gold player count must be 3 or 6.");
        }

        if (entry.InsuranceUsed && entry.InsuranceCost <= 0m)
        {
            errors.Add("Spin & Gold insurance cost is required when insurance is used.");
        }

        if (!entry.InsuranceUsed && entry.InsuranceCost > 0m)
        {
            errors.Add("Spin & Gold insurance cost requires insurance to be marked as used.");
        }
    }

    private static void ValidateFlipAndGo(TournamentEntry entry, List<string> errors)
    {
        if (entry.Format != TournamentFormat.FlipAndGo)
        {
            return;
        }

        if (entry.FlipBuyInPerStack <= 0m)
        {
            errors.Add("Flip & Go buy-in per stack must be greater than zero.");
        }

        if (entry.FlipStacksBought <= 0)
        {
            errors.Add("Flip & Go stacks bought must be greater than zero.");
        }

        if (entry.GoPhaseReached && !entry.FlipPhaseWon)
        {
            errors.Add("Flip & Go phase cannot be reached unless the flip phase was won.");
        }
    }

    private static bool IsFlipTournament(TournamentEntry entry)
    {
        return entry.Category == TournamentCategory.FlipSatellite
            || entry.Format is TournamentFormat.Flip or TournamentFormat.FlipAndGo
            || entry.EventTag == EventTag.FlipAndGo;
    }

    private static void ValidateSatelliteTicket(TournamentEntry entry, List<string> errors)
    {
        if (!IsSatelliteFormat(entry.Format))
        {
            return;
        }

        if (HasNonTicketPrizeValue(entry))
        {
            errors.Add("Satellite outcomes should use ticket value fields instead of cash prize fields.");
        }

        if (entry.TicketValueWon > 0m && !entry.TicketWon && !entry.Qualified)
        {
            errors.Add("Satellite ticket value requires ticket won or qualified to be checked.");
        }

        if ((entry.TicketWon || entry.Qualified) && entry.EffectiveTicketValueWon <= 0m)
        {
            errors.Add("Satellite ticket value or target buy-in is required when a ticket is won.");
        }

        if (entry.TicketConvertedRealized && entry.EffectiveTicketValueWon <= 0m)
        {
            errors.Add("Realized satellite tickets require a ticket value or target buy-in.");
        }

        if ((entry.TicketWon || entry.Qualified || entry.TicketValueWon > 0m || entry.TargetEventBuyIn > 0m)
            && string.IsNullOrWhiteSpace(entry.TargetEventName)
            && entry.TargetEventBuyIn <= 0m)
        {
            errors.Add("Satellite target event name or target buy-in is required.");
        }
    }

    private static bool IsSatelliteFormat(TournamentFormat format)
    {
        return format is TournamentFormat.Satellite
            or TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.WSOPExpress;
    }

    private static bool HasFinishedResult(TournamentEntry entry)
    {
        return HasPrizeValue(entry)
            || entry.TicketWon
            || entry.Qualified
            || entry.TicketConvertedRealized
            || entry.Placement.HasValue
            || entry.ITM
            || entry.FinalTable;
    }

    private static bool HasPrizeValue(TournamentEntry entry)
    {
        return HasNonTicketPrizeValue(entry)
            || entry.TicketValueWon > 0m;
    }

    private static bool HasNonTicketPrizeValue(TournamentEntry entry)
    {
        return entry.CashPrize > 0m
            || entry.BountyTicketValue > 0m
            || entry.TournamentDollarsWon > 0m
            || entry.CashDollarsWon > 0m
            || entry.RegularCashPrize > 0m
            || entry.MysteryBountyPrize > 0m
            || entry.BountyPrize > 0m
            || entry.PrizeWon > 0m;
    }
}
