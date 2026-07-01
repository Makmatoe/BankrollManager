using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class StopLossService
{
    public static StopLossStatus GetStatus(BankrollData data, DateOnly today)
    {
        return GetStatus(data, today, BankrollCalculator.GetDailySummaries(data));
    }

    public static StopLossStatus GetStatus(BankrollData data, DateOnly today, IReadOnlyList<DailySummary> dailySummaries)
    {
        data.EnsureDefaults();
        var settings = data.Settings;
        settings.NormalizePlayLocks(today);
        var monthStart = BankrollSettings.MonthStartFor(today);
        var todayProfitLoss = dailySummaries.FirstOrDefault(summary => summary.Date == today)?.TotalProfitLoss ?? 0m;
        var thisMonthProfitLoss = dailySummaries
            .Where(summary => summary.Date >= monthStart && summary.Date <= today)
            .Sum(summary => summary.TotalProfitLoss);

        var dailyLimit = settings.DailyStopLossAmount;
        var monthFunding = BankrollCalculator.MonthFunding(data, monthStart, today);
        var monthlyLimit = monthFunding * settings.MonthlyPokerStopLossPercent / 100m;
        var currentBankroll = BankrollCalculator.CurrentBankroll(data);
        var dailyStopLossHit = dailyLimit > 0m && todayProfitLoss <= -dailyLimit;
        var monthlyStopLossHit = monthlyLimit > 0m && thisMonthProfitLoss <= -monthlyLimit;
        var protectModeActive = currentBankroll > 0m && currentBankroll < settings.ProtectModeBelowBankroll;
        var sessionLocked = settings.IsPlayLocked(today);
        var breakRequired = dailyStopLossHit || monthlyStopLossHit || protectModeActive || sessionLocked;

        var reasons = new List<string>();
        if (dailyStopLossHit)
        {
            reasons.Add($"daily stop-loss hit ({todayProfitLoss:0.00})");
        }

        if (monthlyStopLossHit)
        {
            reasons.Add($"monthly stop-loss hit ({thisMonthProfitLoss:0.00})");
        }

        if (protectModeActive)
        {
            reasons.Add($"cash bankroll below protect mode threshold ({currentBankroll:0.00})");
        }

        if (settings.IsSessionLocked(today))
        {
            reasons.Add("session locked for today");
        }

        if (settings.CooldownUntilDate is { } cooldownUntil && settings.IsCooldownActive(today))
        {
            reasons.Add($"cooldown active until {cooldownUntil:yyyy-MM-dd}");
        }

        return new StopLossStatus(
            dailyStopLossHit,
            monthlyStopLossHit,
            protectModeActive,
            sessionLocked,
            breakRequired,
            todayProfitLoss,
            thisMonthProfitLoss,
            dailyLimit,
            monthlyLimit,
            breakRequired ? "TAKE BREAK" : "OK",
            reasons.Count == 0 ? "No stop-loss rule is currently active." : string.Join("; ", reasons));
    }
}
