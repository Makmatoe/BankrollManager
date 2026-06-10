using System.Text.Json.Serialization;

namespace BankrollManager.Core.Models;

public sealed class TournamentEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly? RegistrationTime { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.Finished;
    public DateOnly? FinishedDate { get; set; }
    public TimeOnly? FinishedTime { get; set; }
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
    public int? Placement { get; set; }
    public int? FieldSize { get; set; }
    public bool ITM { get; set; }
    public bool FinalTable { get; set; }
    public decimal RiskPercentageOfBankrollAtRegistration { get; set; }
    public string RuleCheckResult { get; set; } = string.Empty;
    public decimal BankrollBefore { get; set; }
    public decimal BankrollAfter { get; set; }
    public string PreGameFocus { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string MistakeLesson { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal TotalCost => Format switch
    {
        TournamentFormat.SpinAndGold => BuyIn + InsuranceCost,
        TournamentFormat.FlipAndGo => FlipCost,
        _ => BuyIn * ActualBullets + AddOnsRebuys + FeeRake
    };

    [JsonIgnore]
    public decimal CashCost => Math.Max(0m, TotalCost - TicketBuyInValue);

    [JsonIgnore]
    public decimal ReturnAmount => Status == TournamentStatus.Finished ? CashReturnAmount : 0m;

    [JsonIgnore]
    public decimal TicketReturnAmount => Status == TournamentStatus.Finished && !TicketConvertedRealized ? EffectiveTicketValueWon : 0m;

    [JsonIgnore]
    public decimal TicketBalanceImpact => TicketReturnAmount - TicketBuyInValue;

    [JsonIgnore]
    public Platform EffectiveTicketBuyInPlatform => TicketBuyInPlatform ?? Platform;

    [JsonIgnore]
    public decimal NetProfit => ReturnAmount - CashCost;

    [JsonIgnore]
    public decimal CashProfitLoss => NetProfit;

    [JsonIgnore]
    public decimal TotalValueProfitLoss => NetProfit + TicketBalanceImpact;

    [JsonIgnore]
    public decimal ROI => CashCost > 0m ? NetProfit / CashCost : 0m;

    [JsonIgnore]
    public decimal ValueROI => TotalCost > 0m ? TotalValueProfitLoss / TotalCost : 0m;

    [JsonIgnore]
    public decimal FlipCost => FlipBuyInPerStack > 0m
        ? FlipBuyInPerStack * Math.Max(1, FlipStacksBought)
        : BuyIn * Math.Max(1, ActualBullets);

    [JsonIgnore]
    public decimal EffectiveTicketValueWon => TicketWon && TicketValueWon <= 0m ? TargetEventBuyIn : TicketValueWon;

    [JsonIgnore]
    private decimal CashReturnAmount => Format switch
    {
        TournamentFormat.PKO => CashPrize + BountyPrize,
        TournamentFormat.MysteryBounty => RegularCashPrize + MysteryBountyPrize + CashPrize + BountyPrize,
        TournamentFormat.SpinAndGold => PrizeWon,
        TournamentFormat.FlipAndGo => PrizeWon,
        TournamentFormat.MysteryBattleRoyale => PrizeWon + MysteryBountyPrize,
        TournamentFormat.Satellite or TournamentFormat.TurboSatellite or TournamentFormat.TargetStackSatellite or TournamentFormat.FlashSatellite or TournamentFormat.WSOPExpress
            => TicketConvertedRealized ? EffectiveTicketValueWon : 0m,
        _ => CashPrize + BountyTicketValue + BountyPrize + MysteryBountyPrize + PrizeWon + RegularCashPrize
    } + TournamentDollarsWon + CashDollarsWon;
}
