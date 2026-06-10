namespace BankrollManager.Core.Models;

public sealed class CategoryRuleSettings
{
    public TournamentCategory Category { get; set; }
    public decimal MaxRiskPercent { get; set; }
    public decimal MonthlyBudgetPercent { get; set; }
    public decimal DefaultBuyInCap { get; set; }
    public decimal MinBankroll { get; set; }
    public int BulletCap { get; set; }
    public int DailyEntryCap { get; set; }
    public int CooldownDays { get; set; }
    public string UsageNote { get; set; } = string.Empty;

    public static List<CategoryRuleSettings> CreateDefaults()
    {
        return
        [
            new()
            {
                Category = TournamentCategory.MainGrind,
                MaxRiskPercent = 2.5m,
                MonthlyBudgetPercent = 58m,
                DefaultBuyInCap = 2m,
                MinBankroll = 0m,
                BulletCap = 2,
                DailyEntryCap = 12,
                CooldownDays = 0,
                UsageNote = "Core EUR0.25-EUR2 MTT/SNG volume."
            },
            new()
            {
                Category = TournamentCategory.TowerShot,
                MaxRiskPercent = 10m,
                MonthlyBudgetPercent = 10m,
                DefaultBuyInCap = 5m,
                MinBankroll = 75m,
                BulletCap = 1,
                DailyEntryCap = 1,
                CooldownDays = 3,
                UsageNote = "One planned shot; no automatic re-entry."
            },
            new()
            {
                Category = TournamentCategory.FlipSatellite,
                MaxRiskPercent = 1m,
                MonthlyBudgetPercent = 7m,
                DefaultBuyInCap = 0.50m,
                MinBankroll = 0m,
                BulletCap = 1,
                DailyEntryCap = 6,
                CooldownDays = 0,
                UsageNote = "Free tokens, centrolls, tiny flips/satellites."
            },
            new()
            {
                Category = TournamentCategory.HexaProSng,
                MaxRiskPercent = 2.5m,
                MonthlyBudgetPercent = 10m,
                DefaultBuyInCap = 2m,
                MinBankroll = 0m,
                BulletCap = 1,
                DailyEntryCap = 8,
                CooldownDays = 0,
                UsageNote = "Fast formats; capped volume."
            },
            new()
            {
                Category = TournamentCategory.CashPractice,
                MaxRiskPercent = 6m,
                MonthlyBudgetPercent = 9m,
                DefaultBuyInCap = 3m,
                MinBankroll = 40m,
                BulletCap = 1,
                DailyEntryCap = 2,
                CooldownDays = 0,
                UsageNote = "NL1/Banzai or short capped sessions."
            },
            new()
            {
                Category = TournamentCategory.Reserve,
                MaxRiskPercent = 0m,
                MonthlyBudgetPercent = 6m,
                DefaultBuyInCap = 0m,
                MinBankroll = 0m,
                BulletCap = 0,
                DailyEntryCap = 0,
                CooldownDays = 0,
                UsageNote = "Protected bankroll; do not spend."
            },
            new()
            {
                Category = TournamentCategory.Other,
                MaxRiskPercent = 2.5m,
                MonthlyBudgetPercent = 0m,
                DefaultBuyInCap = 0m,
                MinBankroll = 0m,
                BulletCap = 1,
                DailyEntryCap = 1,
                CooldownDays = 0,
                UsageNote = "Unclassified play; review before using."
            }
        ];
    }
}
