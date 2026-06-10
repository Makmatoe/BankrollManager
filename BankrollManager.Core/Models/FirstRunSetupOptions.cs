namespace BankrollManager.Core.Models;

public enum FirstRunFundingMode
{
    StartingBankroll,
    DepositEntry
}

public sealed class FirstRunSetupOptions
{
    public string CurrencySymbol { get; set; } = "\u20ac";
    public List<Platform> EnabledPlatforms { get; set; } = Enum.GetValues<Platform>().ToList();
    public Platform DefaultPlatform { get; set; } = Platform.Unibet;
    public FirstRunFundingMode FundingMode { get; set; } = FirstRunFundingMode.StartingBankroll;
    public decimal FundingAmount { get; set; }
    public Platform DepositPlatform { get; set; } = Platform.Unibet;
    public DateOnly SetupDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Dictionary<Platform, decimal?> PlatformBalances { get; set; } = [];
}
