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
            .Where(IsExternalMonthlyFunding)
            .Select(SignedLedgerAmount)
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
        var dates = new HashSet<DateOnly>(data.LedgerEntries.Select(entry => entry.Date));
        var tournamentProfitByDate = new Dictionary<DateOnly, decimal>();
        var cashProfitByDate = new Dictionary<DateOnly, decimal>();
        var ticketProfitByDate = new Dictionary<DateOnly, decimal>();
        var sessionCountByDate = new Dictionary<DateOnly, int>();
        var hoursByDate = new Dictionary<DateOnly, decimal>();

        foreach (var entry in data.TournamentEntries)
        {
            dates.Add(entry.Date);
            AddDecimal(tournamentProfitByDate, entry.Date, TournamentRegistrationAmount(entry));
            AddDecimal(ticketProfitByDate, entry.Date, TournamentTicketRegistrationAmount(entry));
            AddInt(sessionCountByDate, entry.Date, 1);
            AddDecimal(hoursByDate, entry.Date, TournamentHours(entry));

            if (!HasTournamentSettlement(entry))
            {
                continue;
            }

            var settlementDate = TournamentSettlementDate(entry);
            dates.Add(settlementDate);
            AddDecimal(tournamentProfitByDate, settlementDate, entry.ReturnAmount);
            AddDecimal(ticketProfitByDate, settlementDate, TournamentTicketSettlementAmount(entry));
        }

        foreach (var entry in data.CashSessions)
        {
            dates.Add(entry.Date);
            AddDecimal(cashProfitByDate, entry.Date, entry.NetProfit);
            AddInt(sessionCountByDate, entry.Date, 1);
            AddDecimal(hoursByDate, entry.Date, CashSessionHours(entry));
        }

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

        foreach (var date in dates.Order())
        {
            var month = new DateOnly(date.Year, date.Month, 1);
            if (month != currentMonth)
            {
                currentMonth = month;
                runningMonthProfitLoss = 0m;
            }

            var tournamentProfitLoss = tournamentProfitByDate.GetValueOrDefault(date);
            var cashProfitLoss = cashProfitByDate.GetValueOrDefault(date);
            var ticketProfitLoss = ticketProfitByDate.GetValueOrDefault(date);
            var totalProfitLoss = tournamentProfitLoss + cashProfitLoss;
            runningMonthProfitLoss += totalProfitLoss;

            summaries.Add(new DailySummary(
                date,
                tournamentProfitLoss,
                cashProfitLoss,
                ticketProfitLoss,
                totalProfitLoss,
                sessionCountByDate.GetValueOrDefault(date),
                hoursByDate.GetValueOrDefault(date),
                runningMonthProfitLoss,
                bankrollByDate.GetValueOrDefault(date, data.Settings.StartingBankroll),
                bankrollValueByDate.GetValueOrDefault(date, data.Settings.StartingBankroll)));
        }

        return summaries;

        static void AddDecimal(Dictionary<DateOnly, decimal> values, DateOnly date, decimal amount)
        {
            values[date] = values.GetValueOrDefault(date) + amount;
        }

        static void AddInt(Dictionary<DateOnly, int> values, DateOnly date, int amount)
        {
            values[date] = values.GetValueOrDefault(date) + amount;
        }
    }

    public static List<MonthlySummary> GetMonthlySummaries(BankrollData data)
    {
        return GetMonthlySummaries(data, GetDailySummaries(data));
    }

    public static List<MonthlySummary> GetMonthlySummaries(BankrollData data, IReadOnlyList<DailySummary> dailySummaries)
    {
        data.EnsureDefaults();
        var dailyByMonth = dailySummaries
            .GroupBy(summary => MonthOf(summary.Date))
            .ToDictionary(group => group.Key, group => group.ToList());
        var ledgerByMonth = data.LedgerEntries
            .GroupBy(entry => MonthOf(entry.Date))
            .ToDictionary(group => group.Key, group => group.ToList());
        var tournamentByMonth = data.TournamentEntries
            .GroupBy(entry => MonthOf(entry.Date))
            .ToDictionary(group => group.Key, group => group.ToList());
        var cashByMonth = data.CashSessions
            .GroupBy(entry => MonthOf(entry.Date))
            .ToDictionary(group => group.Key, group => group.ToList());
        var months = dailyByMonth.Keys
            .Concat(ledgerByMonth.Keys)
            .Concat(tournamentByMonth.Keys)
            .Concat(cashByMonth.Keys)
            .Distinct()
            .Order();

        return months.Select(month =>
        {
            var daily = dailyByMonth.GetValueOrDefault(month) ?? [];
            var ledgerEntries = ledgerByMonth.GetValueOrDefault(month) ?? [];
            var tournamentEntries = tournamentByMonth.GetValueOrDefault(month) ?? [];
            var cashSessions = cashByMonth.GetValueOrDefault(month) ?? [];
            var netResults = tournamentEntries
                .Select(entry => entry.TotalValueProfitLoss)
                .Concat(cashSessions.Select(entry => entry.NetProfit))
                .ToList();
            var dailyStopLossLimit = data.Settings.DailyStopLossAmount;
            var tournamentProfitLoss = daily.Sum(summary => summary.TournamentProfitLoss);
            var cashProfitLoss = daily.Sum(summary => summary.CashProfitLoss);
            var ticketProfitLoss = daily.Sum(summary => summary.TicketProfitLoss);

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
                daily.Sum(summary => summary.HoursPlayed),
                tournamentEntries.Count == 0 ? 0m : tournamentEntries.Average(entry => entry.BuyIn),
                netResults.Count == 0 ? 0m : netResults.Max(),
                netResults.Count == 0 ? 0m : netResults.Min(),
                dailyStopLossLimit <= 0m ? 0 : daily.Count(summary => summary.TotalProfitLoss <= -dailyStopLossLimit),
                string.Empty);
        }).ToList();
    }

    public static List<YearlySummary> GetYearlySummaries(BankrollData data)
    {
        return GetYearlySummaries(data, GetDailySummaries(data));
    }

    public static List<YearlySummary> GetYearlySummaries(BankrollData data, IReadOnlyList<DailySummary> dailySummaries)
    {
        data.EnsureDefaults();
        var dailyByYear = dailySummaries
            .GroupBy(summary => summary.Date.Year)
            .ToDictionary(group => group.Key, group => group.ToList());
        var ledgerByYear = data.LedgerEntries
            .GroupBy(entry => entry.Date.Year)
            .ToDictionary(group => group.Key, group => group.ToList());
        var tournamentByYear = data.TournamentEntries
            .GroupBy(entry => entry.Date.Year)
            .ToDictionary(group => group.Key, group => group.ToList());
        var cashByYear = data.CashSessions
            .GroupBy(entry => entry.Date.Year)
            .ToDictionary(group => group.Key, group => group.ToList());
        var years = dailyByYear.Keys
            .Concat(ledgerByYear.Keys)
            .Concat(tournamentByYear.Keys)
            .Concat(cashByYear.Keys)
            .Distinct()
            .Order();

        return years.Select(year =>
        {
            var daily = dailyByYear.GetValueOrDefault(year) ?? [];
            var ledgerEntries = ledgerByYear.GetValueOrDefault(year) ?? [];
            var tournamentEntries = tournamentByYear.GetValueOrDefault(year) ?? [];
            var cashSessions = cashByYear.GetValueOrDefault(year) ?? [];
            var netResults = tournamentEntries
                .Select(entry => entry.TotalValueProfitLoss)
                .Concat(cashSessions.Select(entry => entry.NetProfit))
                .ToList();
            var dailyStopLossLimit = data.Settings.DailyStopLossAmount;
            var tournamentProfitLoss = daily.Sum(summary => summary.TournamentProfitLoss);
            var cashProfitLoss = daily.Sum(summary => summary.CashProfitLoss);
            var ticketProfitLoss = daily.Sum(summary => summary.TicketProfitLoss);

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
                daily.Sum(summary => summary.HoursPlayed),
                tournamentEntries.Count == 0 ? 0m : tournamentEntries.Average(entry => entry.BuyIn),
                netResults.Count == 0 ? 0m : netResults.Max(),
                netResults.Count == 0 ? 0m : netResults.Min(),
                dailyStopLossLimit <= 0m ? 0 : daily.Count(summary => summary.TotalProfitLoss <= -dailyStopLossLimit),
                string.Empty);
        }).ToList();
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
                var actualCashBalance = wallet?.AcceptedCashDifference is { } acceptedDifference
                    ? walletCashBalance + acceptedDifference
                    : wallet?.ActualCashBalance;
                var difference = actualCashBalance - walletCashBalance - (wallet?.AcceptedCashDifference ?? 0m);
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
                    actualCashBalance,
                    difference,
                    wallet?.AcceptedCashDifference,
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
        return GetDashboardSummary(data, today, GetDailySummaries(data));
    }

    public static DashboardSummary GetDashboardSummary(BankrollData data, DateOnly today, IReadOnlyList<DailySummary> dailySummaries)
    {
        data.EnsureDefaults();
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
            StopLossService.GetStatus(data, today, dailySummaries),
            GetBankrollTier(data));
    }

    public static void RecalculateTrackingFields(BankrollData data)
    {
        data.EnsureDefaults();
        var runningBankroll = data.Settings.StartingBankroll;
        var ledgerById = data.LedgerEntries.ToDictionary(entry => entry.Id);
        var tournamentById = data.TournamentEntries.ToDictionary(entry => entry.Id);
        var cashById = data.CashSessions.ToDictionary(entry => entry.Id);

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
                var ledgerEntry = ledgerById[item.Id];
                ledgerEntry.BankrollBefore = runningBankroll;
                runningBankroll += SignedLedgerAmount(ledgerEntry);
                ledgerEntry.BankrollAfter = runningBankroll;
                continue;
            }

            if (item.Kind == "TournamentRegistration")
            {
                var tournamentEntry = tournamentById[item.Id];
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
                var tournamentEntry = tournamentById[item.Id];
                runningBankroll += tournamentEntry.ReturnAmount;
                tournamentEntry.BankrollAfter = runningBankroll;
                continue;
            }

            var cashSession = cashById[item.Id];
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

    private static DateOnly MonthOf(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
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

    private static bool IsExternalMonthlyFunding(LedgerEntry entry)
    {
        return entry.Type != LedgerType.TransferIn
            && SignedLedgerAmount(entry) > 0m;
    }

    private static TimeOnly EffectiveTime(TimeOnly? time)
    {
        return time ?? TimeOnly.MinValue;
    }
}
