using BankrollManager.Core.Models;

namespace BankrollManager.App;

internal enum DetailTableDateRange
{
    AllTime,
    CurrentMonth,
    Last30Days,
    Custom
}

internal enum DetailTableKind
{
    Tournaments,
    Cash,
    Ledger,
    Timeline
}

internal sealed record DetailTableFilterCriteria(
    DetailTableDateRange DateRange,
    DateOnly Today,
    DateOnly? CustomFrom,
    DateOnly? CustomTo,
    string SearchText,
    bool OpenOnly,
    bool FinishedOnly,
    bool FlipsOnly,
    bool TicketRelatedOnly,
    bool HighRiskOnly,
    bool ProfitableOnly,
    bool LosingOnly)
{
    public static DetailTableFilterCriteria Default(DateOnly today)
    {
        return new DetailTableFilterCriteria(
            DetailTableDateRange.AllTime,
            today,
            null,
            null,
            string.Empty,
            OpenOnly: false,
            FinishedOnly: false,
            FlipsOnly: false,
            TicketRelatedOnly: false,
            HighRiskOnly: false,
            ProfitableOnly: false,
            LosingOnly: false);
    }
}

internal static class DetailTableFilter
{
    private const decimal HighRiskPercent = 6m;

    public static IEnumerable<TournamentEntry> Apply(IEnumerable<TournamentEntry> rows, DetailTableFilterCriteria criteria)
    {
        return rows.Where(entry =>
            MatchesDate(entry.Date, criteria)
            && MatchesSearch(criteria.SearchText,
                entry.EventName,
                entry.Platform.ToString(),
                entry.Category.ToString(),
                entry.Format.ToString(),
                entry.EventTag.ToString(),
                entry.Currency,
                entry.RuleCheckResult,
                entry.PreGameFocus,
                entry.Tags,
                entry.MistakeLesson,
                entry.Notes)
            && (!criteria.OpenOnly || entry.Status != TournamentStatus.Finished)
            && (!criteria.FinishedOnly || entry.Status == TournamentStatus.Finished)
            && (!criteria.FlipsOnly || IsFlipTournament(entry))
            && (!criteria.TicketRelatedOnly || IsTicketRelated(entry))
            && (!criteria.HighRiskOnly || entry.RiskPercentageOfBankrollAtRegistration >= HighRiskPercent || IsRiskRule(entry.RuleCheckResult))
            && (!criteria.ProfitableOnly || entry.TotalValueProfitLoss > 0m)
            && (!criteria.LosingOnly || entry.TotalValueProfitLoss < 0m));
    }

    public static IEnumerable<CashSession> Apply(IEnumerable<CashSession> rows, DetailTableFilterCriteria criteria)
    {
        return rows.Where(entry =>
            MatchesDate(entry.Date, criteria)
            && MatchesSearch(criteria.SearchText,
                entry.Platform.ToString(),
                entry.Format.ToString(),
                entry.Status.ToString(),
                entry.Game,
                entry.Stakes,
                entry.RuleCheckResult,
                entry.Notes)
            && (!criteria.OpenOnly || entry.Status != CashSessionStatus.Finished)
            && (!criteria.FinishedOnly || entry.Status == CashSessionStatus.Finished)
            && (!criteria.HighRiskOnly || entry.RiskPercentageOfBankrollAtSessionStart >= HighRiskPercent || IsRiskRule(entry.RuleCheckResult))
            && (!criteria.ProfitableOnly || entry.NetProfit > 0m)
            && (!criteria.LosingOnly || entry.NetProfit < 0m));
    }

    public static IEnumerable<LedgerEntry> Apply(IEnumerable<LedgerEntry> rows, DetailTableFilterCriteria criteria)
    {
        return rows.Where(entry =>
            MatchesDate(entry.Date, criteria)
            && MatchesSearch(criteria.SearchText,
                entry.Type.ToString(),
                entry.Platform.ToString(),
                entry.Description,
                entry.Category.ToString(),
                entry.Notes)
            && (!criteria.FlipsOnly || entry.Category == TournamentCategory.FlipSatellite || ContainsAny("flip", entry.Description, entry.Notes))
            && (!criteria.TicketRelatedOnly || entry.Type == LedgerType.TicketCredit || ContainsAny("ticket", entry.Description, entry.Notes))
            && (!criteria.ProfitableOnly || entry.Amount > 0m)
            && (!criteria.LosingOnly || entry.Amount < 0m));
    }

    public static IEnumerable<AuditTimelineEntry> Apply(IEnumerable<AuditTimelineEntry> rows, DetailTableFilterCriteria criteria)
    {
        return rows.Where(entry =>
            MatchesDate(entry.Date, criteria)
            && MatchesSearch(criteria.SearchText, entry.Type, entry.Name, entry.Rule)
            && (!criteria.FlipsOnly || ContainsAny("flip", entry.Type, entry.Name, entry.Rule))
            && (!criteria.TicketRelatedOnly || ContainsAny("ticket", entry.Type, entry.Name, entry.Rule))
            && (!criteria.HighRiskOnly || IsRiskRule(entry.Rule))
            && (!criteria.ProfitableOnly || entry.Result > 0m)
            && (!criteria.LosingOnly || entry.Result < 0m));
    }

    internal static bool MatchesDate(DateOnly date, DetailTableFilterCriteria criteria)
    {
        var (start, end) = ResolveDateRange(criteria);
        return (start is null || date >= start.Value)
            && (end is null || date <= end.Value);
    }

    private static (DateOnly? Start, DateOnly? End) ResolveDateRange(DetailTableFilterCriteria criteria)
    {
        return criteria.DateRange switch
        {
            DetailTableDateRange.CurrentMonth => (new DateOnly(criteria.Today.Year, criteria.Today.Month, 1), criteria.Today),
            DetailTableDateRange.Last30Days => (criteria.Today.AddDays(-29), criteria.Today),
            DetailTableDateRange.Custom => NormalizeCustomRange(criteria.CustomFrom, criteria.CustomTo),
            _ => (null, null)
        };
    }

    private static (DateOnly? Start, DateOnly? End) NormalizeCustomRange(DateOnly? start, DateOnly? end)
    {
        if (start is not { } from || end is not { } to)
        {
            return (start, end);
        }

        return from <= to ? (from, to) : (to, from);
    }

    private static bool MatchesSearch(string searchText, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return values.Any(value => value?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private static bool IsFlipTournament(TournamentEntry entry)
    {
        return entry.Category == TournamentCategory.FlipSatellite
            || entry.Format is TournamentFormat.Flip or TournamentFormat.FlipAndGo
            || entry.EventTag == EventTag.FlipAndGo;
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
            || !string.IsNullOrWhiteSpace(entry.TargetEventName)
            || !string.IsNullOrWhiteSpace(entry.TargetPackageEvent)
            || entry.Format is TournamentFormat.Satellite
                or TournamentFormat.TurboSatellite
                or TournamentFormat.TargetStackSatellite
                or TournamentFormat.FlashSatellite
                or TournamentFormat.WSOPExpress;
    }

    private static bool IsRiskRule(string? rule)
    {
        return ContainsAny("risk", rule)
            || ContainsAny("review", rule)
            || ContainsAny("pass", rule)
            || ContainsAny("shot", rule)
            || ContainsAny("cap", rule)
            || ContainsAny("breach", rule);
    }

    private static bool ContainsAny(string needle, params string?[] values)
    {
        return values.Any(value => value?.Contains(needle, StringComparison.CurrentCultureIgnoreCase) == true);
    }
}
