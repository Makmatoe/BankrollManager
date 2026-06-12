using System.Globalization;
using System.Text;
using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.Core.Persistence;

public static class ChatGptBankrollExporter
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static void ExportToFile(BankrollData data, string destinationPath, DateTime generatedAtLocal)
    {
        File.WriteAllText(destinationPath, BuildMarkdown(data, generatedAtLocal), Encoding.UTF8);
    }

    public static string BuildMarkdown(BankrollData data, DateTime generatedAtLocal)
    {
        data.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);

        var today = DateOnly.FromDateTime(generatedAtLocal);
        var dashboard = BankrollCalculator.GetDashboardSummary(data, today);
        var platformSummaries = BankrollCalculator.GetPlatformSummaries(data)
            .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var dailySummaries = BankrollCalculator.GetDailySummaries(data)
            .OrderBy(summary => summary.Date)
            .ToList();
        var monthlySummaries = BankrollCalculator.GetMonthlySummaries(data)
            .OrderBy(summary => summary.Month)
            .ToList();
        var timeline = BankrollCalculator.GetAuditTimeline(data)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Time ?? TimeOnly.MinValue)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine("# Bankroll Manager ChatGPT Export");
        builder.AppendLine();
        builder.AppendLine($"Generated local time: {generatedAtLocal:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Currency symbol for all money columns: {data.Settings.CurrencySymbol}");
        builder.AppendLine($"Activity date range: {ActivityDateRange(data)}");
        builder.AppendLine();
        builder.AppendLine("## How To Read This");
        builder.AppendLine();
        builder.AppendLine("- Money values are plain decimal numbers in the currency above.");
        builder.AppendLine("- cash_pl affects the cash bankroll; value_pl includes ticket balance changes.");
        builder.AppendLine("- Active cash sessions show committed table exposure but no finished profit or loss yet.");
        builder.AppendLine("- Tournament rows can have separate registration and finish date/time fields.");
        builder.AppendLine("- The event timeline is the clearest chronological view for reasoning about bankroll flow.");
        builder.AppendLine();

        AppendTable(
            builder,
            "## Current Snapshot",
            ["metric", "value"],
            [
                ["current_cash_bankroll", Money(dashboard.CurrentBankroll)],
                ["current_overall_value", Money(dashboard.CurrentBankrollValue)],
                ["active_table_cash", Money(dashboard.ActiveTableCash)],
                ["ticket_balance", Money(dashboard.TicketBalance)],
                ["total_deposits", Money(dashboard.TotalDeposits)],
                ["total_withdrawals", Money(dashboard.TotalWithdrawals)],
                ["tournament_cash_pl", Money(dashboard.TournamentProfitLoss)],
                ["cash_session_pl", Money(dashboard.CashProfitLoss)],
                ["total_poker_cash_pl", Money(dashboard.TotalPokerProfitLoss)],
                ["total_value_pl", Money(dashboard.TotalValueProfitLoss)],
                ["today_cash_pl", Money(dashboard.TodayProfitLoss)],
                ["today_value_pl", Money(dashboard.TodayValueProfitLoss)],
                ["active_month_cash_pl", Money(dashboard.ThisMonthProfitLoss)],
                ["active_month_value_pl", Money(dashboard.ThisMonthValueProfitLoss)],
                ["bankroll_tier", dashboard.BankrollTier]
            ]);

        AppendTable(
            builder,
            "## Stop Loss And Locks",
            ["field", "value"],
            [
                ["status", dashboard.StopLossStatus.StatusText],
                ["break_required", Bool(dashboard.StopLossStatus.BreakRequired)],
                ["daily_stop_loss_hit", Bool(dashboard.StopLossStatus.DailyStopLossHit)],
                ["monthly_stop_loss_hit", Bool(dashboard.StopLossStatus.MonthlyStopLossHit)],
                ["protect_mode_active", Bool(dashboard.StopLossStatus.ProtectModeActive)],
                ["session_locked", Bool(dashboard.StopLossStatus.SessionLocked)],
                ["today_cash_pl", Money(dashboard.StopLossStatus.TodayProfitLoss)],
                ["this_month_cash_pl", Money(dashboard.StopLossStatus.ThisMonthProfitLoss)],
                ["daily_stop_loss_limit", Money(dashboard.StopLossStatus.DailyStopLossLimit)],
                ["monthly_stop_loss_limit", Money(dashboard.StopLossStatus.MonthlyStopLossLimit)],
                ["explanation", dashboard.StopLossStatus.Explanation]
            ]);

        AppendTable(
            builder,
            "## Settings",
            ["field", "value"],
            [
                ["starting_bankroll", Money(data.Settings.StartingBankroll)],
                ["default_platform", data.Settings.DefaultPlatform.ToString()],
                ["enabled_platforms", string.Join(", ", data.Settings.GetEnabledPlatforms())],
                ["active_month_start", Date(data.Settings.ActiveMonthStart)],
                ["default_max_bullets", Int(data.Settings.DefaultMaxBullets)],
                ["active_review_year", Int(data.Settings.ActiveReviewYear)],
                ["normal_mtt_max_risk_percent", Percent(data.Settings.NormalMttMaxRiskPercent)],
                ["sng_hexa_pro_max_risk_percent", Percent(data.Settings.SngHexaProMaxRiskPercent)],
                ["flip_max_risk_percent", Percent(data.Settings.FlipMaxRiskPercent)],
                ["shot_tower_max_risk_percent", Percent(data.Settings.ShotTowerMaxRiskPercent)],
                ["cash_session_max_risk_percent", Percent(data.Settings.CashSessionMaxRiskPercent)],
                ["daily_risk_cap_percent", Percent(data.Settings.DailyRiskCapPercent)],
                ["active_exposure_cap_percent", Percent(data.Settings.ActiveExposureCapPercent)],
                ["daily_stop_loss_amount", Money(data.Settings.DailyStopLossAmount)],
                ["monthly_poker_stop_loss_percent", Percent(data.Settings.MonthlyPokerStopLossPercent)],
                ["protect_mode_below_bankroll", Money(data.Settings.ProtectModeBelowBankroll)],
                ["green_light_shot_bankroll", Money(data.Settings.GreenLightShotBankroll)],
                ["move_up_review_bankroll", Money(data.Settings.MoveUpReviewBankroll)]
            ]);

        AppendTable(
            builder,
            "## Category Rules",
            [
                "category", "max_risk_percent", "monthly_budget_percent", "default_buy_in_cap",
                "min_bankroll", "bullet_cap", "daily_entry_cap", "cooldown_days", "usage_note"
            ],
            data.Settings.CategoryRules
                .OrderBy(rule => rule.Category.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(rule => new[]
                {
                    rule.Category.ToString(),
                    Percent(rule.MaxRiskPercent),
                    Percent(rule.MonthlyBudgetPercent),
                    Money(rule.DefaultBuyInCap),
                    Money(rule.MinBankroll),
                    Int(rule.BulletCap),
                    Int(rule.DailyEntryCap),
                    Int(rule.CooldownDays),
                    rule.UsageNote
                }));

        AppendTable(
            builder,
            "## Platform Summary",
            [
                "platform", "wallet_cash", "on_tables", "total_exposure", "total_value",
                "deposits", "withdrawals", "ledger_net", "tournament_cash_pl", "cash_session_pl",
                "total_cash_pl", "ticket_balance", "cash_cost", "entries", "actual_cash", "difference", "accepted_difference", "updated", "notes"
            ],
            platformSummaries.Select(summary => new[]
            {
                summary.Name,
                Money(summary.WalletCashBalance),
                Money(summary.ActiveTableCash),
                Money(summary.TotalPlatformExposure),
                Money(summary.TotalPlatformValue),
                Money(summary.Deposits),
                Money(summary.Withdrawals),
                Money(summary.LedgerNet),
                Money(summary.TournamentProfitLoss),
                Money(summary.CashSessionProfitLoss),
                Money(summary.TotalPokerProfitLoss),
                Money(summary.TicketBalance),
                Money(summary.CashCost),
                Int(summary.Count),
                NullableMoney(summary.ActualCashBalance),
                NullableMoney(summary.Difference),
                NullableMoney(summary.AcceptedCashDifference),
                NullableDate(summary.LastUpdatedDate),
                summary.Notes
            }));

        AppendTable(
            builder,
            "## Open Exposure",
            ["kind", "date", "time", "platform", "name", "risk_or_exposure", "status", "rule"],
            OpenExposureRows(data));

        AppendTable(
            builder,
            "## Daily Summary",
            [
                "date", "tournament_cash_pl", "cash_session_pl", "ticket_pl", "total_cash_pl",
                "total_value_pl", "sessions", "hours", "cash_per_hour", "value_per_hour",
                "running_month_cash_pl", "running_cash_bankroll", "running_overall_value"
            ],
            dailySummaries.Select(summary => new[]
            {
                Date(summary.Date),
                Money(summary.TournamentProfitLoss),
                Money(summary.CashProfitLoss),
                Money(summary.TicketProfitLoss),
                Money(summary.TotalProfitLoss),
                Money(summary.TotalValueProfitLoss),
                Int(summary.NumberOfSessions),
                Hours(summary.HoursPlayed),
                Money(summary.CashPerHour),
                Money(summary.ValuePerHour),
                Money(summary.RunningMonthProfitLoss),
                Money(summary.RunningLifetimeBankroll),
                Money(summary.RunningLifetimeBankrollValue)
            }));

        AppendTable(
            builder,
            "## Monthly Summary",
            [
                "month", "deposits", "withdrawals", "tournament_cash_pl", "cash_session_pl",
                "ticket_pl", "total_cash_pl", "total_value_pl", "tournaments", "cash_sessions",
                "hours", "cash_per_hour", "value_per_hour", "average_buy_in",
                "biggest_win", "biggest_loss", "stop_loss_breaches"
            ],
            monthlySummaries.Select(summary => new[]
            {
                summary.Month.ToString("yyyy-MM", Culture),
                Money(summary.Deposits),
                Money(summary.Withdrawals),
                Money(summary.TournamentProfitLoss),
                Money(summary.CashProfitLoss),
                Money(summary.TicketProfitLoss),
                Money(summary.TotalPokerProfitLoss),
                Money(summary.TotalValueProfitLoss),
                Int(summary.NumberOfTournaments),
                Int(summary.NumberOfCashSessions),
                Hours(summary.HoursPlayed),
                Money(summary.CashPerHour),
                Money(summary.ValuePerHour),
                Money(summary.AverageTournamentBuyIn),
                Money(summary.BiggestWin),
                Money(summary.BiggestLoss),
                Int(summary.StopLossBreaches)
            }));

        AppendTable(
            builder,
            "## Event Timeline",
            ["date", "time", "type", "name", "cost_or_risk", "cash_result", "cash_bankroll_before", "cash_bankroll_after", "rule"],
            timeline.Select(entry => new[]
            {
                Date(entry.Date),
                NullableTime(entry.Time),
                entry.Type,
                entry.Name,
                Money(entry.CostRisk),
                Money(entry.Result),
                Money(entry.BankrollBefore),
                Money(entry.BankrollAfter),
                entry.Rule
            }));

        AppendTable(
            builder,
            "## Tournament Entries",
            [
                "date", "time", "finished_date", "finished_time", "status", "platform", "category",
                "format", "event", "buy_in", "actual_bullets", "add_ons_rebuys", "fee_rake",
                "cash_cost", "cash_return", "net_cash_pl", "ticket_buy_in", "ticket_won",
                "ticket_balance_impact", "total_value_pl", "value_roi", "placement", "field_size",
                "risk_percent", "cash_bankroll_before", "cash_bankroll_after", "rule", "details", "notes"
            ],
            data.TournamentEntries
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.RegistrationTime ?? TimeOnly.MinValue)
                .Select(entry => new[]
                {
                    Date(entry.Date),
                    NullableTime(entry.RegistrationTime),
                    NullableDate(entry.FinishedDate),
                    NullableTime(entry.FinishedTime),
                    entry.Status.ToString(),
                    entry.Platform.ToString(),
                    entry.Category.ToString(),
                    entry.Format.ToString(),
                    entry.EventName,
                    Money(entry.BuyIn),
                    Int(entry.ActualBullets),
                    Money(entry.AddOnsRebuys),
                    Money(entry.FeeRake),
                    Money(entry.CashCost),
                    Money(entry.ReturnAmount),
                    Money(entry.NetProfit),
                    Money(entry.TicketBuyInValue),
                    Money(entry.EffectiveTicketValueWon),
                    Money(entry.TicketBalanceImpact),
                    Money(entry.TotalValueProfitLoss),
                    Ratio(entry.ValueROI),
                    NullableInt(entry.Placement),
                    NullableInt(entry.FieldSize),
                    Percent(entry.RiskPercentageOfBankrollAtRegistration),
                    Money(entry.BankrollBefore),
                    Money(entry.BankrollAfter),
                    entry.RuleCheckResult,
                    TournamentDetails(entry),
                    JoinedNotes(entry.Tags, entry.PreGameFocus, entry.MistakeLesson, entry.Notes)
                }));

        AppendTable(
            builder,
            "## Cash Sessions",
            [
                "date", "time", "closed_date", "closed_time", "status", "platform", "format", "game",
                "stakes", "small_blind", "big_blind", "start_buy_in", "reloads", "reload_cap",
                "active_table_cash", "cashout", "extras_won", "session_cost", "net_cash_pl",
                "minutes", "hands", "bb_won", "bb_per_100", "risk_percent",
                "cash_bankroll_before", "cash_bankroll_after", "rule", "notes"
            ],
            data.CashSessions
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.SessionTime ?? TimeOnly.MinValue)
                .Select(entry => new[]
                {
                    Date(entry.Date),
                    NullableTime(entry.SessionTime),
                    NullableDate(entry.ClosedDate),
                    NullableTime(entry.ClosedTime),
                    entry.Status.ToString(),
                    entry.Platform.ToString(),
                    entry.Format.ToString(),
                    entry.Game,
                    entry.Stakes,
                    Money(entry.SmallBlindAmount),
                    Money(entry.BigBlindAmount),
                    Money(entry.StartStackBuyIn),
                    Money(entry.Reloads),
                    Money(entry.ReloadCap),
                    Money(entry.ActiveTableCash),
                    Money(entry.Cashout),
                    Money(entry.CashDropWon + entry.JackpotFortunePrizeWon),
                    Money(entry.SessionCost),
                    Money(entry.NetProfit),
                    NullableInt(entry.Minutes),
                    NullableInt(entry.Hands),
                    Ratio(entry.BBWon),
                    Ratio(entry.BBPer100),
                    Percent(entry.RiskPercentageOfBankrollAtSessionStart),
                    Money(entry.BankrollBefore),
                    Money(entry.BankrollAfter),
                    entry.RuleCheckResult,
                    entry.Notes
                }));

        AppendTable(
            builder,
            "## Ledger Entries",
            ["date", "type", "platform", "description", "amount", "signed_amount", "category", "cash_bankroll_before", "cash_bankroll_after", "notes"],
            data.LedgerEntries
                .OrderBy(entry => entry.Date)
                .Select(entry => new[]
                {
                    Date(entry.Date),
                    entry.Type.ToString(),
                    entry.Platform.ToString(),
                    entry.Description,
                    Money(entry.Amount),
                    Money(BankrollCalculator.SignedLedgerAmount(entry)),
                    entry.Category.ToString(),
                    Money(entry.BankrollBefore),
                    Money(entry.BankrollAfter),
                    entry.Notes
                }));

        return builder.ToString();
    }

    private static IEnumerable<string[]> OpenExposureRows(BankrollData data)
    {
        foreach (var entry in data.TournamentEntries.Where(entry => entry.Status != TournamentStatus.Finished)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.RegistrationTime ?? TimeOnly.MinValue))
        {
            yield return
            [
                "tournament",
                Date(entry.Date),
                NullableTime(entry.RegistrationTime),
                entry.Platform.ToString(),
                entry.EventName,
                Money(entry.CashCost),
                entry.Status.ToString(),
                entry.RuleCheckResult
            ];
        }

        foreach (var entry in data.CashSessions.Where(entry => entry.IsActive)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.SessionTime ?? TimeOnly.MinValue))
        {
            yield return
            [
                "cash",
                Date(entry.Date),
                NullableTime(entry.SessionTime),
                entry.Platform.ToString(),
                $"{entry.Game} {entry.Stakes}".Trim(),
                Money(entry.ActiveTableCash),
                entry.Status.ToString(),
                entry.RuleCheckResult
            ];
        }
    }

    private static void AppendTable(StringBuilder builder, string title, string[] headers, IEnumerable<string[]> rows)
    {
        builder.AppendLine(title);
        builder.AppendLine();
        builder.AppendLine(string.Join(" | ", headers.Select(EscapeCell)));
        builder.AppendLine(string.Join(" | ", headers.Select(_ => "---")));

        var wroteRow = false;
        foreach (var row in rows)
        {
            wroteRow = true;
            builder.AppendLine(string.Join(" | ", row.Select(EscapeCell)));
        }

        if (!wroteRow)
        {
            builder.AppendLine(string.Join(" | ", headers.Select((_, index) => index == 0 ? "_none_" : string.Empty)));
        }

        builder.AppendLine();
    }

    private static string ActivityDateRange(BankrollData data)
    {
        var dates = data.LedgerEntries.Select(entry => entry.Date)
            .Concat(data.TournamentEntries.Select(entry => entry.Date))
            .Concat(data.TournamentEntries.Select(entry => entry.FinishedDate).OfType<DateOnly>())
            .Concat(data.CashSessions.Select(entry => entry.Date))
            .Concat(data.CashSessions.Select(entry => entry.ClosedDate).OfType<DateOnly>())
            .ToList();

        return dates.Count == 0
            ? "no activity"
            : $"{Date(dates.Min())} to {Date(dates.Max())}";
    }

    private static string TournamentDetails(TournamentEntry entry)
    {
        var details = new List<string>();
        AddMoneyDetail(details, "bounty_ticket_value", entry.BountyTicketValue);
        AddMoneyDetail(details, "bounty_prize", entry.BountyPrize);
        AddMoneyDetail(details, "regular_cash_prize", entry.RegularCashPrize);
        AddMoneyDetail(details, "mystery_bounty_prize", entry.MysteryBountyPrize);
        AddMoneyDetail(details, "tournament_dollars_won", entry.TournamentDollarsWon);
        AddMoneyDetail(details, "cash_dollars_won", entry.CashDollarsWon);
        AddMoneyDetail(details, "insurance_cost", entry.InsuranceCost);
        AddMoneyDetail(details, "prize_won", entry.PrizeWon);
        AddMoneyDetail(details, "flip_buy_in_per_stack", entry.FlipBuyInPerStack);
        AddIntDetail(details, "flip_stacks_bought", entry.FlipStacksBought);
        AddBoolDetail(details, "ticket_won", entry.TicketWon);
        AddBoolDetail(details, "qualified", entry.Qualified);
        AddBoolDetail(details, "ticket_converted_realized", entry.TicketConvertedRealized);
        AddStringDetail(details, "ticket_buy_in_platform", entry.TicketBuyInPlatform?.ToString() ?? string.Empty);
        AddStringDetail(details, "target_event_name", entry.TargetEventName);
        AddMoneyDetail(details, "target_event_buy_in", entry.TargetEventBuyIn);
        AddStringDetail(details, "target_package_event", entry.TargetPackageEvent);
        AddStringDetail(details, "event_tag", entry.EventTag == EventTag.None ? string.Empty : entry.EventTag.ToString());
        AddBoolDetail(details, "promo_freebie_ticket_event", entry.IsPromoFreebieTicketEvent);
        AddBoolDetail(details, "itm", entry.ITM);
        AddBoolDetail(details, "final_table", entry.FinalTable);
        return string.Join("; ", details);
    }

    private static void AddMoneyDetail(List<string> details, string key, decimal value)
    {
        if (value != 0m)
        {
            details.Add($"{key}={Money(value)}");
        }
    }

    private static void AddIntDetail(List<string> details, string key, int value)
    {
        if (value != 0)
        {
            details.Add($"{key}={Int(value)}");
        }
    }

    private static void AddBoolDetail(List<string> details, string key, bool value)
    {
        if (value)
        {
            details.Add($"{key}=true");
        }
    }

    private static void AddStringDetail(List<string> details, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            details.Add($"{key}={value}");
        }
    }

    private static string JoinedNotes(params string[] values)
    {
        return string.Join(" / ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string EscapeCell(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Trim();
    }

    private static string Date(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", Culture);
    }

    private static string NullableDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", Culture) ?? string.Empty;
    }

    private static string NullableTime(TimeOnly? value)
    {
        return value?.ToString("HH:mm", Culture) ?? string.Empty;
    }

    private static string Money(decimal value)
    {
        return value.ToString("0.00", Culture);
    }

    private static string NullableMoney(decimal? value)
    {
        return value.HasValue ? Money(value.Value) : string.Empty;
    }

    private static string Percent(decimal value)
    {
        return value.ToString("0.##", Culture);
    }

    private static string Ratio(decimal value)
    {
        return value.ToString("0.####", Culture);
    }

    private static string Hours(decimal value)
    {
        return value.ToString("0.##", Culture);
    }

    private static string Int(int value)
    {
        return value.ToString(Culture);
    }

    private static string NullableInt(int? value)
    {
        return value?.ToString(Culture) ?? string.Empty;
    }

    private static string Bool(bool value)
    {
        return value ? "true" : "false";
    }
}
