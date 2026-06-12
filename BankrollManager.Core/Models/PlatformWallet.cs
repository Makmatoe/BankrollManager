namespace BankrollManager.Core.Models;

public sealed class PlatformWallet
{
    public Platform Platform { get; set; } = Platform.Unibet;
    public decimal? ActualCashBalance { get; set; }
    public decimal? AcceptedCashDifference { get; set; }
    public DateOnly? LastUpdatedDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
