using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.Tests;

internal static class ReleaseScaleDataFactory
{
    private static readonly Platform[] Platforms = Enum.GetValues<Platform>();

    private static readonly TournamentFormat[] TournamentFormats =
    [
        TournamentFormat.MTT,
        TournamentFormat.PKO,
        TournamentFormat.MysteryBounty,
        TournamentFormat.Satellite,
        TournamentFormat.TargetStackSatellite,
        TournamentFormat.SpinAndGold,
        TournamentFormat.Flip,
        TournamentFormat.FlipAndGo,
        TournamentFormat.ReEntry,
        TournamentFormat.Freezeout
    ];

    private static readonly CashFormat[] CashFormats =
    [
        CashFormat.HoldemCash,
        CashFormat.OmahaCash,
        CashFormat.RushAndCashHoldem,
        CashFormat.AllInOrFoldHoldem
    ];

    private static readonly LedgerType[] LedgerTypes =
    [
        LedgerType.Deposit,
        LedgerType.Withdrawal,
        LedgerType.Bonus,
        LedgerType.Rakeback,
        LedgerType.TicketCredit,
        LedgerType.TransferIn,
        LedgerType.TransferOut,
        LedgerType.Correction
    ];

    public static BankrollData Create(
        int tournamentCount = 5_000,
        int cashSessionCount = 2_000,
        int ledgerEntryCount = 2_000)
    {
        var startDate = new DateOnly(2025, 1, 1);
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 10_000m,
                CurrencySymbol = "$",
                FirstRunSetupCompleted = true,
                DailyStopLossAmount = 150m,
                NormalMttMaxRiskPercent = 5m,
                FlipMaxRiskPercent = 1m,
                SngHexaProMaxRiskPercent = 3m,
                ShotTowerMaxRiskPercent = 2m,
                CashSessionMaxRiskPercent = 6m,
                ProtectModeBelowBankroll = 1_000m,
                GreenLightShotBankroll = 2_500m,
                MoveUpReviewBankroll = 5_000m
            },
            TournamentEntries = Enumerable.Range(0, tournamentCount)
                .Select(index => BuildTournament(index, startDate))
                .ToList(),
            CashSessions = Enumerable.Range(0, cashSessionCount)
                .Select(index => BuildCashSession(index, startDate))
                .ToList(),
            LedgerEntries = Enumerable.Range(0, ledgerEntryCount)
                .Select(index => BuildLedgerEntry(index, startDate))
                .ToList(),
            TournamentPresets =
            [
                new TournamentPreset
                {
                    Name = "Favorite release flip",
                    EventName = "Favorite release flip",
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.FlipSatellite,
                    Format = TournamentFormat.Flip,
                    BuyIn = 0.25m,
                    ActualBullets = 1,
                    IsFavorite = true,
                    SortOrder = 10
                },
                new TournamentPreset
                {
                    Name = "Daily release MTT",
                    EventName = "Daily release MTT",
                    Platform = Platform.GGPoker,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    BuyIn = 2.50m,
                    ActualBullets = 1,
                    SortOrder = 20
                }
            ]
        };

        data.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);
        ReconcileWallets(data, startDate.AddDays(730));
        return data;
    }

    private static TournamentEntry BuildTournament(int index, DateOnly startDate)
    {
        var format = TournamentFormats[index % TournamentFormats.Length];
        var date = startDate.AddDays(index % 640);
        var registrationTime = new TimeOnly(8 + index % 12, index % 60);
        var (finishedDate, finishedTime) = FinishAt(
            date,
            registrationTime,
            IsFastFinish(format) ? 1 : 75 + index % 210);
        var category = format is TournamentFormat.Flip or TournamentFormat.FlipAndGo
            or TournamentFormat.Satellite or TournamentFormat.TargetStackSatellite
            ? TournamentCategory.FlipSatellite
            : index % 7 == 0
                ? TournamentCategory.TowerShot
                : TournamentCategory.MainGrind;
        var entry = new TournamentEntry
        {
            Date = date,
            RegistrationTime = registrationTime,
            Status = TournamentStatus.Finished,
            FinishedDate = finishedDate,
            FinishedTime = finishedTime,
            Platform = Platforms[index % Platforms.Length],
            Category = category,
            Format = format,
            EventName = $"Release Scale {format} #{index:00000}",
            Currency = "USD",
            EventTag = EventTagFor(format),
            BuyIn = 1m + index % 15,
            FeeRake = index % 5 == 0 ? 0.10m : 0m,
            PlannedBullets = 1 + index % 3,
            ActualBullets = 1 + index % 3,
            AddOnsRebuys = index % 11 == 0 ? 1m : 0m,
            FieldSize = 100 + index % 900,
            Placement = 1 + index % (100 + index % 900),
            ITM = index % 4 == 0,
            FinalTable = index % 37 == 0,
            Tags = index % 13 == 0 ? "release, regression" : "release",
            Notes = index % 17 == 0 ? "highlight: deterministic scale row" : string.Empty
        };

        ApplyFormatSpecificResult(entry, index);

        if (index % 19 == 0)
        {
            entry.TicketBuyInValue = Math.Min(entry.TotalCost, Math.Max(0.25m, entry.TotalCost / 2m));
            entry.TicketBuyInPlatform = Platforms[(index + 1) % Platforms.Length];
        }

        return entry;
    }

    private static void ApplyFormatSpecificResult(TournamentEntry entry, int index)
    {
        switch (entry.Format)
        {
            case TournamentFormat.Satellite:
            case TournamentFormat.TargetStackSatellite:
                entry.TargetEventName = $"Target Event {index % 50}";
                entry.TargetEventBuyIn = 5m + index % 25;
                if (index % 4 != 0)
                {
                    entry.TicketValueWon = entry.TargetEventBuyIn;
                    entry.TicketWon = true;
                    entry.Qualified = index % 8 == 0;
                    entry.TicketConvertedRealized = index % 6 == 3;
                }
                break;
            case TournamentFormat.SpinAndGold:
                entry.SpinPlayerCount = index % 2 == 0 ? 3 : 6;
                entry.MultiplierHit = 2 + index % 5;
                entry.PrizeWon = index % 3 == 0 ? entry.BuyIn * entry.MultiplierHit : 0m;
                break;
            case TournamentFormat.FlipAndGo:
                entry.BuyIn = 0m;
                entry.FlipBuyInPerStack = 0.25m + (index % 5 * 0.05m);
                entry.FlipStacksBought = 1 + index % 3;
                entry.FlipPhaseWon = index % 2 == 0;
                entry.GoPhaseReached = entry.FlipPhaseWon && index % 4 == 0;
                entry.PrizeWon = entry.GoPhaseReached ? entry.FlipCost * (2 + index % 6) : 0m;
                break;
            case TournamentFormat.Flip:
                entry.PrizeWon = index % 3 == 0 ? entry.TotalCost * 4m : 0m;
                break;
            case TournamentFormat.PKO:
                entry.CashPrize = index % 3 == 0 ? entry.TotalCost * 2m : 0m;
                entry.BountyPrize = index % 5 == 0 ? 1.50m : 0m;
                entry.Knockouts = index % 5 == 0 ? 2 : null;
                break;
            case TournamentFormat.MysteryBounty:
                entry.RegularCashPrize = index % 3 == 0 ? entry.TotalCost : 0m;
                entry.MysteryBountyPrize = index % 5 == 0 ? 3m : 0m;
                entry.BountyPhaseReached = entry.MysteryBountyPrize > 0m;
                break;
            default:
                entry.CashPrize = (index % 5) switch
                {
                    0 => entry.TotalCost * 3m,
                    1 => entry.TotalCost,
                    2 => entry.TotalCost / 2m,
                    _ => 0m
                };
                break;
        }
    }

    private static CashSession BuildCashSession(int index, DateOnly startDate)
    {
        var date = startDate.AddDays(index % 640);
        var startTime = new TimeOnly(9 + index % 10, index % 60);
        var minutes = 45 + index % 240;
        var (closedDate, closedTime) = FinishAt(date, startTime, minutes);
        var buyIn = 20m + (index % 10 * 5m);
        var reloads = index % 6 == 0 ? 10m : 0m;
        var result = (index % 9 - 4) * 5m;

        return new CashSession
        {
            Date = date,
            SessionTime = startTime,
            Status = CashSessionStatus.Finished,
            ClosedDate = closedDate,
            ClosedTime = closedTime,
            Platform = Platforms[(index + 2) % Platforms.Length],
            Format = CashFormats[index % CashFormats.Length],
            Game = index % 3 == 0 ? "Rush" : "Cash",
            Stakes = $"0.{index % 5 + 1:00}/0.{index % 5 + 2:00}",
            SmallBlindAmount = 0.01m + index % 5 * 0.01m,
            BigBlindAmount = 0.02m + index % 5 * 0.02m,
            StartStackBuyIn = buyIn,
            Reloads = reloads,
            ReloadCap = reloads + 20m,
            Cashout = Math.Max(0m, buyIn + reloads + result),
            CashDropWon = index % 31 == 0 ? 2m : 0m,
            JackpotFortunePrizeWon = index % 97 == 0 ? 5m : 0m,
            Minutes = minutes,
            Hands = 100 + index % 700,
            Notes = index % 23 == 0 ? "leak: scale test review marker" : string.Empty
        };
    }

    private static LedgerEntry BuildLedgerEntry(int index, DateOnly startDate)
    {
        var type = LedgerTypes[index % LedgerTypes.Length];
        var amount = type == LedgerType.Correction
            ? index % 2 == 0 ? 1.25m : -0.75m
            : 10m + index % 90;

        return new LedgerEntry
        {
            Date = startDate.AddDays(index % 640),
            Type = type,
            Platform = Platforms[(index + 3) % Platforms.Length],
            Description = $"Release scale {type} #{index:00000}",
            Amount = amount,
            Category = index % 5 == 0 ? TournamentCategory.FlipSatellite : TournamentCategory.MainGrind,
            Notes = type == LedgerType.TicketCredit ? "ticket credit regression" : string.Empty
        };
    }

    private static void ReconcileWallets(BankrollData data, DateOnly reconciledAt)
    {
        foreach (var summary in BankrollCalculator.GetPlatformSummaries(data))
        {
            if (!Enum.TryParse<Platform>(summary.Name, out var platform))
            {
                continue;
            }

            var wallet = data.PlatformWallets.Single(item => item.Platform == platform);
            wallet.ActualCashBalance = summary.WalletCashBalance;
            wallet.AcceptedCashDifference = null;
            wallet.LastUpdatedDate = reconciledAt;
            wallet.Notes = "release scale reconciled";
        }
    }

    private static (DateOnly Date, TimeOnly Time) FinishAt(DateOnly date, TimeOnly time, int minutes)
    {
        var finishedAt = date.ToDateTime(time).AddMinutes(minutes);
        return (DateOnly.FromDateTime(finishedAt), TimeOnly.FromDateTime(finishedAt));
    }

    private static bool IsFastFinish(TournamentFormat format)
    {
        return format is TournamentFormat.Flip
            or TournamentFormat.FlipAndGo
            or TournamentFormat.Satellite
            or TournamentFormat.TargetStackSatellite;
    }

    private static EventTag EventTagFor(TournamentFormat format)
    {
        return format switch
        {
            TournamentFormat.FlipAndGo => EventTag.FlipAndGo,
            TournamentFormat.SpinAndGold => EventTag.SpinAndGold,
            TournamentFormat.MysteryBounty => EventTag.MysteryBounty,
            TournamentFormat.Satellite or TournamentFormat.TargetStackSatellite => EventTag.Ticket,
            _ => EventTag.None
        };
    }
}
