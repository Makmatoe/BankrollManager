using System.Globalization;
using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class MonthlyReviewService
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static IReadOnlyList<DateOnly> GetAvailableMonths(BankrollData data)
    {
        data.EnsureDefaults();
        return data.LedgerEntries.Select(entry => MonthOf(entry.Date))
            .Concat(data.TournamentEntries.Select(entry => MonthOf(entry.Date)))
            .Concat(data.TournamentEntries.Select(entry => entry.FinishedDate).OfType<DateOnly>().Select(MonthOf))
            .Concat(data.CashSessions.Select(entry => MonthOf(entry.Date)))
            .Concat(data.CashSessions.Select(entry => entry.ClosedDate).OfType<DateOnly>().Select(MonthOf))
            .Distinct()
            .OrderByDescending(month => month)
            .ToList();
    }

    public static MonthlyReviewReport GetReport(BankrollData data, DateOnly month)
    {
        data.EnsureDefaults();
        data.Settings.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);

        month = MonthOf(month);
        var dailySummaries = BankrollCalculator.GetDailySummaries(data);
        var summary = BankrollCalculator.GetMonthlySummaries(data, dailySummaries)
            .FirstOrDefault(summary => summary.Month == month)
            ?? EmptySummary(month);
        var contributions = BuildContributions(data, month).ToList();
        var dailyInMonth = dailySummaries
            .Where(summary => MonthOf(summary.Date) == month)
            .OrderBy(summary => summary.Date)
            .ToList();
        var riskBreaches = BuildRiskBreaches(data, month).ToList();

        return new MonthlyReviewReport(
            month,
            summary,
            BuildMetrics(summary, riskBreaches.Count),
            BuildGroupResults(contributions, contribution => contribution.Format),
            BuildGroupResults(contributions, contribution => contribution.Category),
            BuildGroupResults(contributions, contribution => contribution.Platform),
            BuildSpecialtyResults(contributions),
            BuildSwings(contributions, wins: true),
            BuildSwings(contributions, wins: false),
            BuildStopLossBreaches(dailyInMonth, data.Settings),
            riskBreaches,
            BuildNotes(data, month));
    }

    private static IReadOnlyList<MonthlyReviewMetric> BuildMetrics(MonthlySummary summary, int riskBreachCount)
    {
        return
        [
            new("Cash P/L", Money(summary.TotalPokerProfitLoss), "Tournament cash P/L plus cash-session P/L."),
            new("Value P/L", Money(summary.TotalValueProfitLoss), "Cash P/L plus ticket balance changes."),
            new("Ticket P/L", Money(summary.TicketProfitLoss), "Ticket value won minus ticket buy-ins."),
            new("Tournament cash P/L", Money(summary.TournamentProfitLoss), "Cash-only tournament result for the month."),
            new("Cash session P/L", Money(summary.CashProfitLoss), "Finished cash-session result for the month."),
            new("Hours played", Hours(summary.HoursPlayed), "Tournament and cash hours attributed to the activity date."),
            new("Cash / hour", Money(summary.CashPerHour), "Cash P/L divided by hours played."),
            new("Value / hour", Money(summary.ValuePerHour), "Value P/L divided by hours played."),
            new("Biggest win", Money(summary.BiggestWin), "Largest single value result in the month."),
            new("Biggest loss", Money(summary.BiggestLoss), "Smallest single value result in the month."),
            new("Stop-loss breaches", summary.StopLossBreaches.ToString(Culture), "Daily cash P/L at or below the daily stop-loss."),
            new("Risk breaches", riskBreachCount.ToString(Culture), "Entries above cap or marked review/pass by the rule engine.")
        ];
    }

    private static IEnumerable<MonthlyContribution> BuildContributions(BankrollData data, DateOnly month)
    {
        foreach (var entry in data.TournamentEntries)
        {
            var split = UsesSplitTournamentSettlement(entry);
            if (MonthOf(entry.Date) == month)
            {
                yield return TournamentContribution(
                    entry,
                    entry.Date,
                    entry.RegistrationTime,
                    "Tournament",
                    split ? -entry.CashCost : entry.NetProfit,
                    split ? -entry.TicketBuyInValue : entry.TicketBalanceImpact,
                    entry.TotalCost,
                    TournamentHours(entry));
            }

            if (HasTournamentSettlement(entry) && MonthOf(TournamentSettlementDate(entry)) == month)
            {
                yield return TournamentContribution(
                    entry,
                    TournamentSettlementDate(entry),
                    TournamentSettlementTime(entry),
                    "Tournament Result",
                    entry.ReturnAmount,
                    TournamentTicketSettlementAmount(entry),
                    0m,
                    0m);
            }
        }

        foreach (var entry in data.CashSessions.Where(entry => MonthOf(entry.Date) == month))
        {
            yield return new MonthlyContribution(
                entry.Id,
                "Cash",
                entry.Date,
                entry.SessionTime,
                CashSessionName(entry),
                entry.Platform.ToString(),
                entry.Format.ToString(),
                TournamentCategory.CashPractice.ToString(),
                IsAllInOrFold(entry.Format) ? "All-in-or-fold cash" : "Cash",
                0m,
                entry.NetProfit,
                0m,
                entry.SessionCost,
                CashSessionHours(entry),
                entry.RiskPercentageOfBankrollAtSessionStart,
                entry.RuleCheckResult,
                entry.Notes);
        }
    }

    private static MonthlyContribution TournamentContribution(
        TournamentEntry entry,
        DateOnly date,
        TimeOnly? time,
        string kind,
        decimal cashProfitLoss,
        decimal ticketProfitLoss,
        decimal totalCost,
        decimal hours)
    {
        return new MonthlyContribution(
            entry.Id,
            kind,
            date,
            time,
            TournamentName(entry),
            entry.Platform.ToString(),
            entry.Format.ToString(),
            entry.Category.ToString(),
            SpecialtyName(entry),
            cashProfitLoss,
            0m,
            ticketProfitLoss,
            totalCost,
            hours,
            entry.RiskPercentageOfBankrollAtRegistration,
            entry.RuleCheckResult,
            JoinedNotes(entry.Tags, entry.PreGameFocus, entry.MistakeLesson, entry.Notes));
    }

    private static IReadOnlyList<MonthlyReviewGroupResult> BuildGroupResults(
        IEnumerable<MonthlyContribution> contributions,
        Func<MonthlyContribution, string> keySelector)
    {
        return contributions
            .GroupBy(keySelector)
            .Select(group => ToGroupResult(group.Key, group))
            .Where(result => result.Count > 0 || result.TotalValueProfitLoss != 0m || result.TotalCost != 0m)
            .OrderByDescending(result => Math.Abs(result.TotalValueProfitLoss))
            .ThenBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<MonthlyReviewGroupResult> BuildSpecialtyResults(
        IReadOnlyList<MonthlyContribution> contributions)
    {
        var groups = new (string Name, Func<MonthlyContribution, bool> Predicate)[]
        {
            ("Flip / Flip & Go", contribution => HasSpecialty(contribution, "Flip / Flip & Go")),
            ("Satellite", contribution => HasSpecialty(contribution, "Satellite")),
            ("Ticket-related", contribution => HasSpecialty(contribution, "Ticket-related"))
        };

        return groups
            .Select(group => ToGroupResult(group.Name, contributions.Where(group.Predicate)))
            .Where(result => result.Count > 0 || result.TotalValueProfitLoss != 0m || result.TotalCost != 0m)
            .OrderByDescending(result => Math.Abs(result.TotalValueProfitLoss))
            .ThenBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static MonthlyReviewGroupResult ToGroupResult(
        string name,
        IEnumerable<MonthlyContribution> contributions)
    {
        var items = contributions.ToList();
        return new MonthlyReviewGroupResult(
            name,
            items.Sum(item => item.TournamentProfitLoss),
            items.Sum(item => item.CashProfitLoss),
            items.Sum(item => item.TicketProfitLoss),
            items.Sum(item => item.TournamentProfitLoss + item.CashProfitLoss),
            items.Sum(item => item.TournamentProfitLoss + item.CashProfitLoss + item.TicketProfitLoss),
            items.Sum(item => item.TotalCost),
            items.Select(item => item.SourceId).Distinct().Count(),
            items.Sum(item => item.HoursPlayed));
    }

    private static IReadOnlyList<MonthlyReviewEntry> BuildSwings(
        IReadOnlyList<MonthlyContribution> contributions,
        bool wins)
    {
        var entries = contributions
            .GroupBy(contribution => contribution.SourceId)
            .Select(group => ToEntry(group.OrderBy(item => item.Date).ThenBy(item => item.Time ?? TimeOnly.MinValue)))
            .Where(entry => wins ? entry.ValueProfitLoss > 0m : entry.ValueProfitLoss < 0m)
            .OrderBy(entry => wins ? -entry.ValueProfitLoss : entry.ValueProfitLoss)
            .ThenBy(entry => entry.Date)
            .Take(10)
            .ToList();
        return entries;
    }

    private static IReadOnlyList<MonthlyReviewEntry> BuildStopLossBreaches(
        IReadOnlyList<DailySummary> dailySummaries,
        BankrollSettings settings)
    {
        if (settings.DailyStopLossAmount <= 0m)
        {
            return [];
        }

        return dailySummaries
            .Where(summary => summary.TotalProfitLoss <= -settings.DailyStopLossAmount)
            .Select(summary => new MonthlyReviewEntry(
                summary.Date,
                null,
                "Stop-loss",
                "Daily stop-loss breach",
                string.Empty,
                string.Empty,
                string.Empty,
                summary.TotalProfitLoss,
                summary.TicketProfitLoss,
                summary.TotalValueProfitLoss,
                0m,
                "TAKE BREAK",
                $"Daily stop-loss limit {Money(settings.DailyStopLossAmount)}."))
            .ToList();
    }

    private static IEnumerable<MonthlyReviewEntry> BuildRiskBreaches(BankrollData data, DateOnly month)
    {
        foreach (var entry in data.TournamentEntries.Where(entry => MonthOf(entry.Date) == month))
        {
            var cap = EffectiveRiskCap(data.Settings, entry.Category, entry.Format, null);
            if (!IsRiskBreach(entry.RiskPercentageOfBankrollAtRegistration, cap, entry.RuleCheckResult))
            {
                continue;
            }

            yield return new MonthlyReviewEntry(
                entry.Date,
                entry.RegistrationTime,
                "Tournament Risk",
                TournamentName(entry),
                entry.Platform.ToString(),
                entry.Format.ToString(),
                entry.Category.ToString(),
                entry.NetProfit,
                entry.TicketBalanceImpact,
                entry.TotalValueProfitLoss,
                entry.RiskPercentageOfBankrollAtRegistration,
                entry.RuleCheckResult,
                cap > 0m ? $"Configured cap {Percent(cap)}." : "No configured cap.");
        }

        foreach (var entry in data.CashSessions.Where(entry => MonthOf(entry.Date) == month))
        {
            var cap = EffectiveRiskCap(data.Settings, TournamentCategory.CashPractice, TournamentFormat.Other, entry.Format);
            if (!IsRiskBreach(entry.RiskPercentageOfBankrollAtSessionStart, cap, entry.RuleCheckResult))
            {
                continue;
            }

            yield return new MonthlyReviewEntry(
                entry.Date,
                entry.SessionTime,
                "Cash Risk",
                CashSessionName(entry),
                entry.Platform.ToString(),
                entry.Format.ToString(),
                TournamentCategory.CashPractice.ToString(),
                entry.NetProfit,
                0m,
                entry.NetProfit,
                entry.RiskPercentageOfBankrollAtSessionStart,
                entry.RuleCheckResult,
                cap > 0m ? $"Configured cap {Percent(cap)}." : "No configured cap.");
        }
    }

    private static IReadOnlyList<MonthlyReviewNote> BuildNotes(BankrollData data, DateOnly month)
    {
        var notes = new List<MonthlyReviewNote>();
        foreach (var entry in data.TournamentEntries.Where(entry =>
            MonthOf(entry.Date) == month || entry.FinishedDate is { } finishedDate && MonthOf(finishedDate) == month))
        {
            var name = TournamentName(entry);
            AddNote(notes, entry.Date, "Tournament", name, "Focus", entry.PreGameFocus);
            AddNote(notes, entry.Date, "Tournament", name, "Tags", entry.Tags);
            AddNote(notes, entry.Date, "Tournament", name, "Leak/Lesson", entry.MistakeLesson);
            AddNote(notes, entry.Date, "Tournament", name, ClassifyNoteArea(entry.Notes), entry.Notes);
        }

        foreach (var entry in data.CashSessions.Where(entry =>
            MonthOf(entry.Date) == month || entry.ClosedDate is { } closedDate && MonthOf(closedDate) == month))
        {
            AddNote(notes, entry.Date, "Cash", CashSessionName(entry), ClassifyNoteArea(entry.Notes), entry.Notes);
        }

        foreach (var entry in data.LedgerEntries.Where(entry => MonthOf(entry.Date) == month))
        {
            AddNote(notes, entry.Date, "Ledger", entry.Description, ClassifyNoteArea(entry.Notes), entry.Notes);
        }

        return notes
            .OrderBy(note => note.Date)
            .ThenBy(note => note.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(note => note.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddNote(
        List<MonthlyReviewNote> notes,
        DateOnly date,
        string kind,
        string name,
        string area,
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        notes.Add(new MonthlyReviewNote(date, kind, string.IsNullOrWhiteSpace(name) ? kind : name, area, text.Trim()));
    }

    private static MonthlyReviewEntry ToEntry(IEnumerable<MonthlyContribution> contributions)
    {
        var items = contributions.ToList();
        var first = items[0];
        return new MonthlyReviewEntry(
            first.Date,
            first.Time,
            first.Kind,
            first.Name,
            first.Platform,
            first.Format,
            first.Category,
            items.Sum(item => item.TournamentProfitLoss + item.CashProfitLoss),
            items.Sum(item => item.TicketProfitLoss),
            items.Sum(item => item.TournamentProfitLoss + item.CashProfitLoss + item.TicketProfitLoss),
            items.Max(item => item.RiskPercentage),
            first.Rule,
            first.Notes);
    }

    private static bool IsRiskBreach(decimal riskPercentage, decimal cap, string rule)
    {
        return cap > 0m && riskPercentage > cap
            || RuleIndicatesRiskBreach(rule);
    }

    private static bool RuleIndicatesRiskBreach(string rule)
    {
        return !string.IsNullOrWhiteSpace(rule)
            && (rule.Contains("PASS", StringComparison.OrdinalIgnoreCase)
                || rule.Contains("REVIEW", StringComparison.OrdinalIgnoreCase)
                || rule.Contains("TAKE BREAK", StringComparison.OrdinalIgnoreCase)
                || rule.Contains("FUND FIRST", StringComparison.OrdinalIgnoreCase)
                || rule.Contains("SHOT ONLY", StringComparison.OrdinalIgnoreCase));
    }

    private static decimal EffectiveRiskCap(
        BankrollSettings settings,
        TournamentCategory category,
        TournamentFormat format,
        CashFormat? cashFormat)
    {
        var rule = settings.GetRule(category);
        var formatCap = NormalRiskCap(settings, category, format, cashFormat);
        return rule.MaxRiskPercent > 0m && formatCap > 0m
            ? Math.Min(rule.MaxRiskPercent, formatCap)
            : Math.Max(rule.MaxRiskPercent, formatCap);
    }

    private static decimal NormalRiskCap(
        BankrollSettings settings,
        TournamentCategory category,
        TournamentFormat format,
        CashFormat? cashFormat)
    {
        if (cashFormat is not null || category == TournamentCategory.CashPractice)
        {
            return settings.CashSessionMaxRiskPercent;
        }

        if (category == TournamentCategory.FlipSatellite
            || format is TournamentFormat.Flip or TournamentFormat.FlipAndGo)
        {
            return settings.FlipMaxRiskPercent;
        }

        if (category == TournamentCategory.HexaProSng
            || format is TournamentFormat.HexaPro or TournamentFormat.SNG or TournamentFormat.SpinAndGold)
        {
            return settings.SngHexaProMaxRiskPercent;
        }

        if (category == TournamentCategory.TowerShot || format == TournamentFormat.Tower)
        {
            return settings.ShotTowerMaxRiskPercent;
        }

        return settings.NormalMttMaxRiskPercent;
    }

    private static string SpecialtyName(TournamentEntry entry)
    {
        var specialties = new List<string>();
        if (IsFlipTournament(entry))
        {
            specialties.Add("Flip / Flip & Go");
        }

        if (IsSatelliteFormat(entry.Format))
        {
            specialties.Add("Satellite");
        }

        if (IsTicketRelated(entry))
        {
            specialties.Add("Ticket-related");
        }

        return string.Join(";", specialties);
    }

    private static bool HasSpecialty(MonthlyContribution contribution, string specialty)
    {
        return contribution.Specialty.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(specialty, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsTicketRelated(TournamentEntry entry)
    {
        return entry.TicketBuyInValue != 0m
            || entry.TicketValueWon != 0m
            || entry.TicketBalanceImpact != 0m
            || entry.TicketWon
            || entry.Qualified
            || entry.TicketUsedValue != 0m
            || entry.EventTag == EventTag.Ticket
            || entry.IsPromoFreebieTicketEvent;
    }

    private static bool IsSatelliteFormat(TournamentFormat format)
    {
        return format is TournamentFormat.Satellite
            or TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.WSOPExpress;
    }

    private static bool IsFlipTournament(TournamentEntry entry)
    {
        return entry.Category == TournamentCategory.FlipSatellite
            || entry.Format is TournamentFormat.Flip or TournamentFormat.FlipAndGo
            || entry.EventTag == EventTag.FlipAndGo;
    }

    private static bool IsAllInOrFold(CashFormat format)
    {
        return format is CashFormat.AllInOrFoldHoldem or CashFormat.AllInOrFoldOmaha;
    }

    private static bool HasTournamentSettlement(TournamentEntry entry)
    {
        return UsesSplitTournamentSettlement(entry)
            && entry.Status == TournamentStatus.Finished
            && (entry.ReturnAmount != 0m || TournamentTicketSettlementAmount(entry) != 0m);
    }

    private static bool UsesSplitTournamentSettlement(TournamentEntry entry)
    {
        return entry.Status != TournamentStatus.Finished
            || entry.FinishedDate is not null
            || entry.FinishedTime is not null;
    }

    private static decimal TournamentTicketSettlementAmount(TournamentEntry entry)
    {
        return entry.TicketReturnAmount;
    }

    private static DateOnly TournamentSettlementDate(TournamentEntry entry)
    {
        return entry.FinishedDate ?? entry.Date;
    }

    private static TimeOnly? TournamentSettlementTime(TournamentEntry entry)
    {
        return entry.FinishedTime ?? entry.RegistrationTime;
    }

    private static decimal TournamentHours(TournamentEntry entry)
    {
        if (entry.Status != TournamentStatus.Finished)
        {
            return 0m;
        }

        if (IsFlipTournament(entry))
        {
            return 1m / 60m;
        }

        if (entry.RegistrationTime is not { } registeredAt
            || entry.FinishedTime is not { } finishedAt)
        {
            return 0m;
        }

        var finishedDate = entry.FinishedDate ?? entry.Date;
        return HoursBetween(entry.Date, registeredAt, finishedDate, finishedAt);
    }

    private static decimal CashSessionHours(CashSession entry)
    {
        if (entry.Minutes is > 0)
        {
            return entry.Minutes.Value / 60m;
        }

        if (entry.Status != CashSessionStatus.Finished
            || entry.SessionTime is not { } startedAt
            || entry.ClosedDate is not { } closedDate
            || entry.ClosedTime is not { } closedAt)
        {
            return 0m;
        }

        return HoursBetween(entry.Date, startedAt, closedDate, closedAt);
    }

    private static decimal HoursBetween(
        DateOnly startDate,
        TimeOnly startTime,
        DateOnly endDate,
        TimeOnly endTime)
    {
        var started = startDate.ToDateTime(startTime);
        var ended = endDate.ToDateTime(endTime);
        return ended > started
            ? (decimal)(ended - started).TotalHours
            : 0m;
    }

    private static MonthlySummary EmptySummary(DateOnly month)
    {
        return new MonthlySummary(month, 0m, 0m, 0m, 0m, 0m, 0m, 0, 0, 0m, 0m, 0m, 0m, 0, string.Empty);
    }

    private static string TournamentName(TournamentEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.EventName) ? entry.Format.ToString() : entry.EventName;
    }

    private static string CashSessionName(CashSession entry)
    {
        var name = $"{entry.Game} {entry.Stakes}".Trim();
        return string.IsNullOrWhiteSpace(name) ? entry.Format.ToString() : name;
    }

    private static string ClassifyNoteArea(string text)
    {
        if (text.Contains("leak", StringComparison.OrdinalIgnoreCase)
            || text.Contains("mistake", StringComparison.OrdinalIgnoreCase))
        {
            return "Leak";
        }

        if (text.Contains("highlight", StringComparison.OrdinalIgnoreCase)
            || text.Contains("good", StringComparison.OrdinalIgnoreCase))
        {
            return "Highlight";
        }

        return "Notes";
    }

    private static string JoinedNotes(params string[] values)
    {
        return string.Join(" / ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static DateOnly MonthOf(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    private static string Money(decimal value)
    {
        return value.ToString("0.00", Culture);
    }

    private static string Percent(decimal value)
    {
        return value.ToString("0.0", Culture) + "%";
    }

    private static string Hours(decimal value)
    {
        return value.ToString("0.##", Culture);
    }

    private sealed record MonthlyContribution(
        Guid SourceId,
        string Kind,
        DateOnly Date,
        TimeOnly? Time,
        string Name,
        string Platform,
        string Format,
        string Category,
        string Specialty,
        decimal TournamentProfitLoss,
        decimal CashProfitLoss,
        decimal TicketProfitLoss,
        decimal TotalCost,
        decimal HoursPlayed,
        decimal RiskPercentage,
        string Rule,
        string Notes);
}
