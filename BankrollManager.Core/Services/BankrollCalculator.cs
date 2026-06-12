using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class BankrollCalculator
{
    public static decimal SignedLedgerAmount(LedgerEntry entry)
    {
        return entry.Type switch
        {
            LedgerType.Withdrawal or LedgerType.TransferOut => -Math.Abs(entry.Amount),
            LedgerType.Deposit or LedgerType.TransferIn => Math.Abs(entry.Amount),
            _ => entry.Amount
        };
    }

    public static decimal TotalDeposits(BankrollData data)
    {
        data.EnsureDefaults();
        return data.LedgerEntries
            .Where(entry => entry.Type == LedgerType.Deposit)
            .Sum(entry => Math.Abs(entry.Amount));
    }

    public static decimal TotalWithdrawals(BankrollData data)
    {
        data.EnsureDefaults();
        return data.LedgerEntries
            .Where(entry => entry.Type == LedgerType.Withdrawal)
            .Sum(entry => Math.Abs(entry.Amount));
    }

    public static decimal LedgerTotal(BankrollData data)
    {
        data.EnsureDefaults();
        return data.LedgerEntries.Sum(SignedLedgerAmount);
    }

    public static decimal TicketBalance(BankrollData data)
    {
        data.EnsureDefaults();
        return data.TournamentEntries.Sum(entry => entry.TicketBalanceImpact);
    }

    public static decimal TicketBalance(BankrollData data, Platform platform)
    {
        data.EnsureDefaults();
        return data.TournamentEntries.Sum(entry =>
            TicketReturnForPlatform(entry, platform) - TicketBuyInForPlatform(entry, platform));
    }

    public static decimal TournamentProfitLoss(BankrollData data)
    {
        data.EnsureDefaults();
        return data.TournamentEntries.Sum(entry => entry.NetProfit);
    }

    public static decimal CashProfitLoss(BankrollData data)
    {
        data.EnsureDefaults();
        return data.CashSessions.Sum(entry => entry.NetProfit);
    }

    public static decimal ActiveTableCash(BankrollData data)
    {
        data.EnsureDefaults();
        return data.CashSessions.Sum(entry => entry.ActiveTableCash);
    }

    public static decimal TotalPokerProfitLoss(BankrollData data)
    {
        return TournamentProfitLoss(data) + CashProfitLoss(data);
    }

    public static decimal TotalValueProfitLoss(BankrollData data)
    {
        return TotalPokerProfitLoss(data) + TicketBalance(data);
    }

    public static decimal CurrentBankroll(BankrollData data)
    {
        data.EnsureDefaults();
        return data.Settings.StartingBankroll + LedgerTotal(data) + TotalPokerProfitLoss(data);
    }

    public static decimal CurrentBankrollValue(BankrollData data)
    {
        return CurrentBankroll(data) + TicketBalance(data);
    }

    public static decimal RiskPercentage(decimal riskAmount, decimal bankroll)
    {
        if (riskAmount <= 0m)
        {
            return 0m;
        }

        return bankroll > 0m ? riskAmount / bankroll * 100m : 100m;
    }

    public static decimal MonthFunding(BankrollData data, DateOnly monthStart, DateOnly throughDate)
    {
        data.EnsureDefaults();
        var positiveLedgerFunding = data.LedgerEntries
            .Where(entry => entry.Date >= monthStart && entry.Date <= throughDate)
            .Select(SignedLedgerAmount)
            .Where(amount => amount > 0m)
            .Sum();

        return Math.Max(0m, data.Settings.StartingBankroll + positiveLedgerFunding);
    }

    public static decimal CategorySpendForMonth(
        BankrollData data,
        TournamentCategory category,
        DateOnly monthStart,
        DateOnly throughDate)
    {
        data.EnsureDefaults();
        var tournamentSpend = data.TournamentEntries
            .Where(entry => entry.Category == category && entry.Date >= monthStart && entry.Date <= throughDate)
            .Sum(entry => entry.CashCost);

        var cashSpend = category == TournamentCategory.CashPractice
            ? data.CashSessions
                .Where(entry => entry.Date >= monthStart && entry.Date <= throughDate)
                .Sum(entry => entry.SessionCost)
            : 0m;

        return tournamentSpend + cashSpend;
    }

    public static decimal CategoryBudgetRemaining(
        BankrollData data,
        TournamentCategory category,
        DateOnly monthStart,
        DateOnly throughDate)
    {
        data.EnsureDefaults();
        var rule = data.Settings.GetRule(category);
        var monthlyFunding = MonthFunding(data, monthStart, throughDate);
        var budget = monthlyFunding * rule.MonthlyBudgetPercent / 100m;
        return budget - CategorySpendForMonth(data, category, monthStart, throughDate);
    }

    public static List<RunningBankrollPoint> GetRunningBankroll(BankrollData data)
    {
        data.EnsureDefaults();

        var events = new List<RunningEvent>();
        events.AddRange(data.LedgerEntries.Select(entry => new RunningEvent(
            entry.Date,
            null,
            0,
            "Ledger",
            $"{entry.Type}: {entry.Description}",
            SignedLedgerAmount(entry),
            0m,
            entry.Id)));
        events.AddRange(data.TournamentEntries.Select(entry => new RunningEvent(
            entry.Date,
            entry.RegistrationTime,
            1,
            "Tournament",
            UsesSplitTournamentSettlement(entry) ? $"{entry.EventName} buy-in" : entry.EventName,
            TournamentRegistrationAmount(entry),
            TournamentTicketRegistrationAmount(entry),
            entry.Id)));
        events.AddRange(data.TournamentEntries
            .Where(HasTournamentSettlement)
            .Select(entry => new RunningEvent(
                TournamentSettlementDate(entry),
                TournamentSettlementTime(entry),
                2,
                "Tournament",
                $"{entry.EventName} result",
                entry.ReturnAmount,
                TournamentTicketSettlementAmount(entry),
                entry.Id)));
        events.AddRange(data.CashSessions.Select(entry => new RunningEvent(
            entry.Date,
            entry.SessionTime,
            3,
            "Cash",
            $"{entry.Game} {entry.Stakes}".Trim(),
            entry.NetProfit,
            0m,
            entry.Id)));

        var runningBankroll = data.Settings.StartingBankroll;
        var runningTicketBalance = 0m;
        var points = new List<RunningBankrollPoint>();

        foreach (var item in events
            .OrderBy(item => item.Date)
            .ThenBy(item => EffectiveTime(item.Time))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id))
        {
            runningBankroll += item.CashAmount;
            runningTicketBalance += item.TicketAmount;
            points.Add(new RunningBankrollPoint(
                item.Date,
                item.Source,
                item.Label,
                item.CashAmount,
                runningBankroll,
                item.TicketAmount,
                runningTicketBalance));
        }

        return points;
    }

    public static List<AuditTimelineEntry> GetAuditTimeline(BankrollData data)
    {
        data.EnsureDefaults();

        var events = new List<AuditEvent>();
        events.AddRange(data.LedgerEntries.Select(entry => new AuditEvent(
            entry.Date,
            null,
            0,
            entry.Id,
            entry.Type.ToString(),
            string.IsNullOrWhiteSpace(entry.Description) ? entry.Category.ToString() : entry.Description,
            0m,
            SignedLedgerAmount(entry),
            string.Empty)));
        events.AddRange(data.TournamentEntries.Select(entry => new AuditEvent(
            entry.Date,
            entry.RegistrationTime,
            1,
            entry.Id,
            UsesSplitTournamentSettlement(entry) ? "Tournament Buy-in" : "Tournament",
            string.IsNullOrWhiteSpace(entry.EventName)
                ? entry.Format.ToString()
                : UsesSplitTournamentSettlement(entry)
                    ? $"{entry.EventName} registration"
                    : entry.EventName,
            entry.CashCost,
            TournamentRegistrationAmount(entry),
            entry.RuleCheckResult)));
        events.AddRange(data.TournamentEntries
            .Where(HasTournamentSettlement)
            .Select(entry => new AuditEvent(
                TournamentSettlementDate(entry),
                TournamentSettlementTime(entry),
                2,
                entry.Id,
                "Tournament Result",
                string.IsNullOrWhiteSpace(entry.EventName) ? entry.Format.ToString() : $"{entry.EventName} finish",
                0m,
                entry.ReturnAmount,
                entry.RuleCheckResult)));
        events.AddRange(data.CashSessions.Select(entry => new AuditEvent(
            entry.Date,
            entry.SessionTime,
            3,
            entry.Id,
            "Cash",
            $"{entry.Game} {entry.Stakes}".Trim(),
            entry.SessionCost,
            entry.NetProfit,
            entry.RuleCheckResult)));

        var runningBankroll = data.Settings.StartingBankroll;
        var timeline = new List<AuditTimelineEntry>();

        foreach (var item in events
            .OrderBy(item => item.Date)
            .ThenBy(item => EffectiveTime(item.Time))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id))
        {
            var bankrollBefore = runningBankroll;
            runningBankroll += item.Result;
            timeline.Add(new AuditTimelineEntry(
                item.Date,
                item.Time,
                item.Type,
                item.Name,
                item.CostRisk,
                item.Result,
                bankrollBefore,
                runningBankroll,
                item.Rule));
        }

        return timeline;
    }

    public static List<DayTimelineEntry> GetDayTimeline(BankrollData data, DateOnly date)
    {
        data.EnsureDefaults();

        var events = new List<DayTimelineEvent>();
        events.AddRange(data.LedgerEntries.Select(entry => new DayTimelineEvent(
            entry.Date,
            null,
            0,
            entry.Id,
            entry.Type.ToString(),
            string.IsNullOrWhiteSpace(entry.Description) ? entry.Category.ToString() : entry.Description,
            0m,
            SignedLedgerAmount(entry),
            0m,
            string.Empty)));
        events.AddRange(data.TournamentEntries.Select(entry => new DayTimelineEvent(
            entry.Date,
            entry.RegistrationTime,
            1,
            entry.Id,
            UsesSplitTournamentSettlement(entry) ? "Tournament Buy-in" : "Tournament",
            string.IsNullOrWhiteSpace(entry.EventName)
                ? entry.Format.ToString()
                : UsesSplitTournamentSettlement(entry)
                    ? $"{entry.EventName} registration"
                    : entry.EventName,
            entry.CashCost,
            TournamentRegistrationAmount(entry),
            TournamentTicketRegistrationAmount(entry),
            entry.RuleCheckResult)));
        events.AddRange(data.TournamentEntries
            .Where(HasTournamentSettlement)
            .Select(entry => new DayTimelineEvent(
                TournamentSettlementDate(entry),
                TournamentSettlementTime(entry),
                2,
                entry.Id,
                "Tournament Result",
                string.IsNullOrWhiteSpace(entry.EventName) ? entry.Format.ToString() : $"{entry.EventName} finish",
                0m,
                entry.ReturnAmount,
                TournamentTicketSettlementAmount(entry),
                entry.RuleCheckResult)));
        events.AddRange(data.CashSessions.Select(entry => new DayTimelineEvent(
            entry.Date,
            entry.SessionTime,
            3,
            entry.Id,
            "Cash",
            CashSessionName(entry),
            entry.SessionCost,
            entry.NetProfit,
            0m,
            entry.RuleCheckResult)));

        var runningBankroll = data.Settings.StartingBankroll;
        var runningTicketBalance = 0m;
        var timeline = new List<DayTimelineEntry>();

        foreach (var item in events
            .OrderBy(item => item.Date)
            .ThenBy(item => EffectiveTime(item.Time))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id))
        {
            var bankrollBefore = runningBankroll;
            var ticketBalanceBefore = runningTicketBalance;
            runningBankroll += item.CashChange;
            runningTicketBalance += item.TicketChange;

            if (item.Date != date)
            {
                continue;
            }

            timeline.Add(new DayTimelineEntry(
                item.Date,
                item.Time,
                item.Type,
                item.Name,
                item.CostRisk,
                item.CashChange,
                item.TicketChange,
                bankrollBefore,
                runningBankroll,
                ticketBalanceBefore,
                runningTicketBalance,
                item.Rule));
        }

        return timeline;
    }

    public static List<DailySummary> GetDailySummaries(BankrollData data)
    {
        data.EnsureDefaults();
        var dates = data.TournamentEntries.Select(entry => entry.Date)
            .Concat(data.TournamentEntries.Where(HasTournamentSettlement).Select(TournamentSettlementDate))
            .Concat(data.CashSessions.Select(entry => entry.Date))
            .Concat(data.LedgerEntries.Select(entry => entry.Date))
            .Distinct()
            .Order()
            .ToList();

        var runningPoints = GetRunningBankroll(data);
        var bankrollByDate = runningPoints
            .GroupBy(point => point.Date)
            .ToDictionary(group => group.Key, group => group.Last().Bankroll);
        var bankrollValueByDate = runningPoints
            .GroupBy(point => point.Date)
            .ToDictionary(group => group.Key, group => group.Last().BankrollValue);

        var summaries = new List<DailySummary>();
        var currentMonth = DateOnly.MinValue;
        var runningMonthProfitLoss = 0m;

        foreach (var date in dates)
        {
            var month = new DateOnly(date.Year, date.Month, 1);
            if (month != currentMonth)
            {
                currentMonth = month;
                runningMonthProfitLoss = 0m;
            }

            var tournamentProfitLoss = TournamentProfitLossForDate(data, date);
            var cashProfitLoss = data.CashSessions
                .Where(entry => entry.Date == date)
                .Sum(entry => entry.NetProfit);
            var ticketProfitLoss = TicketProfitLossForDate(data, date);
            var totalProfitLoss = tournamentProfitLoss + cashProfitLoss;
            runningMonthProfitLoss += totalProfitLoss;

            summaries.Add(new DailySummary(
                date,
                tournamentProfitLoss,
                cashProfitLoss,
                ticketProfitLoss,
                totalProfitLoss,
                data.TournamentEntries.Count(entry => entry.Date == date)
                    + data.CashSessions.Count(entry => entry.Date == date),
                HoursPlayedForDate(data, date),
                runningMonthProfitLoss,
                bankrollByDate.GetValueOrDefault(date, data.Settings.StartingBankroll),
                bankrollValueByDate.GetValueOrDefault(date, data.Settings.StartingBankroll)));
        }

        return summaries;
    }

    public static List<MonthlySummary> GetMonthlySummaries(BankrollData data)
    {
        data.EnsureDefaults();
        var months = data.LedgerEntries.Select(entry => new DateOnly(entry.Date.Year, entry.Date.Month, 1))
            .Concat(data.TournamentEntries.Select(entry => new DateOnly(entry.Date.Year, entry.Date.Month, 1)))
            .Concat(data.TournamentEntries.Where(HasTournamentSettlement).Select(entry =>
            {
                var date = TournamentSettlementDate(entry);
                return new DateOnly(date.Year, date.Month, 1);
            }))
            .Concat(data.CashSessions.Select(entry => new DateOnly(entry.Date.Year, entry.Date.Month, 1)))
            .Distinct()
            .Order()
            .ToList();

        return months.Select(month => BuildMonthlySummary(data, month)).ToList();
    }

    public static List<YearlySummary> GetYearlySummaries(BankrollData data)
    {
        data.EnsureDefaults();
        var years = data.LedgerEntries.Select(entry => entry.Date.Year)
            .Concat(data.TournamentEntries.Select(entry => entry.Date.Year))
            .Concat(data.TournamentEntries.Where(HasTournamentSettlement).Select(entry => TournamentSettlementDate(entry).Year))
            .Concat(data.CashSessions.Select(entry => entry.Date.Year))
            .Distinct()
            .Order()
            .ToList();

        return years.Select(year => BuildYearlySummary(data, year)).ToList();
    }

    public static List<PlatformSummary> GetPlatformSummaries(BankrollData data)
    {
        data.EnsureDefaults();
        return Enum.GetValues<Platform>()
            .Select(platform =>
            {
                var ledgerEntries = data.LedgerEntries.Where(entry => entry.Platform == platform).ToList();
                var tournamentEntries = data.TournamentEntries.Where(entry => entry.Platform == platform).ToList();
                var cashSessions = data.CashSessions.Where(entry => entry.Platform == platform).ToList();
                var deposits = ledgerEntries
                    .Where(entry => entry.Type == LedgerType.Deposit)
                    .Sum(entry => Math.Abs(entry.Amount));
                var withdrawals = ledgerEntries
                    .Where(entry => entry.Type == LedgerType.Withdrawal)
                    .Sum(entry => Math.Abs(entry.Amount));
                var ledgerNet = ledgerEntries.Sum(SignedLedgerAmount);
                var tournamentProfitLoss = tournamentEntries.Sum(entry => entry.NetProfit);
                var cashSessionProfitLoss = cashSessions.Sum(entry => entry.NetProfit);
                var totalPokerProfitLoss = tournamentProfitLoss + cashSessionProfitLoss;
                var ticketBalance = TicketBalance(data, platform);
                var walletCashBalance = ledgerNet
                    + tournamentProfitLoss
                    + cashSessions.Sum(entry => entry.WalletCashImpact);
                var activeTableCash = cashSessions.Sum(entry => entry.ActiveTableCash);
                var totalPlatformExposure = walletCashBalance + activeTableCash;
                var wallet = data.PlatformWallets.FirstOrDefault(wallet => wallet.Platform == platform);
                return new PlatformSummary(
                    platform.ToString(),
                    walletCashBalance,
                    activeTableCash,
                    totalPlatformExposure,
                    deposits,
                    withdrawals,
                    ledgerNet,
                    tournamentProfitLoss,
                    cashSessionProfitLoss,
                    totalPokerProfitLoss,
                    ticketBalance,
                    tournamentEntries.Sum(entry => entry.CashCost) + cashSessions.Sum(entry => entry.SessionCost),
                    ledgerEntries.Count + tournamentEntries.Count + cashSessions.Count,
                    wallet?.ActualCashBalance,
                    wallet?.ActualCashBalance - walletCashBalance,
                    wallet?.LastUpdatedDate,
                    wallet?.Notes ?? string.Empty);
            })
            .Where(summary => summary.Count > 0
                || summary.TicketBalance != 0m
                || summary.ActualCashBalance.HasValue
                || !string.IsNullOrWhiteSpace(summary.Notes))
            .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<ComparisonSummary> GetPlatformComparison(BankrollData data)
    {
        return GetPlatformSummaries(data)
            .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .Select(summary => new ComparisonSummary(
                summary.Name,
                summary.TournamentProfitLoss,
                summary.CashSessionProfitLoss,
                summary.TotalPokerProfitLoss,
                summary.CashCost,
                summary.Count))
            .ToList();
    }

    public static List<ComparisonSummary> GetFormatComparison(BankrollData data)
    {
        data.EnsureDefaults();
        return data.TournamentEntries
            .GroupBy(entry => entry.Format)
            .OrderBy(group => group.Key.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ComparisonSummary(
                group.Key.ToString(),
                group.Sum(entry => entry.NetProfit),
                0m,
                group.Sum(entry => entry.NetProfit),
                group.Sum(entry => entry.CashCost),
                group.Count()))
            .ToList();
    }

    public static List<ComparisonSummary> GetCategoryComparison(BankrollData data)
    {
        data.EnsureDefaults();
        return Enum.GetValues<TournamentCategory>()
            .OrderBy(category => category.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(category =>
            {
                var tournamentEntries = data.TournamentEntries.Where(entry => entry.Category == category).ToList();
                var cashSessions = category == TournamentCategory.CashPractice ? data.CashSessions : [];
                var tournamentProfitLoss = tournamentEntries.Sum(entry => entry.NetProfit);
                var cashProfitLoss = cashSessions.Sum(entry => entry.NetProfit);
                return new ComparisonSummary(
                    category.ToString(),
                    tournamentProfitLoss,
                    cashProfitLoss,
                    tournamentProfitLoss + cashProfitLoss,
                    tournamentEntries.Sum(entry => entry.CashCost) + cashSessions.Sum(entry => entry.SessionCost),
                    tournamentEntries.Count + cashSessions.Count);
            })
            .Where(summary => summary.Count > 0)
            .ToList();
    }

    public static DashboardSummary GetDashboardSummary(BankrollData data, DateOnly today)
    {
        data.EnsureDefaults();
        var dailySummaries = GetDailySummaries(data);
        var monthStart = data.Settings.ActiveMonthStart;
        var thisMonthProfitLoss = dailySummaries
            .Where(summary => summary.Date >= monthStart)
            .Sum(summary => summary.TotalProfitLoss);
        var thisMonthValueProfitLoss = dailySummaries
            .Where(summary => summary.Date >= monthStart)
            .Sum(summary => summary.TotalValueProfitLoss);
        var todayProfitLoss = dailySummaries.FirstOrDefault(summary => summary.Date == today)?.TotalProfitLoss ?? 0m;
        var todayValueProfitLoss = dailySummaries.FirstOrDefault(summary => summary.Date == today)?.TotalValueProfitLoss ?? 0m;
        var bestDay = dailySummaries.Count == 0 ? null : dailySummaries.MaxBy(summary => summary.TotalValueProfitLoss);
        var worstDay = dailySummaries.Count == 0 ? null : dailySummaries.MinBy(summary => summary.TotalValueProfitLoss);

        return new DashboardSummary(
            CurrentBankroll(data),
            ActiveTableCash(data),
            TotalDeposits(data),
            TotalWithdrawals(data),
            TicketBalance(data),
            TotalPokerProfitLoss(data),
            TournamentProfitLoss(data),
            CashProfitLoss(data),
            todayProfitLoss,
            todayValueProfitLoss,
            thisMonthProfitLoss,
            thisMonthValueProfitLoss,
            bestDay,
            worstDay,
            StopLossService.GetStatus(data, today),
            GetBankrollTier(data));
    }

    public static void RecalculateTrackingFields(BankrollData data)
    {
        data.EnsureDefaults();
        var runningBankroll = data.Settings.StartingBankroll;

        var events = new List<TrackingEvent>();
        events.AddRange(data.LedgerEntries.Select(entry => new TrackingEvent(entry.Date, null, 0, entry.Id, "Ledger")));
        events.AddRange(data.TournamentEntries.Select(entry => new TrackingEvent(entry.Date, entry.RegistrationTime, 1, entry.Id, "TournamentRegistration")));
        events.AddRange(data.TournamentEntries
            .Where(HasTournamentSettlement)
            .Select(entry => new TrackingEvent(TournamentSettlementDate(entry), TournamentSettlementTime(entry), 2, entry.Id, "TournamentSettlement")));
        events.AddRange(data.CashSessions.Select(entry => new TrackingEvent(entry.Date, entry.SessionTime, 3, entry.Id, "Cash")));

        foreach (var item in events
            .OrderBy(item => item.Date)
            .ThenBy(item => EffectiveTime(item.Time))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id))
        {
            if (item.Kind == "Ledger")
            {
                var ledgerEntry = data.LedgerEntries.First(entry => entry.Id == item.Id);
                ledgerEntry.BankrollBefore = runningBankroll;
                runningBankroll += SignedLedgerAmount(ledgerEntry);
                ledgerEntry.BankrollAfter = runningBankroll;
                continue;
            }

            if (item.Kind == "TournamentRegistration")
            {
                var tournamentEntry = data.TournamentEntries.First(entry => entry.Id == item.Id);
                tournamentEntry.BankrollBefore = runningBankroll;
                tournamentEntry.RiskPercentageOfBankrollAtRegistration = RiskPercentage(tournamentEntry.CashCost, runningBankroll);
                tournamentEntry.RuleCheckResult = RuleEngine.EvaluateHistoricalRisk(
                    data.Settings,
                    isCashSession: false,
                    tournamentEntry.Category,
                    tournamentEntry.Format,
                    tournamentEntry.CashCost,
                    tournamentEntry.ActualBullets,
                    runningBankroll);
                runningBankroll += TournamentRegistrationAmount(tournamentEntry);
                tournamentEntry.BankrollAfter = runningBankroll;
                continue;
            }

            if (item.Kind == "TournamentSettlement")
            {
                var tournamentEntry = data.TournamentEntries.First(entry => entry.Id == item.Id);
                runningBankroll += tournamentEntry.ReturnAmount;
                tournamentEntry.BankrollAfter = runningBankroll;
                continue;
            }

            var cashSession = data.CashSessions.First(entry => entry.Id == item.Id);
            cashSession.BankrollBefore = runningBankroll;
            cashSession.RiskPercentageOfBankrollAtSessionStart = RiskPercentage(cashSession.SessionCost, runningBankroll);
            cashSession.RuleCheckResult = RuleEngine.EvaluateHistoricalRisk(
                data.Settings,
                isCashSession: true,
                TournamentCategory.CashPractice,
                TournamentFormat.Other,
                cashSession.SessionCost,
                1,
                runningBankroll,
                cashSession.Format);
            runningBankroll += cashSession.NetProfit;
            cashSession.BankrollAfter = runningBankroll;
        }
    }

    private static MonthlySummary BuildMonthlySummary(BankrollData data, DateOnly month)
    {
        var nextMonth = month.AddMonths(1);
        var ledgerEntries = data.LedgerEntries.Where(entry => entry.Date >= month && entry.Date < nextMonth).ToList();
        var tournamentEntries = data.TournamentEntries.Where(entry => entry.Date >= month && entry.Date < nextMonth).ToList();
        var cashSessions = data.CashSessions.Where(entry => entry.Date >= month && entry.Date < nextMonth).ToList();
        var netResults = tournamentEntries.Select(entry => entry.TotalValueProfitLoss).Concat(cashSessions.Select(entry => entry.NetProfit)).ToList();
        var dailyStopLossLimit = data.Settings.DailyStopLossAmount;
        var stopLossBreaches = dailyStopLossLimit <= 0m
            ? 0
            : GetDailySummaries(data)
                .Count(summary => summary.Date >= month && summary.Date < nextMonth && summary.TotalProfitLoss <= -dailyStopLossLimit);

        var tournamentProfitLoss = TournamentProfitLossForRange(data, month, nextMonth);
        var cashProfitLoss = cashSessions.Sum(entry => entry.NetProfit);
        var ticketProfitLoss = TicketProfitLossForRange(data, month, nextMonth);
        return new MonthlySummary(
            month,
            ledgerEntries.Where(entry => entry.Type == LedgerType.Deposit).Sum(entry => Math.Abs(entry.Amount)),
            ledgerEntries.Where(entry => entry.Type == LedgerType.Withdrawal).Sum(entry => Math.Abs(entry.Amount)),
            tournamentProfitLoss,
            cashProfitLoss,
            ticketProfitLoss,
            tournamentProfitLoss + cashProfitLoss,
            tournamentEntries.Count,
            cashSessions.Count,
            HoursPlayedForRange(data, month, nextMonth),
            tournamentEntries.Count == 0 ? 0m : tournamentEntries.Average(entry => entry.BuyIn),
            netResults.Count == 0 ? 0m : netResults.Max(),
            netResults.Count == 0 ? 0m : netResults.Min(),
            stopLossBreaches,
            string.Empty);
    }

    private static YearlySummary BuildYearlySummary(BankrollData data, int year)
    {
        var ledgerEntries = data.LedgerEntries.Where(entry => entry.Date.Year == year).ToList();
        var tournamentEntries = data.TournamentEntries.Where(entry => entry.Date.Year == year).ToList();
        var cashSessions = data.CashSessions.Where(entry => entry.Date.Year == year).ToList();
        var netResults = tournamentEntries.Select(entry => entry.TotalValueProfitLoss).Concat(cashSessions.Select(entry => entry.NetProfit)).ToList();
        var dailyStopLossLimit = data.Settings.DailyStopLossAmount;
        var stopLossBreaches = dailyStopLossLimit <= 0m
            ? 0
            : GetDailySummaries(data)
                .Count(summary => summary.Date.Year == year && summary.TotalProfitLoss <= -dailyStopLossLimit);

        var tournamentProfitLoss = TournamentProfitLossForRange(data, new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1));
        var cashProfitLoss = cashSessions.Sum(entry => entry.NetProfit);
        var ticketProfitLoss = TicketProfitLossForRange(data, new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1));
        return new YearlySummary(
            year,
            ledgerEntries.Where(entry => entry.Type == LedgerType.Deposit).Sum(entry => Math.Abs(entry.Amount)),
            ledgerEntries.Where(entry => entry.Type == LedgerType.Withdrawal).Sum(entry => Math.Abs(entry.Amount)),
            tournamentProfitLoss,
            cashProfitLoss,
            ticketProfitLoss,
            tournamentProfitLoss + cashProfitLoss,
            tournamentEntries.Count,
            cashSessions.Count,
            HoursPlayedForRange(data, new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1)),
            tournamentEntries.Count == 0 ? 0m : tournamentEntries.Average(entry => entry.BuyIn),
            netResults.Count == 0 ? 0m : netResults.Max(),
            netResults.Count == 0 ? 0m : netResults.Min(),
            stopLossBreaches,
            string.Empty);
    }

    private static string GetBankrollTier(BankrollData data)
    {
        var bankroll = CurrentBankroll(data);
        var settings = data.Settings;

        if (bankroll <= 0m)
        {
            return "Unfunded";
        }

        if (bankroll < settings.ProtectModeBelowBankroll)
        {
            return "Protect Mode";
        }

        if (bankroll < settings.GreenLightShotBankroll)
        {
            return "Main Grind";
        }

        if (bankroll < settings.MoveUpReviewBankroll)
        {
            return "Green-Light Shots";
        }

        return "Move-Up Review";
    }

    private sealed record RunningEvent(
        DateOnly Date,
        TimeOnly? Time,
        int SortOrder,
        string Source,
        string Label,
        decimal CashAmount,
        decimal TicketAmount,
        Guid Id);

    private sealed record TrackingEvent(DateOnly Date, TimeOnly? Time, int SortOrder, Guid Id, string Kind);

    private sealed record AuditEvent(
        DateOnly Date,
        TimeOnly? Time,
        int SortOrder,
        Guid Id,
        string Type,
        string Name,
        decimal CostRisk,
        decimal Result,
        string Rule);

    private sealed record DayTimelineEvent(
        DateOnly Date,
        TimeOnly? Time,
        int SortOrder,
        Guid Id,
        string Type,
        string Name,
        decimal CostRisk,
        decimal CashChange,
        decimal TicketChange,
        string Rule);

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

    private static decimal TournamentRegistrationAmount(TournamentEntry entry)
    {
        return UsesSplitTournamentSettlement(entry) ? -entry.CashCost : entry.NetProfit;
    }

    private static decimal TournamentTicketRegistrationAmount(TournamentEntry entry)
    {
        return UsesSplitTournamentSettlement(entry) ? -entry.TicketBuyInValue : entry.TicketBalanceImpact;
    }

    private static decimal TournamentTicketSettlementAmount(TournamentEntry entry)
    {
        return entry.TicketReturnAmount;
    }

    private static decimal TicketReturnForPlatform(TournamentEntry entry, Platform platform)
    {
        return entry.Platform == platform ? entry.TicketReturnAmount : 0m;
    }

    private static decimal TicketBuyInForPlatform(TournamentEntry entry, Platform platform)
    {
        return entry.EffectiveTicketBuyInPlatform == platform ? entry.TicketBuyInValue : 0m;
    }

    private static DateOnly TournamentSettlementDate(TournamentEntry entry)
    {
        return entry.FinishedDate ?? entry.Date;
    }

    private static TimeOnly? TournamentSettlementTime(TournamentEntry entry)
    {
        return entry.FinishedTime ?? entry.RegistrationTime;
    }

    private static string CashSessionName(CashSession entry)
    {
        var name = $"{entry.Game} {entry.Stakes}".Trim();
        return string.IsNullOrWhiteSpace(name) ? entry.Format.ToString() : name;
    }

    private static decimal TournamentProfitLossForDate(BankrollData data, DateOnly date)
    {
        var registrationAmount = data.TournamentEntries
            .Where(entry => entry.Date == date)
            .Sum(TournamentRegistrationAmount);
        var settlementReturns = data.TournamentEntries
            .Where(entry => HasTournamentSettlement(entry) && TournamentSettlementDate(entry) == date)
            .Sum(entry => entry.ReturnAmount);
        return registrationAmount + settlementReturns;
    }

    private static decimal TicketProfitLossForDate(BankrollData data, DateOnly date)
    {
        var registrationAmount = data.TournamentEntries
            .Where(entry => entry.Date == date)
            .Sum(TournamentTicketRegistrationAmount);
        var settlementReturns = data.TournamentEntries
            .Where(entry => HasTournamentSettlement(entry) && TournamentSettlementDate(entry) == date)
            .Sum(TournamentTicketSettlementAmount);
        return registrationAmount + settlementReturns;
    }

    private static decimal TournamentProfitLossForRange(BankrollData data, DateOnly startInclusive, DateOnly endExclusive)
    {
        var registrationAmount = data.TournamentEntries
            .Where(entry => entry.Date >= startInclusive && entry.Date < endExclusive)
            .Sum(TournamentRegistrationAmount);
        var settlementReturns = data.TournamentEntries
            .Where(entry => HasTournamentSettlement(entry))
            .Where(entry =>
            {
                var settlementDate = TournamentSettlementDate(entry);
                return settlementDate >= startInclusive && settlementDate < endExclusive;
            })
            .Sum(entry => entry.ReturnAmount);
        return registrationAmount + settlementReturns;
    }

    private static decimal TicketProfitLossForRange(BankrollData data, DateOnly startInclusive, DateOnly endExclusive)
    {
        var registrationAmount = data.TournamentEntries
            .Where(entry => entry.Date >= startInclusive && entry.Date < endExclusive)
            .Sum(TournamentTicketRegistrationAmount);
        var settlementReturns = data.TournamentEntries
            .Where(HasTournamentSettlement)
            .Where(entry =>
            {
                var settlementDate = TournamentSettlementDate(entry);
                return settlementDate >= startInclusive && settlementDate < endExclusive;
            })
            .Sum(TournamentTicketSettlementAmount);
        return registrationAmount + settlementReturns;
    }

    private static decimal HoursPlayedForDate(BankrollData data, DateOnly date)
    {
        return data.TournamentEntries
            .Where(entry => entry.Date == date)
            .Sum(TournamentHours)
            + data.CashSessions
                .Where(entry => entry.Date == date)
                .Sum(CashSessionHours);
    }

    private static decimal HoursPlayedForRange(BankrollData data, DateOnly startInclusive, DateOnly endExclusive)
    {
        return data.TournamentEntries
            .Where(entry => entry.Date >= startInclusive && entry.Date < endExclusive)
            .Sum(TournamentHours)
            + data.CashSessions
                .Where(entry => entry.Date >= startInclusive && entry.Date < endExclusive)
                .Sum(CashSessionHours);
    }

    private static decimal TournamentHours(TournamentEntry entry)
    {
        if (entry.Status != TournamentStatus.Finished
            || entry.RegistrationTime is not { } registeredAt
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

    private static TimeOnly EffectiveTime(TimeOnly? time)
    {
        return time ?? TimeOnly.MinValue;
    }
}
