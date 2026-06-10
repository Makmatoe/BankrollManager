using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class NeedsAttentionService
{
    public static List<AttentionItem> GetItems(
        BankrollData data,
        DateOnly today,
        IReadOnlyCollection<PlatformSummary>? platformSummaries = null,
        AttentionOptions? options = null)
    {
        data.EnsureDefaults();
        options ??= new AttentionOptions();
        platformSummaries ??= BankrollCalculator.GetPlatformSummaries(data);

        var items = new List<AttentionItem>();
        AddRuleItems(items, data, today);
        AddActiveCashItems(items, data, today);
        AddWalletItems(items, platformSummaries, data.Settings, options);
        AddOpenTournamentItems(items, data);

        if (items.Count == 0)
        {
            items.Add(new AttentionItem(
                99,
                AttentionSeverity.Clear,
                "Overview",
                "No active sessions, wallet mismatches, or pending entries need action.",
                "None",
                AttentionTargetType.None,
                null,
                string.Empty));
        }

        return items
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.Area)
            .ThenBy(item => item.Summary)
            .Take(Math.Max(1, options.MaxItems))
            .ToList();
    }

    private static void AddRuleItems(List<AttentionItem> items, BankrollData data, DateOnly today)
    {
        var stopLoss = StopLossService.GetStatus(data, today);
        if (stopLoss.BreakRequired)
        {
            items.Add(new AttentionItem(
                0,
                AttentionSeverity.High,
                "Rules",
                stopLoss.Explanation,
                "Take break",
                AttentionTargetType.Settings,
                null,
                string.Empty));
        }
        else if (stopLoss.ProtectModeActive)
        {
            items.Add(new AttentionItem(
                8,
                AttentionSeverity.Info,
                "Rules",
                stopLoss.Explanation,
                "Review",
                AttentionTargetType.Settings,
                null,
                string.Empty));
        }

        if (data.Settings.IsSessionLocked(today))
        {
            items.Add(new AttentionItem(
                1,
                AttentionSeverity.High,
                "Rules",
                "Session lock is active for today.",
                "Keep locked",
                AttentionTargetType.Settings,
                null,
                string.Empty));
        }

        if (data.Settings.CooldownUntilDate is { } cooldownUntil && data.Settings.IsCooldownActive(today))
        {
            items.Add(new AttentionItem(
                1,
                AttentionSeverity.High,
                "Rules",
                $"Cooldown active until {cooldownUntil:yyyy-MM-dd}.",
                "Keep locked",
                AttentionTargetType.Settings,
                null,
                string.Empty));
        }
    }

    private static void AddActiveCashItems(List<AttentionItem> items, BankrollData data, DateOnly today)
    {
        foreach (var session in data.CashSessions
            .Where(entry => entry.IsActive)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.SessionTime ?? TimeOnly.MinValue))
        {
            var ageDays = Math.Max(0, today.DayNumber - session.Date.DayNumber);
            var ageText = ageDays == 0 ? "open now" : $"open {ageDays}d";
            items.Add(new AttentionItem(
                ageDays > 0 ? 1 : 2,
                ageDays > 0 ? AttentionSeverity.High : AttentionSeverity.Check,
                "Cash",
                $"{session.Platform} {CashSessionLabel(session)} {ageText}; {Money(session.ActiveTableCash, data.Settings)} on table.",
                "Close cash",
                AttentionTargetType.Cash,
                session.Id,
                string.Empty));
        }
    }

    private static void AddWalletItems(
        List<AttentionItem> items,
        IReadOnlyCollection<PlatformSummary> platformSummaries,
        BankrollSettings settings,
        AttentionOptions options)
    {
        foreach (var summary in platformSummaries)
        {
            if (summary.ActualCashBalance.HasValue
                && summary.Difference is { } difference
                && Math.Abs(difference) >= options.WalletDifferenceThreshold)
            {
                var high = Math.Abs(difference) >= options.HighWalletDifferenceThreshold;
                items.Add(new AttentionItem(
                    high ? 2 : 4,
                    high ? AttentionSeverity.High : AttentionSeverity.Check,
                    "Wallet",
                    $"{summary.Name}: actual {Money(summary.ActualCashBalance.Value, settings)} vs expected {Money(summary.WalletCashBalance, settings)}.",
                    "Reconcile",
                    AttentionTargetType.Wallet,
                    null,
                    summary.Name));
            }
            else if (!summary.ActualCashBalance.HasValue && summary.Count > 0)
            {
                items.Add(new AttentionItem(
                    6,
                    AttentionSeverity.Check,
                    "Wallet",
                    $"{summary.Name}: wallet has not been reconciled yet.",
                    "Reconcile",
                    AttentionTargetType.Wallet,
                    null,
                    summary.Name));
            }
        }
    }

    private static void AddOpenTournamentItems(List<AttentionItem> items, BankrollData data)
    {
        foreach (var tournament in data.TournamentEntries
            .Where(entry => entry.Status != TournamentStatus.Finished)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.RegistrationTime ?? TimeOnly.MinValue))
        {
            items.Add(new AttentionItem(
                tournament.Status == TournamentStatus.Active ? 3 : 5,
                tournament.Status == TournamentStatus.Active ? AttentionSeverity.Check : AttentionSeverity.Info,
                "MTT",
                $"{tournament.Status}: {TournamentLabel(tournament)} for {Money(tournament.CashCost, data.Settings)}.",
                tournament.Status == TournamentStatus.Active ? "Finish" : "Review",
                AttentionTargetType.Tournament,
                tournament.Id,
                string.Empty));
        }
    }

    private static string CashSessionLabel(CashSession session)
    {
        var label = $"{session.Game} {session.Stakes}".Trim();
        return string.IsNullOrWhiteSpace(label) ? "Cash" : label;
    }

    private static string TournamentLabel(TournamentEntry tournament)
    {
        return string.IsNullOrWhiteSpace(tournament.EventName)
            ? tournament.Format.ToString()
            : tournament.EventName;
    }

    private static string Money(decimal value, BankrollSettings? settings = null)
    {
        var currency = settings?.CurrencySymbol ?? string.Empty;
        var sign = value < 0m ? "-" : string.Empty;
        return $"{sign}{currency}{Math.Abs(value):0.00}";
    }
}
