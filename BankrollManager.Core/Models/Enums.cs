namespace BankrollManager.Core.Models;

public enum Platform
{
    Unibet,
    HollandCasino,
    GGPoker,
    Other
}

public enum LedgerType
{
    Deposit,
    Withdrawal,
    Bonus,
    Rakeback,
    TicketCredit,
    TransferIn,
    TransferOut,
    Correction,
    Other
}

public enum TournamentCategory
{
    MainGrind,
    TowerShot,
    FlipSatellite,
    HexaProSng,
    CashPractice,
    Reserve,
    Other
}

public enum TournamentFormat
{
    MTT,
    Satellite,
    TurboSatellite,
    TargetStackSatellite,
    FlashSatellite,
    Freezeout,
    ReEntry,
    RebuyAddon,
    PKO,
    MysteryBounty,
    SpinAndGold,
    FlipAndGo,
    MysteryBattleRoyale,
    AoFSitAndGo,
    GGMasters,
    GGMillion,
    WSOPExpress,
    Flip,
    Tower,
    Brawl,
    BattleRoyale,
    HexaPro,
    SNG,
    Other
}

public enum CashFormat
{
    HoldemCash,
    OmahaCash,
    PLO5,
    PLO6,
    ShortDeck,
    RushAndCashHoldem,
    RushAndCashOmaha,
    AllInOrFoldHoldem,
    AllInOrFoldOmaha,
    Other
}

public enum EventTag
{
    None,
    DailyGuarantee,
    BountyHunters,
    MysteryBounty,
    GGMasters,
    GGMillion,
    WSOPExpress,
    FlipAndGo,
    SpinAndGold,
    MysteryBattleRoyale,
    RushAndCash,
    AllInOrFold,
    Omaholic,
    HighRoller,
    Micro,
    Freeroll,
    Ticket,
    Promo,
    Other
}

public enum TournamentStatus
{
    Finished,
    Registered,
    Active
}

public enum CashSessionStatus
{
    Finished,
    Active
}

public enum AppearanceMode
{
    System,
    Dark,
    Light
}

public enum DecisionLabel
{
    PlayOk,
    Review,
    ShotOk,
    ShotOnly,
    Pass,
    TakeBreak,
    FundFirst
}
