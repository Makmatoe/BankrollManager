namespace BankrollManager.Core.Models;

public sealed class TournamentPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Platform Platform { get; set; } = Platform.Unibet;
    public TournamentCategory Category { get; set; } = TournamentCategory.MainGrind;
    public TournamentFormat Format { get; set; } = TournamentFormat.MTT;
    public string EventName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public EventTag EventTag { get; set; } = EventTag.None;
    public bool IsPromoFreebieTicketEvent { get; set; }
    public decimal BuyIn { get; set; }
    public decimal FeeRake { get; set; }
    public int PlannedBullets { get; set; } = 1;
    public int ActualBullets { get; set; } = 1;
    public decimal AddOnsRebuys { get; set; }
    public decimal BountyTicketValue { get; set; }
    public decimal TicketBuyInValue { get; set; }
    public Platform? TicketBuyInPlatform { get; set; }
    public decimal TicketValueWon { get; set; }
    public decimal CashPrize { get; set; }
    public decimal TournamentDollarsWon { get; set; }
    public decimal CashDollarsWon { get; set; }
    public decimal RegularCashPrize { get; set; }
    public decimal MysteryBountyPrize { get; set; }
    public bool BountyPhaseReached { get; set; }
    public int? KnockoutsAfterBountyPhase { get; set; }
    public string MysteryBountyNotes { get; set; } = string.Empty;
    public decimal BountyPrize { get; set; }
    public int? Knockouts { get; set; }
    public int? SpinPlayerCount { get; set; }
    public bool InsuranceUsed { get; set; }
    public decimal InsuranceCost { get; set; }
    public decimal MultiplierHit { get; set; }
    public decimal PrizeWon { get; set; }
    public decimal FlipBuyInPerStack { get; set; }
    public int FlipStacksBought { get; set; }
    public bool FlipPhaseWon { get; set; }
    public bool GoPhaseReached { get; set; }
    public bool RushStageSurvived { get; set; }
    public bool BattleRoyaleFinalTableReached { get; set; }
    public string TargetEventName { get; set; } = string.Empty;
    public decimal TargetEventBuyIn { get; set; }
    public bool TicketWon { get; set; }
    public bool Qualified { get; set; }
    public bool TicketConvertedRealized { get; set; }
    public int? WsopExpressStepNumber { get; set; }
    public decimal TicketUsedValue { get; set; }
    public string TargetPackageEvent { get; set; } = string.Empty;
    public int? FieldSize { get; set; }
    public string PreGameFocus { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedUtc { get; set; }
}
