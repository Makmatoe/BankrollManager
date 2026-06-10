using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class PlatformCatalog
{
    private static readonly IReadOnlyList<Platform> AllPlatforms = Enum.GetValues<Platform>();
    private static readonly IReadOnlyList<TournamentCategory> AllTournamentCategories = Enum.GetValues<TournamentCategory>();
    private static readonly IReadOnlyList<TournamentFormat> AllTournamentFormats = Enum.GetValues<TournamentFormat>();
    private static readonly IReadOnlyList<CashFormat> AllCashFormats = Enum.GetValues<CashFormat>();

    public static IReadOnlyList<Platform> EnabledPlatforms(BankrollSettings settings, Platform? includePlatform = null)
    {
        settings.EnsureDefaults();
        var platforms = settings.EnabledPlatforms
            .Where(Enum.IsDefined)
            .Distinct()
            .ToList();

        if (includePlatform is { } platform && !platforms.Contains(platform))
        {
            platforms.Add(platform);
        }

        return platforms.Count == 0 ? AllPlatforms : platforms;
    }

    public static IReadOnlyList<TournamentCategory> TournamentCategoriesFor(Platform platform)
    {
        return platform switch
        {
            Platform.Unibet =>
            [
                TournamentCategory.MainGrind,
                TournamentCategory.FlipSatellite,
                TournamentCategory.HexaProSng,
                TournamentCategory.Reserve,
                TournamentCategory.Other
            ],
            Platform.HollandCasino =>
            [
                TournamentCategory.MainGrind,
                TournamentCategory.TowerShot,
                TournamentCategory.Reserve,
                TournamentCategory.Other
            ],
            Platform.GGPoker =>
            [
                TournamentCategory.MainGrind,
                TournamentCategory.FlipSatellite,
                TournamentCategory.CashPractice,
                TournamentCategory.Reserve,
                TournamentCategory.Other
            ],
            Platform.Other => AllTournamentCategories,
            _ => AllTournamentCategories
        };
    }

    public static IReadOnlyList<TournamentFormat> TournamentFormatsFor(Platform platform)
    {
        return platform switch
        {
            Platform.Unibet =>
            [
                TournamentFormat.MTT,
                TournamentFormat.Satellite,
                TournamentFormat.Flip,
                TournamentFormat.HexaPro,
                TournamentFormat.SNG,
                TournamentFormat.Other
            ],
            Platform.HollandCasino =>
            [
                TournamentFormat.MTT,
                TournamentFormat.Satellite,
                TournamentFormat.Tower,
                TournamentFormat.Brawl,
                TournamentFormat.BattleRoyale,
                TournamentFormat.SNG,
                TournamentFormat.Other
            ],
            Platform.GGPoker =>
            [
                TournamentFormat.MTT,
                TournamentFormat.Satellite,
                TournamentFormat.TurboSatellite,
                TournamentFormat.TargetStackSatellite,
                TournamentFormat.FlashSatellite,
                TournamentFormat.Freezeout,
                TournamentFormat.ReEntry,
                TournamentFormat.RebuyAddon,
                TournamentFormat.PKO,
                TournamentFormat.MysteryBounty,
                TournamentFormat.SpinAndGold,
                TournamentFormat.FlipAndGo,
                TournamentFormat.MysteryBattleRoyale,
                TournamentFormat.AoFSitAndGo,
                TournamentFormat.GGMasters,
                TournamentFormat.GGMillion,
                TournamentFormat.WSOPExpress,
                TournamentFormat.SNG,
                TournamentFormat.Other
            ],
            Platform.Other => AllTournamentFormats,
            _ => AllTournamentFormats
        };
    }

    public static IReadOnlyList<CashFormat> CashFormatsFor(Platform platform)
    {
        return platform switch
        {
            Platform.Unibet =>
            [
                CashFormat.HoldemCash,
                CashFormat.OmahaCash,
                CashFormat.Other
            ],
            Platform.HollandCasino =>
            [
                CashFormat.HoldemCash,
                CashFormat.OmahaCash,
                CashFormat.Other
            ],
            Platform.GGPoker =>
            [
                CashFormat.HoldemCash,
                CashFormat.OmahaCash,
                CashFormat.PLO5,
                CashFormat.PLO6,
                CashFormat.ShortDeck,
                CashFormat.RushAndCashHoldem,
                CashFormat.RushAndCashOmaha,
                CashFormat.AllInOrFoldHoldem,
                CashFormat.AllInOrFoldOmaha,
                CashFormat.Other
            ],
            Platform.Other => AllCashFormats,
            _ => AllCashFormats
        };
    }
}
