using System.Text.Json.Serialization;

namespace BankrollManager.Core.Models;

public sealed class CashSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly? SessionTime { get; set; }
    public CashSessionStatus Status { get; set; } = CashSessionStatus.Finished;
    public DateOnly? ClosedDate { get; set; }
    public TimeOnly? ClosedTime { get; set; }
    public Platform Platform { get; set; } = Platform.Unibet;
    public CashFormat Format { get; set; } = CashFormat.HoldemCash;
    public string Game { get; set; } = "Cash";
    public string Stakes { get; set; } = string.Empty;
    public decimal SmallBlindAmount { get; set; }
    public decimal BigBlindAmount { get; set; }
    public decimal StartStackBuyIn { get; set; }
    public decimal Reloads { get; set; }
    public decimal ReloadCap { get; set; }
    public decimal Cashout { get; set; }
    public decimal CashDropWon { get; set; }
    public decimal JackpotFortunePrizeWon { get; set; }
    public int? Minutes { get; set; }
    public int? Hands { get; set; }
    public decimal RiskPercentageOfBankrollAtSessionStart { get; set; }
    public string RuleCheckResult { get; set; } = string.Empty;
    public decimal BankrollBefore { get; set; }
    public decimal BankrollAfter { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal SessionCost => StartStackBuyIn + Reloads;

    [JsonIgnore]
    public bool IsActive => Status == CashSessionStatus.Active;

    [JsonIgnore]
    public decimal ActiveTableCash => IsActive ? SessionCost : 0m;

    [JsonIgnore]
    public decimal WalletCashImpact => IsActive ? -SessionCost : NetProfit;

    [JsonIgnore]
    public decimal NetProfit => IsActive ? 0m : Cashout + CashDropWon + JackpotFortunePrizeWon - SessionCost;

    [JsonIgnore]
    public bool IsRushAndCash => Format is CashFormat.RushAndCashHoldem or CashFormat.RushAndCashOmaha;

    [JsonIgnore]
    public bool IsAllInOrFold => Format is CashFormat.AllInOrFoldHoldem or CashFormat.AllInOrFoldOmaha;

    [JsonIgnore]
    public decimal BBWon => BigBlindAmount > 0m ? NetProfit / BigBlindAmount : 0m;

    [JsonIgnore]
    public decimal BBPer100 => Hands is > 0 ? BBWon / Hands.Value * 100m : 0m;
}
