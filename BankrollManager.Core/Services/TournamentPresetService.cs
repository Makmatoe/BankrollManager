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
            draft.SortOrder = NextSortOrder(presets);
            presets.Add(draft);
            return draft;
        }

        CopyPreset(draft, existing, preserveIdentity: true);
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
            TicketBuyInValue = preset.TicketBuyInValue,
            TicketBuyInPlatform = preset.TicketBuyInPlatform,
            MysteryBountyNotes = preset.MysteryBountyNotes,
            SpinPlayerCount = preset.SpinPlayerCount,
            InsuranceUsed = preset.InsuranceUsed,
            InsuranceCost = preset.InsuranceCost,
            FlipBuyInPerStack = preset.FlipBuyInPerStack,
            FlipStacksBought = preset.FlipStacksBought,
            TargetEventName = preset.TargetEventName,
            TargetEventBuyIn = preset.TargetEventBuyIn,
            WsopExpressStepNumber = preset.WsopExpressStepNumber,
            TicketUsedValue = preset.TicketUsedValue,
            TargetPackageEvent = preset.TargetPackageEvent,
            FieldSize = preset.FieldSize,
            PreGameFocus = preset.PreGameFocus,
            Tags = AppendTag(preset.Tags, "Preset"),
            Notes = preset.Notes
        };
    }

    public static TournamentEntry CreateTemplateEntry(TournamentPreset preset, DateTime localNow)
    {
        var entry = CreateEntry(preset, localNow);
        entry.BountyTicketValue = preset.BountyTicketValue;
        entry.TicketValueWon = preset.TicketValueWon;
        entry.CashPrize = preset.CashPrize;
        entry.TournamentDollarsWon = preset.TournamentDollarsWon;
        entry.CashDollarsWon = preset.CashDollarsWon;
        entry.RegularCashPrize = preset.RegularCashPrize;
        entry.MysteryBountyPrize = preset.MysteryBountyPrize;
        entry.BountyPhaseReached = preset.BountyPhaseReached;
        entry.KnockoutsAfterBountyPhase = preset.KnockoutsAfterBountyPhase;
        entry.BountyPrize = preset.BountyPrize;
        entry.Knockouts = preset.Knockouts;
        entry.MultiplierHit = preset.MultiplierHit;
        entry.PrizeWon = preset.PrizeWon;
        entry.FlipPhaseWon = preset.FlipPhaseWon;
        entry.GoPhaseReached = preset.GoPhaseReached;
        entry.RushStageSurvived = preset.RushStageSurvived;
        entry.BattleRoyaleFinalTableReached = preset.BattleRoyaleFinalTableReached;
        entry.TicketWon = preset.TicketWon;
        entry.Qualified = preset.Qualified;
        entry.TicketConvertedRealized = preset.TicketConvertedRealized;
        entry.ITM = HasStoredResult(preset);
        if (HasStoredResult(preset))
        {
            entry.Status = TournamentStatus.Finished;
            entry.FinishedDate = entry.Date;
            entry.FinishedTime = DefaultFinishedAt(
                entry.Date,
                entry.RegistrationTime ?? TimeOnly.FromDateTime(localNow),
                entry).Time;
        }

        return entry;
    }

    public static TournamentEntry CreateQuickEntry(
        TournamentPreset preset,
        DateOnly date,
        TimeOnly registrationTime,
        bool finished,
        decimal winAmount)
    {
        var defaultFinish = DefaultFinishedAt(date, registrationTime, preset);
        return CreateQuickEntry(
            preset,
            new TournamentQuickEntryRequest
            {
                RegistrationDate = date,
                RegistrationTime = registrationTime,
                Finished = finished,
                FinishedDate = finished ? defaultFinish.Date : null,
                FinishedTime = finished ? defaultFinish.Time : null,
                ResultKind = TournamentQuickResultKind.Auto,
                ResultAmount = winAmount
            });
    }

    public static TournamentEntry CreateQuickEntry(TournamentPreset preset, TournamentQuickEntryRequest request)
    {
        var entry = CreateEntry(preset, request.RegistrationDate.ToDateTime(request.RegistrationTime));
        ClearTicketBuyIn(entry);

        entry.Date = request.RegistrationDate;
        entry.RegistrationTime = request.RegistrationTime;
        ApplyTicketBuyIn(entry, request.TicketBuyInValue, request.TicketBuyInPlatform);

        if (!request.Finished)
        {
            entry.Status = TournamentStatus.Registered;
            entry.FinishedDate = null;
            entry.FinishedTime = null;
            ClearFinishedResultFields(entry);
            return entry;
        }

        var defaultFinish = DefaultFinishedAt(request.RegistrationDate, request.RegistrationTime, entry);
        ApplyFinish(
            entry,
            new TournamentFinishRequest
            {
                FinishedDate = request.FinishedDate ?? defaultFinish.Date,
                FinishedTime = request.FinishedTime ?? defaultFinish.Time,
                ResultKind = request.ResultKind,
                ResultAmount = request.ResultAmount,
                Placement = request.Placement,
                FieldSize = request.FieldSize,
                ITM = request.ITM,
                FinalTable = request.FinalTable,
                FlipPhaseWon = request.FlipPhaseWon,
                GoPhaseReached = request.GoPhaseReached
            });
        return entry;
    }

    public static void ApplyFinish(TournamentEntry entry, TournamentFinishRequest request)
    {
        entry.Status = TournamentStatus.Finished;
        entry.FinishedDate = request.FinishedDate;
        entry.FinishedTime = request.FinishedTime;
        ClearFinishedResultFields(entry);

        entry.Placement = request.Placement;
        entry.FieldSize = request.FieldSize;
        entry.ITM = request.ITM;
        entry.FinalTable = request.FinalTable;
        entry.FlipPhaseWon = request.FlipPhaseWon;
        entry.GoPhaseReached = request.GoPhaseReached && entry.FlipPhaseWon;

        ApplyQuickResult(entry, request.ResultKind, Math.Max(0m, request.ResultAmount));
    }

    public static void ApplyTicketBuyIn(TournamentEntry entry, decimal amount, Platform? platform)
    {
        if (amount <= 0m)
        {
            ClearTicketBuyIn(entry);
            return;
        }

        entry.TicketBuyInValue = amount;
        entry.TicketBuyInPlatform = platform ?? entry.Platform;
    }

    public static bool IsBulkFinishCandidate(TournamentEntry entry)
    {
        return entry.Status != TournamentStatus.Finished && IsFastFinishStyle(entry);
    }

    public static List<TournamentPreset> OrderedPresets(IEnumerable<TournamentPreset> presets)
    {
        return presets
            .OrderByDescending(preset => preset.IsFavorite)
            .ThenBy(preset => preset.SortOrder > 0 ? preset.SortOrder : int.MaxValue)
            .ThenBy(PresetSortName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void NormalizePresetOrder(IList<TournamentPreset> presets)
    {
        for (var index = 0; index < presets.Count; index++)
        {
            presets[index].SortOrder = (index + 1) * 10;
        }
    }

    public static TournamentPreset ClonePreset(TournamentPreset preset)
    {
        var clone = new TournamentPreset();
        CopyPreset(preset, clone, preserveIdentity: false);
        clone.Id = preset.Id;
        clone.IsFavorite = preset.IsFavorite;
        clone.SortOrder = preset.SortOrder;
        clone.CreatedUtc = preset.CreatedUtc;
        clone.UpdatedUtc = preset.UpdatedUtc;
        clone.LastUsedUtc = preset.LastUsedUtc;
        return clone;
    }

    public static void UpdateFromEntry(TournamentPreset target, TournamentEntry entry, string name, DateTime utcNow)
    {
        var source = CreateFromEntry(entry, name, utcNow);
        CopyPreset(source, target, preserveIdentity: true);
        target.UpdatedUtc = utcNow;
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

    private static void CopyPreset(TournamentPreset source, TournamentPreset target, bool preserveIdentity)
    {
        var id = target.Id;
        var isFavorite = target.IsFavorite;
        var sortOrder = target.SortOrder;
        var createdUtc = target.CreatedUtc;
        var lastUsedUtc = target.LastUsedUtc;

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
        target.UpdatedUtc = source.UpdatedUtc;

        if (preserveIdentity)
        {
            target.Id = id;
            target.IsFavorite = isFavorite;
            target.SortOrder = sortOrder;
            target.CreatedUtc = createdUtc;
            target.LastUsedUtc = lastUsedUtc;
            return;
        }

        target.Id = source.Id;
        target.IsFavorite = source.IsFavorite;
        target.SortOrder = source.SortOrder;
        target.CreatedUtc = source.CreatedUtc;
        target.LastUsedUtc = source.LastUsedUtc;
    }

    private static void ClearTicketBuyIn(TournamentEntry entry)
    {
        entry.TicketBuyInValue = 0m;
        entry.TicketBuyInPlatform = null;
    }

    private static void ClearFinishedResultFields(TournamentEntry entry)
    {
        entry.BountyTicketValue = 0m;
        entry.TicketValueWon = 0m;
        entry.CashPrize = 0m;
        entry.TournamentDollarsWon = 0m;
        entry.CashDollarsWon = 0m;
        entry.RegularCashPrize = 0m;
        entry.MysteryBountyPrize = 0m;
        entry.BountyPhaseReached = false;
        entry.KnockoutsAfterBountyPhase = null;
        entry.MysteryBountyNotes = string.Empty;
        entry.BountyPrize = 0m;
        entry.Knockouts = null;
        entry.MultiplierHit = 0m;
        entry.PrizeWon = 0m;
        entry.FlipPhaseWon = false;
        entry.GoPhaseReached = false;
        entry.RushStageSurvived = false;
        entry.BattleRoyaleFinalTableReached = false;
        entry.TicketWon = false;
        entry.Qualified = false;
        entry.TicketConvertedRealized = false;
        entry.TicketUsedValue = 0m;
        entry.Placement = null;
        entry.ITM = false;
        entry.FinalTable = false;
    }

    private static void ApplyQuickResult(TournamentEntry entry, TournamentQuickResultKind resultKind, decimal resultAmount)
    {
        if (resultAmount <= 0m || resultKind == TournamentQuickResultKind.None)
        {
            return;
        }

        var effectiveKind = resultKind == TournamentQuickResultKind.Auto
            ? DefaultResultKind(entry)
            : resultKind;

        if (effectiveKind == TournamentQuickResultKind.TicketWon)
        {
            entry.TicketValueWon = resultAmount;
            entry.TicketWon = true;
            if (string.IsNullOrWhiteSpace(entry.TargetEventName) && entry.TargetEventBuyIn <= 0m)
            {
                entry.TargetEventBuyIn = resultAmount;
            }
        }
        else if (effectiveKind == TournamentQuickResultKind.RealizedTicket)
        {
            entry.TicketValueWon = resultAmount;
            entry.TicketWon = true;
            entry.TicketConvertedRealized = true;
            if (string.IsNullOrWhiteSpace(entry.TargetEventName) && entry.TargetEventBuyIn <= 0m)
            {
                entry.TargetEventBuyIn = resultAmount;
            }
        }
        else if (entry.Format is TournamentFormat.SpinAndGold
            or TournamentFormat.FlipAndGo
            or TournamentFormat.MysteryBattleRoyale)
        {
            entry.PrizeWon = resultAmount;
        }
        else
        {
            entry.CashPrize = resultAmount;
        }

        entry.ITM = true;
    }

    private static TournamentQuickResultKind DefaultResultKind(TournamentEntry entry)
    {
        return IsSatelliteFormat(entry.Format)
            ? TournamentQuickResultKind.TicketWon
            : TournamentQuickResultKind.CashPrize;
    }

    private static bool IsSatelliteFormat(TournamentFormat format)
    {
        return format is TournamentFormat.Satellite
            or TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.WSOPExpress;
    }

    private static bool IsFastFinishStyle(TournamentEntry entry)
    {
        return entry.Category == TournamentCategory.FlipSatellite
            || entry.Format is TournamentFormat.Flip
                or TournamentFormat.FlipAndGo
                or TournamentFormat.Satellite
                or TournamentFormat.TurboSatellite
                or TournamentFormat.TargetStackSatellite
                or TournamentFormat.FlashSatellite
                or TournamentFormat.WSOPExpress
            || entry.EventTag is EventTag.FlipAndGo or EventTag.Ticket;
    }

    private static (DateOnly Date, TimeOnly Time) DefaultFinishedAt(
        DateOnly date,
        TimeOnly registrationTime,
        TournamentPreset preset)
    {
        var entry = new TournamentEntry
        {
            Category = preset.Category,
            Format = preset.Format,
            EventTag = preset.EventTag
        };

        return DefaultFinishedAt(date, registrationTime, entry);
    }

    private static (DateOnly Date, TimeOnly Time) DefaultFinishedAt(
        DateOnly date,
        TimeOnly registrationTime,
        TournamentEntry entry)
    {
        var finish = IsFastFinishStyle(entry)
            ? date.ToDateTime(registrationTime).AddMinutes(1)
            : date.ToDateTime(registrationTime);
        return (DateOnly.FromDateTime(finish), TimeOnly.FromDateTime(finish));
    }

    private static bool HasStoredResult(TournamentPreset preset)
    {
        return preset.BountyTicketValue > 0m
            || preset.TicketValueWon > 0m
            || preset.CashPrize > 0m
            || preset.TournamentDollarsWon > 0m
            || preset.CashDollarsWon > 0m
            || preset.RegularCashPrize > 0m
            || preset.MysteryBountyPrize > 0m
            || preset.BountyPrize > 0m
            || preset.MultiplierHit > 0m
            || preset.PrizeWon > 0m
            || preset.TicketWon
            || preset.Qualified
            || preset.TicketConvertedRealized
            || preset.FlipPhaseWon
            || preset.GoPhaseReached
            || preset.RushStageSurvived
            || preset.BattleRoyaleFinalTableReached;
    }

    private static int NextSortOrder(IEnumerable<TournamentPreset> presets)
    {
        var maximum = presets.Select(preset => preset.SortOrder).DefaultIfEmpty(0).Max();
        return maximum <= 0 ? 10 : maximum + 10;
    }

    private static string PresetSortName(TournamentPreset preset)
    {
        var name = CleanName(preset.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        name = CleanName(preset.EventName);
        return string.IsNullOrWhiteSpace(name) ? preset.Format.ToString() : name;
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
