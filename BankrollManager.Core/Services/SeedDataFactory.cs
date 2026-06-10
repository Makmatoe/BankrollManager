using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class SeedDataFactory
{
    public static BankrollData Create()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings(),
            LedgerEntries =
            [
                new()
                {
                    Date = new DateOnly(2026, 6, 5),
                    Type = LedgerType.Deposit,
                    Platform = Platform.HollandCasino,
                    Description = "5th June bankroll Max deposit EUR15",
                    Amount = 15.00m,
                    Category = TournamentCategory.CashPractice
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Type = LedgerType.Deposit,
                    Platform = Platform.Unibet,
                    Description = "Max weekly deposit EUR10",
                    Amount = 10.00m,
                    Category = TournamentCategory.MainGrind
                }
            ],
            TournamentEntries =
            [
                new()
                {
                    Date = new DateOnly(2026, 6, 7),
                    Platform = Platform.HollandCasino,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    EventName = "EUR0.50 BOUNTY HUNTER",
                    BuyIn = 0.50m,
                    PlannedBullets = 1,
                    ActualBullets = 1,
                    Placement = 96,
                    FieldSize = 100
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.Satellite,
                    EventName = "SuperMoon Qualifier",
                    BuyIn = 2.00m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    EventName = "EUR200 Hyper Deepstack",
                    BuyIn = 2.00m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.FlipSatellite,
                    Format = TournamentFormat.Flip,
                    EventName = "Supermoon Flip",
                    BuyIn = 0.40m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.Satellite,
                    EventName = "SuperMoon Qualifier",
                    BuyIn = 0.40m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    EventName = "Turbo R/A",
                    BuyIn = 1.00m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 9),
                    Platform = Platform.HollandCasino,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    EventName = "Bounty Hunter",
                    BuyIn = 0.50m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 9),
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.FlipSatellite,
                    Format = TournamentFormat.Flip,
                    EventName = "Supermoon Flip",
                    BuyIn = 0.40m,
                    PlannedBullets = 1,
                    ActualBullets = 1
                }
            ],
            CashSessions =
            [
                new()
                {
                    Date = new DateOnly(2026, 6, 7),
                    Platform = Platform.HollandCasino,
                    Game = "Cash",
                    Stakes = "EUR0.01/EUR0.02",
                    BigBlindAmount = 0.02m,
                    StartStackBuyIn = 0.90m,
                    Cashout = 0.00m,
                    Minutes = 34,
                    Hands = 68,
                    Notes = "Played at night, had some decent hands, was pretty aggressive on betting AF. Lost an all-in because I bluffed too hard. Downsize bluff bet to maybe get them to realize its a real bet instead of bluff."
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 7),
                    Platform = Platform.HollandCasino,
                    Game = "Cash",
                    Stakes = "EUR0.01/EUR0.02",
                    BigBlindAmount = 0.02m,
                    StartStackBuyIn = 2.00m,
                    Cashout = 2.10m,
                    Minutes = 17,
                    Hands = 23,
                    Notes = "This is the only winning session in the seed data."
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 8),
                    Platform = Platform.Unibet,
                    Game = "Cash",
                    Stakes = "EUR0.02/EUR0.04",
                    BigBlindAmount = 0.04m,
                    StartStackBuyIn = 4.00m,
                    Cashout = 0.58m,
                    Minutes = 44,
                    Hands = 47
                },
                new()
                {
                    Date = new DateOnly(2026, 6, 9),
                    Platform = Platform.HollandCasino,
                    Game = "Cash",
                    Stakes = "EUR0.05/EUR0.10",
                    BigBlindAmount = 0.10m,
                    StartStackBuyIn = 10.00m,
                    Cashout = 0.00m,
                    Notes = "The EUR20 was my threshold to withdraw, not my actual cashout. I went all-in and lost while running it twice. I finished the session at 0. This was a stop-loss / threshold mistake."
                }
            ]
        };

        BankrollCalculator.RecalculateTrackingFields(data);
        return data;
    }
}
