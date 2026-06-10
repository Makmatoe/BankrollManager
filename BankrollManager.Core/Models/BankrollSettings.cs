namespace BankrollManager.Core.Models;

public sealed class BankrollSettings
{
    private const int CurrentRuleProfileVersion = 3;

    public decimal StartingBankroll { get; set; } = 0m;
    public string CurrencySymbol { get; set; } = "\u20ac";
    public Platform DefaultPlatform { get; set; } = Platform.Unibet;
    public DateOnly ActiveMonthStart { get; set; } = new(2026, 6, 1);
    public int DefaultMaxBullets { get; set; } = 1;
    public int ActiveReviewYear { get; set; } = 2026;
    public decimal NormalMttMaxRiskPercent { get; set; } = 2.5m;
    public decimal SngHexaProMaxRiskPercent { get; set; } = 2.5m;
    public decimal FlipMaxRiskPercent { get; set; } = 1m;
    public decimal ShotTowerMaxRiskPercent { get; set; } = 10m;
    public decimal CashSessionMaxRiskPercent { get; set; } = 6m;
    public decimal DailyStopLossAmount { get; set; } = 5m;
    public decimal MonthlyPokerStopLossPercent { get; set; } = 20m;
    public decimal ReserveTargetPercent { get; set; } = 6m;
    public decimal ProtectModeBelowBankroll { get; set; } = 40m;
    public decimal MoveUpReviewBankroll { get; set; } = 100m;
    public decimal GreenLightShotBankroll { get; set; } = 75m;
    public decimal WithdrawalProfitLockThreshold { get; set; } = 20m;
    public decimal ReviewRiskCapUsagePercent { get; set; } = 80m;
    public decimal BudgetWarningPercent { get; set; } = 20m;
    public decimal DailyRiskCapPercent { get; set; } = 12m;
    public decimal ActiveExposureCapPercent { get; set; } = 18m;
    public decimal StopLossWarningPercent { get; set; } = 70m;
    public decimal CashReloadWarningPercent { get; set; } = 100m;
    public bool SessionLockedForToday { get; set; }
    public DateOnly? SessionLockedDate { get; set; }
    public DateOnly? CooldownUntilDate { get; set; }
    public AppearanceMode AppearanceMode { get; set; } = AppearanceMode.Dark;
    public bool TutorialCompleted { get; set; }
    public int TutorialStepIndex { get; set; }
    public List<string> TutorialCompletedTasks { get; set; } = [];
    public int RuleProfileVersion { get; set; }
    public List<CategoryRuleSettings> CategoryRules { get; set; } = CategoryRuleSettings.CreateDefaults();

    public CategoryRuleSettings GetRule(TournamentCategory category)
    {
        EnsureDefaults();
        return CategoryRules.FirstOrDefault(rule => rule.Category == category)
            ?? CategoryRules.First(rule => rule.Category == TournamentCategory.Other);
    }

    public void EnsureDefaults()
    {
        var previousRuleProfileVersion = RuleProfileVersion;
        var upgradeGuideThresholds = previousRuleProfileVersion < 3;
        var upgradeCategoryRules = previousRuleProfileVersion < 2;
        if (!Enum.IsDefined(AppearanceMode))
        {
            AppearanceMode = AppearanceMode.Dark;
        }
        TutorialCompletedTasks ??= [];
        TutorialStepIndex = Math.Max(0, TutorialStepIndex);

        if (upgradeGuideThresholds)
        {
            ApplyMissingGuideThresholdDefaults();
        }

        if (ReviewRiskCapUsagePercent <= 0m)
        {
            ReviewRiskCapUsagePercent = 80m;
        }
        ReviewRiskCapUsagePercent = ClampPercent(ReviewRiskCapUsagePercent);

        if (BudgetWarningPercent < 0m)
        {
            BudgetWarningPercent = 0m;
        }
        BudgetWarningPercent = ClampPercent(BudgetWarningPercent);

        if (DailyRiskCapPercent < 0m)
        {
            DailyRiskCapPercent = 0m;
        }
        DailyRiskCapPercent = ClampPercent(DailyRiskCapPercent);

        if (ActiveExposureCapPercent < 0m)
        {
            ActiveExposureCapPercent = 0m;
        }
        ActiveExposureCapPercent = ClampPercent(ActiveExposureCapPercent);

        if (StopLossWarningPercent < 0m)
        {
            StopLossWarningPercent = 0m;
        }
        StopLossWarningPercent = ClampPercent(StopLossWarningPercent);

        if (CashReloadWarningPercent < 0m)
        {
            CashReloadWarningPercent = 0m;
        }
        CashReloadWarningPercent = ClampPercent(CashReloadWarningPercent);

        CategoryRules ??= CategoryRuleSettings.CreateDefaults();
        if (CategoryRules.Count == 0)
        {
            CategoryRules = CategoryRuleSettings.CreateDefaults();
        }

        var defaultRules = CategoryRuleSettings.CreateDefaults();
        foreach (var defaultRule in defaultRules)
        {
            if (CategoryRules.All(rule => rule.Category != defaultRule.Category))
            {
                CategoryRules.Add(defaultRule);
            }
        }

        if (upgradeCategoryRules)
        {
            foreach (var rule in CategoryRules)
            {
                if (defaultRules.FirstOrDefault(defaultRule => defaultRule.Category == rule.Category) is not { } defaultRule)
                {
                    continue;
                }

                rule.MinBankroll = defaultRule.MinBankroll;
                rule.DailyEntryCap = defaultRule.DailyEntryCap;
                rule.CooldownDays = defaultRule.CooldownDays;
            }
        }

        if (previousRuleProfileVersion < CurrentRuleProfileVersion)
        {
            RuleProfileVersion = CurrentRuleProfileVersion;
        }

        foreach (var rule in CategoryRules)
        {
            rule.MaxRiskPercent = ClampPercent(rule.MaxRiskPercent);
            rule.MonthlyBudgetPercent = ClampPercent(rule.MonthlyBudgetPercent);
            rule.DefaultBuyInCap = Math.Max(0m, rule.DefaultBuyInCap);
            rule.MinBankroll = Math.Max(0m, rule.MinBankroll);
            rule.BulletCap = Math.Max(0, rule.BulletCap);
            rule.DailyEntryCap = Math.Max(0, rule.DailyEntryCap);
            rule.CooldownDays = Math.Max(0, rule.CooldownDays);
        }
    }

    public bool IsSessionLocked(DateOnly today)
    {
        return SessionLockedDate == today
            || (SessionLockedForToday && SessionLockedDate is null);
    }

    public bool IsCooldownActive(DateOnly today)
    {
        return CooldownUntilDate is { } cooldownUntil && cooldownUntil >= today;
    }

    public bool IsPlayLocked(DateOnly today)
    {
        return IsSessionLocked(today) || IsCooldownActive(today);
    }

    public void LockSessionFor(DateOnly date)
    {
        SessionLockedForToday = true;
        SessionLockedDate = date;
    }

    public void ClearSessionLock()
    {
        SessionLockedForToday = false;
        SessionLockedDate = null;
    }

    public void SetCooldownUntil(DateOnly date)
    {
        CooldownUntilDate = date;
    }

    public void ClearCooldown()
    {
        CooldownUntilDate = null;
    }

    public void ClearPlayLocks()
    {
        ClearSessionLock();
        ClearCooldown();
    }

    public bool NormalizePlayLocks(DateOnly today)
    {
        var changed = false;
        if (SessionLockedForToday && SessionLockedDate is null)
        {
            SessionLockedDate = today;
            changed = true;
        }

        if (SessionLockedDate is { } lockedDate && lockedDate != today)
        {
            ClearSessionLock();
            changed = true;
        }

        if (SessionLockedDate == today && !SessionLockedForToday)
        {
            SessionLockedForToday = true;
            changed = true;
        }

        if (CooldownUntilDate is { } cooldownUntil && cooldownUntil < today)
        {
            ClearCooldown();
            changed = true;
        }

        return changed;
    }

    private void ApplyMissingGuideThresholdDefaults()
    {
        if (ReviewRiskCapUsagePercent <= 0m)
        {
            ReviewRiskCapUsagePercent = 80m;
        }

        if (BudgetWarningPercent <= 0m)
        {
            BudgetWarningPercent = 20m;
        }

        if (DailyRiskCapPercent <= 0m)
        {
            DailyRiskCapPercent = 12m;
        }

        if (ActiveExposureCapPercent <= 0m)
        {
            ActiveExposureCapPercent = 18m;
        }

        if (StopLossWarningPercent <= 0m)
        {
            StopLossWarningPercent = 70m;
        }

        if (CashReloadWarningPercent <= 0m)
        {
            CashReloadWarningPercent = 100m;
        }
    }

    private static decimal ClampPercent(decimal value)
    {
        return Math.Clamp(value, 0m, 100m);
    }
}
