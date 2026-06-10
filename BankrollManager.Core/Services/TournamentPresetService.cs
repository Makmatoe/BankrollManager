using System.Globalization;
using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class TournamentPresetService
{
    public static TournamentPreset UpsertFromEntry(
        IList<TournamentPreset> presets,
        TournamentEntry entry,
        string name,
        DateTime utcNow)
    {
        var draft = CreateFromEntry(entry, name, utcNow);
        var existing = presets.FirstOrDefault(preset => SameTemplateKey(preset, draft));
        if (existing is null)
        {
            presets.Add(draft);
            return draft;
        }

        CopyPreset(draft, existing);
        existing.UpdatedUtc = utcNow;
        return existing;
    }

    public static TournamentPreset CreateFromEntry(TournamentEntry entry, string name, DateTime utcNow)
    {
        var cleanedName = CleanName(name);
        if (string.IsNullOrWhiteSpace(cleanedName))
        {
            cleanedName = CleanName(entry.EventName);
        }

        if (string.IsNullOrWhiteSpace(cleanedName))
        {
            cleanedName = entry.Format.ToString();
        }

        return new TournamentPreset
        {
            Name = cleanedName,
            EventName = cleanedName,
            Platform = entry.Platform,
            Category = entry.Category,
            Format = entry.Format,
            Currency = entry.Currency,
            EventTag = entry.EventTag,
            IsPromoFreebieTicketEvent = entry.IsPromoFreebieTicketEvent,
            BuyIn = entry.BuyIn,
            FeeRake = entry.FeeRake,
            PlannedBullets = Math.Max(1, entry.PlannedBullets),
            ActualBullets = Math.Max(1, entry.ActualBullets),
            AddOnsRebuys = entry.AddOnsRebuys,
            BountyTicketValue = entry.BountyTicketValue,
            TicketBuyInValue = entry.TicketBuyInValue,
            TicketBuyInPlatform = entry.TicketBuyInPlatform,
            TicketValueWon = entry.TicketValueWon,
            CashPrize = entry.CashPrize,
            TournamentDollarsWon = entry.TournamentDollarsWon,
            CashDollarsWon = entry.CashDollarsWon,
            RegularCashPrize = entry.RegularCashPrize,
            MysteryBountyPrize = entry.MysteryBountyPrize,
            BountyPhaseReached = entry.BountyPhaseReached,
            KnockoutsAfterBountyPhase = entry.KnockoutsAfterBountyPhase,
            MysteryBountyNotes = entry.MysteryBountyNotes,
            BountyPrize = entry.BountyPrize,
            Knockouts = entry.Knockouts,
            SpinPlayerCount = entry.SpinPlayerCount,
            InsuranceUsed = entry.InsuranceUsed,
            InsuranceCost = entry.InsuranceCost,
            MultiplierHit = entry.MultiplierHit,
            PrizeWon = entry.PrizeWon,
            FlipBuyInPerStack = entry.FlipBuyInPerStack,
            FlipStacksBought = entry.FlipStacksBought,
            FlipPhaseWon = entry.FlipPhaseWon,
            GoPhaseReached = entry.GoPhaseReached,
            RushStageSurvived = entry.RushStageSurvived,
            BattleRoyaleFinalTableReached = entry.BattleRoyaleFinalTableReached,
            TargetEventName = entry.TargetEventName,
            TargetEventBuyIn = entry.TargetEventBuyIn,
            TicketWon = entry.TicketWon,
            Qualified = entry.Qualified,
            TicketConvertedRealized = entry.TicketConvertedRealized,
            WsopExpressStepNumber = entry.WsopExpressStepNumber,
            TicketUsedValue = entry.TicketUsedValue,
            TargetPackageEvent = entry.TargetPackageEvent,
            FieldSize = entry.FieldSize,
            PreGameFocus = entry.PreGameFocus,
            Tags = entry.Tags,
            Notes = entry.Notes,
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow
        };
    }

    public static TournamentEntry CreateEntry(TournamentPreset preset, DateTime localNow)
    {
        return new TournamentEntry
        {
            Date = DateOnly.FromDateTime(localNow),
            RegistrationTime = TimeOnly.FromDateTime(localNow),
            Status = TournamentStatus.Registered,
            Platform = preset.Platform,
            Category = preset.Category,
            Format = preset.Format,
            EventName = string.IsNullOrWhiteSpace(preset.EventName) ? preset.Name : preset.EventName,
            Currency = preset.Currency,
            EventTag = preset.EventTag,
            IsPromoFreebieTicketEvent = preset.IsPromoFreebieTicketEvent,
            BuyIn = preset.BuyIn,
            FeeRake = preset.FeeRake,
            PlannedBullets = Math.Max(1, preset.PlannedBullets),
            ActualBullets = Math.Max(1, preset.ActualBullets),
            AddOnsRebuys = preset.AddOnsRebuys,
            BountyTicketValue = preset.BountyTicketValue,
            TicketBuyInValue = preset.TicketBuyInValue,
            TicketBuyInPlatform = preset.TicketBuyInPlatform,
            TicketValueWon = preset.TicketValueWon,
            CashPrize = preset.CashPrize,
            TournamentDollarsWon = preset.TournamentDollarsWon,
            CashDollarsWon = preset.CashDollarsWon,
            RegularCashPrize = preset.RegularCashPrize,
            MysteryBountyPrize = preset.MysteryBountyPrize,
            BountyPhaseReached = preset.BountyPhaseReached,
            KnockoutsAfterBountyPhase = preset.KnockoutsAfterBountyPhase,
            MysteryBountyNotes = preset.MysteryBountyNotes,
            BountyPrize = preset.BountyPrize,
            Knockouts = preset.Knockouts,
            SpinPlayerCount = preset.SpinPlayerCount,
            InsuranceUsed = preset.InsuranceUsed,
            InsuranceCost = preset.InsuranceCost,
            MultiplierHit = preset.MultiplierHit,
            PrizeWon = preset.PrizeWon,
            FlipBuyInPerStack = preset.FlipBuyInPerStack,
            FlipStacksBought = preset.FlipStacksBought,
            FlipPhaseWon = preset.FlipPhaseWon,
            GoPhaseReached = preset.GoPhaseReached,
            RushStageSurvived = preset.RushStageSurvived,
            BattleRoyaleFinalTableReached = preset.BattleRoyaleFinalTableReached,
            TargetEventName = preset.TargetEventName,
            TargetEventBuyIn = preset.TargetEventBuyIn,
            TicketWon = preset.TicketWon,
            Qualified = preset.Qualified,
            TicketConvertedRealized = preset.TicketConvertedRealized,
            WsopExpressStepNumber = preset.WsopExpressStepNumber,
            TicketUsedValue = preset.TicketUsedValue,
            TargetPackageEvent = preset.TargetPackageEvent,
            FieldSize = preset.FieldSize,
            PreGameFocus = preset.PreGameFocus,
            Tags = AppendTag(preset.Tags, "Preset"),
            Notes = preset.Notes
        };
    }

    public static string DisplayName(TournamentPreset preset, BankrollSettings settings)
    {
        var label = CleanName(preset.Name);
        if (string.IsNullOrWhiteSpace(label))
        {
            label = CleanName(preset.EventName);
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            label = preset.Format.ToString();
        }

        var parts = new List<string> { Money(preset.BuyIn, settings), label };
        if (preset.TicketValueWon > 0m)
        {
            parts.Add($"{Money(preset.TicketValueWon, settings)} Ticket");
        }
        else if (preset.BountyTicketValue > 0m)
        {
            parts.Add($"{Money(preset.BountyTicketValue, settings)} Bounty");
        }
        else if (preset.CashPrize > 0m)
        {
            parts.Add($"{Money(preset.CashPrize, settings)} Prize");
        }
        else if (preset.PrizeWon > 0m)
        {
            parts.Add($"{Money(preset.PrizeWon, settings)} Prize");
        }
        else if (preset.RegularCashPrize + preset.MysteryBountyPrize + preset.BountyPrize > 0m)
        {
            parts.Add($"{Money(preset.RegularCashPrize + preset.MysteryBountyPrize + preset.BountyPrize, settings)} Prize");
        }

        return string.Join(" ", parts);
    }

    private static bool SameTemplateKey(TournamentPreset left, TournamentPreset right)
    {
        return left.Platform == right.Platform
            && left.Category == right.Category
            && left.Format == right.Format
            && left.BuyIn == right.BuyIn
            && string.Equals(CleanName(left.Name), CleanName(right.Name), StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyPreset(TournamentPreset source, TournamentPreset target)
    {
        target.Name = source.Name;
        target.EventName = source.EventName;
        target.Platform = source.Platform;
        target.Category = source.Category;
        target.Format = source.Format;
        target.BuyIn = source.BuyIn;
        target.Currency = source.Currency;
        target.EventTag = source.EventTag;
        target.IsPromoFreebieTicketEvent = source.IsPromoFreebieTicketEvent;
        target.FeeRake = source.FeeRake;
        target.PlannedBullets = source.PlannedBullets;
        target.ActualBullets = source.ActualBullets;
        target.AddOnsRebuys = source.AddOnsRebuys;
        target.BountyTicketValue = source.BountyTicketValue;
        target.TicketBuyInValue = source.TicketBuyInValue;
        target.TicketBuyInPlatform = source.TicketBuyInPlatform;
        target.TicketValueWon = source.TicketValueWon;
        target.CashPrize = source.CashPrize;
        target.TournamentDollarsWon = source.TournamentDollarsWon;
        target.CashDollarsWon = source.CashDollarsWon;
        target.RegularCashPrize = source.RegularCashPrize;
        target.MysteryBountyPrize = source.MysteryBountyPrize;
        target.BountyPhaseReached = source.BountyPhaseReached;
        target.KnockoutsAfterBountyPhase = source.KnockoutsAfterBountyPhase;
        target.MysteryBountyNotes = source.MysteryBountyNotes;
        target.BountyPrize = source.BountyPrize;
        target.Knockouts = source.Knockouts;
        target.SpinPlayerCount = source.SpinPlayerCount;
        target.InsuranceUsed = source.InsuranceUsed;
        target.InsuranceCost = source.InsuranceCost;
        target.MultiplierHit = source.MultiplierHit;
        target.PrizeWon = source.PrizeWon;
        target.FlipBuyInPerStack = source.FlipBuyInPerStack;
        target.FlipStacksBought = source.FlipStacksBought;
        target.FlipPhaseWon = source.FlipPhaseWon;
        target.GoPhaseReached = source.GoPhaseReached;
        target.RushStageSurvived = source.RushStageSurvived;
        target.BattleRoyaleFinalTableReached = source.BattleRoyaleFinalTableReached;
        target.TargetEventName = source.TargetEventName;
        target.TargetEventBuyIn = source.TargetEventBuyIn;
        target.TicketWon = source.TicketWon;
        target.Qualified = source.Qualified;
        target.TicketConvertedRealized = source.TicketConvertedRealized;
        target.WsopExpressStepNumber = source.WsopExpressStepNumber;
        target.TicketUsedValue = source.TicketUsedValue;
        target.TargetPackageEvent = source.TargetPackageEvent;
        target.FieldSize = source.FieldSize;
        target.PreGameFocus = source.PreGameFocus;
        target.Tags = source.Tags;
        target.Notes = source.Notes;
    }

    private static string AppendTag(string tags, string tag)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return tag;
        }

        return tags.Contains(tag, StringComparison.OrdinalIgnoreCase) ? tags.Trim() : $"{tags.Trim()}, {tag}";
    }

    private static string CleanName(string value)
    {
        return string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string Money(decimal value, BankrollSettings settings)
    {
        var sign = value < 0m ? "-" : string.Empty;
        var currency = string.IsNullOrWhiteSpace(settings.CurrencySymbol) ? "\u20ac" : settings.CurrencySymbol;
        return $"{sign}{currency}{Math.Abs(value).ToString("0.00", CultureInfo.CurrentCulture)}";
    }
}
