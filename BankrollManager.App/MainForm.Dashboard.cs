using System.ComponentModel;
using System.Globalization;
using System.Drawing.Drawing2D;
using BankrollManager.App.Controls;
using BankrollManager.App.Forms;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using Microsoft.Win32;

namespace BankrollManager.App;

public sealed partial class MainForm
{

    private Control BuildDashboardTab()
    {
        const int minimumDashboardHeight = 920;
        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back
        };

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = minimumDashboardHeight,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Back
        };
        viewport.Controls.Add(shell);
        viewport.Resize += (_, _) =>
        {
            shell.Height = Math.Max(viewport.ClientSize.Height, minimumDashboardHeight);
        };

        var guardrailColumn = new ColumnStyle(SizeType.Absolute, 330);
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.ColumnStyles.Add(guardrailColumn);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            BackColor = Theme.Back,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 292));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 244));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(root, 0, 0);
        shell.Controls.Add(BuildGuardrailRail(), 1, 0);

        _stopLossBanner = Theme.Label(string.Empty, Theme.HeaderFont, Theme.Text);
        _stopLossBanner.AutoSize = false;
        _stopLossBanner.AutoEllipsis = true;
        _stopLossBanner.Dock = DockStyle.Fill;
        _stopLossBanner.TextAlign = ContentAlignment.MiddleLeft;
        _stopLossBanner.Padding = new Padding(18, 8, 18, 8);
        _stopLossBanner.BackColor = Theme.Panel;
        root.Controls.Add(_stopLossBanner, 0, 0);

        var kpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(kpis, 0, 1);

        foreach (var title in new[]
        {
            "Overall value", "Cash bankroll", "Available funds", "Reserved target", "Today value P/L", "This month value P/L"
        })
        {
            AddKpi(kpis, title);
        }

        _runningChart = new MiniChart { Dock = DockStyle.Fill, Margin = new Padding(6, 4, 6, 8) };
        _runningChart.PointActivated += (_, e) => OpenRunningChartPoint(e.Point);
        root.Controls.Add(_runningChart, 0, 2);

        var overview = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Back
        };
        overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        root.Controls.Add(overview, 0, 3);

        _overviewAttentionGrid = CreateGrid(_overviewAttentionSource);
        _overviewAttentionGrid.CellDoubleClick += (_, _) => OpenSelectedAttentionItem();
        AddTextColumn(_overviewAttentionGrid, "Severity", "Need", 70);
        AddTextColumn(_overviewAttentionGrid, "Area", "Area", 86);
        AddTextColumn(_overviewAttentionGrid, "Summary", "Summary", 280);
        AddTextColumn(_overviewAttentionGrid, "Action", "Action", 112);
        overview.Controls.Add(BuildOverviewPanel("Needs attention", _overviewAttentionGrid, OpenSelectedAttentionItem, "Open"), 0, 0);

        _overviewOpenGrid = CreateGrid(_overviewOpenTournamentSource);
        _overviewOpenGrid.CellDoubleClick += (_, _) => EditSelectedTournament(_overviewOpenTournamentSource);
        AddTextColumn(_overviewOpenGrid, "Date", "Date", 92);
        AddTextColumn(_overviewOpenGrid, "RegistrationTime", "Time", 70);
        AddTextColumn(_overviewOpenGrid, "Status", "Status", 88);
        AddTextColumn(_overviewOpenGrid, "EventName", "Tournament/Event", 210);
        AddTextColumn(_overviewOpenGrid, "CashCost", "Cash Cost", 90);
        AddTextColumn(_overviewOpenGrid, "TicketBuyInValue", "Ticket", 82);
        AddTextColumn(_overviewOpenGrid, "RuleCheckResult", "Rule", 96);
        overview.Controls.Add(BuildOverviewPanel("Open tournaments", _overviewOpenGrid, () => EditSelectedTournament(_overviewOpenTournamentSource)), 1, 0);

        _overviewActivityGrid = CreateGrid(_overviewRecentActivitySource);
        AddTextColumn(_overviewActivityGrid, "Date", "Date", 92);
        AddTextColumn(_overviewActivityGrid, "Time", "Time", 70);
        AddTextColumn(_overviewActivityGrid, "Type", "Type", 110);
        AddTextColumn(_overviewActivityGrid, "Name", "Name", 230);
        AddTextColumn(_overviewActivityGrid, "Result", "Result", 90);
        AddTextColumn(_overviewActivityGrid, "BankrollAfter", "Cash After", 104);
        overview.Controls.Add(BuildOverviewPanel("Recent activity", _overviewActivityGrid), 2, 0);

        var charts = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Back
        };
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        charts.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(charts, 0, 4);

        _dailyChart = new MiniChart { Dock = DockStyle.Fill };
        _comparisonChart = new MiniChart { Dock = DockStyle.Fill };
        _monthlyChart = new MiniChart { Dock = DockStyle.Fill };
        _dailyChart.PointActivated += (_, e) => OpenDailyChartPoint(e.Point);
        _comparisonChart.PointActivated += (_, e) => OpenComparisonChartPoint(e.Point);
        _monthlyChart.PointActivated += (_, e) => OpenMonthlyChartPoint(e.Point);
        charts.Controls.Add(_dailyChart, 0, 0);
        charts.Controls.Add(_comparisonChart, 1, 0);
        charts.Controls.Add(_monthlyChart, 2, 0);

        return viewport;
    }


    private void RefreshDashboard(BankrollViewData viewData)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var summary = BankrollCalculator.GetDashboardSummary(_data, today, viewData.DailySummaries);
        var settings = _data.Settings;
        var mainGrindRule = settings.GetRule(TournamentCategory.MainGrind);
        var reserveTarget = summary.CurrentBankroll * settings.ReserveTargetPercent / 100m;
        var availableFunds = Math.Max(0m, summary.CurrentBankroll - reserveTarget - summary.ActiveTableCash);
        var suggestedUnit = summary.CurrentBankroll * mainGrindRule.MaxRiskPercent / 100m;
        var maxSessionRisk = summary.CurrentBankroll * settings.CashSessionMaxRiskPercent / 100m;
        var dailyCommittedRisk = DailyCommittedRisk(today);
        var dailyRiskCap = summary.CurrentBankroll > 0m
            ? summary.CurrentBankroll * settings.DailyRiskCapPercent / 100m
            : 0m;
        var activeExposure = ActiveExposure();
        var activeExposureCap = summary.CurrentBankroll > 0m
            ? summary.CurrentBankroll * settings.ActiveExposureCapPercent / 100m
            : 0m;
        var dailyStopLeft = StopLossRemaining(summary.StopLossStatus.TodayProfitLoss, summary.StopLossStatus.DailyStopLossLimit);
        var monthlyStopLeft = StopLossRemaining(summary.StopLossStatus.ThisMonthProfitLoss, summary.StopLossStatus.MonthlyStopLossLimit);
        var highWaterValue = viewData.RunningBankroll.Count == 0
            ? summary.CurrentBankrollValue
            : Math.Max(summary.CurrentBankrollValue, viewData.RunningBankroll.Max(point => point.BankrollValue));
        var drawdown = Math.Max(0m, highWaterValue - summary.CurrentBankrollValue);

        SetKpi("Overall value", Money(summary.CurrentBankrollValue), summary.CurrentBankrollValue);
        SetKpi("Cash bankroll", Money(summary.CurrentBankroll), summary.CurrentBankroll);
        SetKpi("Available funds", Money(availableFunds), availableFunds);
        SetKpi("Reserved target", Money(reserveTarget), -reserveTarget);
        SetKpi("On tables", Money(summary.ActiveTableCash), summary.ActiveTableCash);
        SetKpi("Total deposits", Money(summary.TotalDeposits), summary.TotalDeposits);
        SetKpi("Total withdrawals", Money(summary.TotalWithdrawals), -summary.TotalWithdrawals);
        SetKpi("Total value P/L", Money(summary.TotalValueProfitLoss), summary.TotalValueProfitLoss);
        SetKpi("Total poker P/L", Money(summary.TotalPokerProfitLoss), summary.TotalPokerProfitLoss);
        SetKpi("Tournament value P/L", Money(summary.TournamentValueProfitLoss), summary.TournamentValueProfitLoss);
        SetKpi("Tournament P/L", Money(summary.TournamentProfitLoss), summary.TournamentProfitLoss);
        SetKpi("Cash P/L", Money(summary.CashProfitLoss), summary.CashProfitLoss);
        SetKpi("Today P/L", Money(summary.TodayProfitLoss), summary.TodayProfitLoss);
        SetKpi("Today value P/L", Money(summary.TodayValueProfitLoss), summary.TodayValueProfitLoss);
        SetKpi("This month P/L", Money(summary.ThisMonthProfitLoss), summary.ThisMonthProfitLoss);
        SetKpi("This month value P/L", Money(summary.ThisMonthValueProfitLoss), summary.ThisMonthValueProfitLoss);
        SetKpi("Tickets available", Money(summary.TicketBalance), summary.TicketBalance);
        SetKpi("Best day", summary.BestDay is null ? "-" : $"{summary.BestDay.Date:yyyy-MM-dd}  {Money(summary.BestDay.TotalValueProfitLoss)}", summary.BestDay?.TotalValueProfitLoss ?? 0m);
        SetKpi("Worst day", summary.WorstDay is null ? "-" : $"{summary.WorstDay.Date:yyyy-MM-dd}  {Money(summary.WorstDay.TotalValueProfitLoss)}", summary.WorstDay?.TotalValueProfitLoss ?? 0m);
        SetKpi("Stop-loss status", summary.StopLossStatus.StatusText, summary.StopLossStatus.BreakRequired ? -1m : 1m);
        SetKpi("Risk status", summary.StopLossStatus.StatusText, summary.StopLossStatus.BreakRequired ? -1m : 1m);
        SetKpi("Suggested unit", Money(suggestedUnit), suggestedUnit);
        SetKpi("Max session risk", Money(maxSessionRisk), maxSessionRisk);
        SetKpi("Daily stop left", Money(dailyStopLeft), dailyStopLeft);
        SetKpi("Monthly stop left", Money(monthlyStopLeft), monthlyStopLeft);
        SetKpi("Daily risk left", Money(dailyRiskCap - dailyCommittedRisk), dailyRiskCap - dailyCommittedRisk);
        SetKpi("Exposure left", Money(activeExposureCap - activeExposure), activeExposureCap - activeExposure);
        SetKpi("Current drawdown", Money(drawdown), -drawdown);
        SetKpi("Protect mode", summary.StopLossStatus.ProtectModeActive ? "ACTIVE" : "Off", summary.StopLossStatus.ProtectModeActive ? -1m : 1m);
        SetKpi("Bankroll tier", summary.BankrollTier, summary.CurrentBankroll);

        if (!FirstRunSetupService.HasUserData(_data))
        {
            _stopLossBanner.Text = "Clean cash bankroll ready - run Setup or add your first deposit.";
            _stopLossBanner.BackColor = Theme.AccentSurface;
        }
        else
        {
            _stopLossBanner.Text = summary.StopLossStatus.BreakRequired
                ? $"TAKE BREAK - {summary.StopLossStatus.Explanation}"
                : "OK - no stop-loss or protect-mode rule is active";
            _stopLossBanner.BackColor = summary.StopLossStatus.BreakRequired ? Theme.DangerSurface : Theme.PositiveSurface;
        }

        var daily = viewData.DailySummaries.OrderBy(summary => summary.Date).ToList();
        var running = viewData.RunningBankroll.OrderBy(point => point.Date).ToList();
        var monthly = viewData.MonthlySummaries.OrderBy(summary => summary.Month).ToList();
        var dailyPoints = daily.Select(summary => new MiniChartPoint(
            summary.Date.ToString("MM-dd"),
            summary.TotalValueProfitLoss,
            summary.Date,
            $"Daily Value P&L{Environment.NewLine}{summary.Date:yyyy-MM-dd}{Environment.NewLine}Value: {Money(summary.TotalValueProfitLoss)}{Environment.NewLine}Cash P/L: {Money(summary.TotalProfitLoss)}{Environment.NewLine}Tickets: {Money(summary.TicketProfitLoss)}{Environment.NewLine}Tournaments cash: {Money(summary.TournamentProfitLoss)}{Environment.NewLine}Cash sessions: {Money(summary.CashProfitLoss)}{Environment.NewLine}Sessions: {summary.NumberOfSessions}"));
        _dailyChart.SetData("Daily Value P&L", dailyPoints, MiniChartKind.Bars);
        _dailyReviewChart.SetData("Daily Value P&L", dailyPoints, MiniChartKind.Bars);
        _runningChart.SetData("Running Overall Value", running.Select(point => new MiniChartPoint(
            point.Date.ToString("MM-dd"),
            point.BankrollValue,
            point,
            $"Running Overall Value{Environment.NewLine}{point.Date:yyyy-MM-dd}{Environment.NewLine}{point.Label}{Environment.NewLine}Value change: {Money(point.ValueAmount)}{Environment.NewLine}Cash change: {Money(point.Amount)}{Environment.NewLine}Ticket change: {Money(point.TicketAmount)}{Environment.NewLine}Overall value: {Money(point.BankrollValue)}{Environment.NewLine}Cash bankroll: {Money(point.Bankroll)}{Environment.NewLine}Tickets: {Money(point.TicketBalance)}")), MiniChartKind.Line);
        _comparisonChart.SetData("Tournament Value vs Cash P/L",
        [
            new MiniChartPoint("Tournaments", summary.TournamentValueProfitLoss, "MTTs", $"Tournament value P/L{Environment.NewLine}{Money(summary.TournamentValueProfitLoss)}{Environment.NewLine}Cash: {Money(summary.TournamentProfitLoss)}{Environment.NewLine}Tickets: {Money(summary.TicketBalance)}"),
            new MiniChartPoint("Cash", summary.CashProfitLoss, "Cash", $"Cash P/L{Environment.NewLine}{Money(summary.CashProfitLoss)}")
        ], MiniChartKind.Bars);
        _monthlyChart.SetData("Monthly Value P&L", monthly.Select(month => new MiniChartPoint(
            month.Month.ToString("yyyy-MM"),
            month.TotalValueProfitLoss,
            month.Month,
            $"Monthly Value P&L{Environment.NewLine}{month.Month:yyyy-MM}{Environment.NewLine}Value: {Money(month.TotalValueProfitLoss)}{Environment.NewLine}Cash P/L: {Money(month.TotalPokerProfitLoss)}{Environment.NewLine}Tickets: {Money(month.TicketProfitLoss)}{Environment.NewLine}Tournaments cash: {Money(month.TournamentProfitLoss)}{Environment.NewLine}Cash: {Money(month.CashProfitLoss)}{Environment.NewLine}MTTs: {month.NumberOfTournaments}  Cash sessions: {month.NumberOfCashSessions}")), MiniChartKind.Bars);
    }


    private Control BuildGuardrailRail()
    {
        var shell = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Panel,
            Padding = new Padding(6),
            Margin = new Padding(6, 8, 0, 8)
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Panel,
            Margin = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.Controls.Add(content);

        var heading = Theme.Label("Guardrails", Theme.SubHeaderFont, Theme.Text);
        heading.AutoSize = false;
        heading.Dock = DockStyle.Top;
        heading.Height = 28;
        heading.Margin = new Padding(8, 4, 8, 8);
        heading.TextAlign = ContentAlignment.MiddleLeft;
        content.Controls.Add(heading, 0, 0);

        var guardrails = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Panel,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        content.Controls.Add(guardrails, 0, 1);

        foreach (var title in new[]
        {
            "Risk status", "Suggested unit", "Max session risk", "Daily stop left",
            "Monthly stop left", "Daily risk left", "Exposure left", "Current drawdown"
        })
        {
            AddKpi(guardrails, title);
        }

        var toggle = Theme.Button("More stats");
        toggle.AutoSize = false;
        toggle.Dock = DockStyle.Top;
        toggle.Height = Theme.ButtonHeight;
        toggle.Margin = new Padding(8, 0, 8, 10);
        content.Controls.Add(toggle, 0, 2);

        var detailKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Panel,
            WrapContents = true,
            Visible = false
        };
        content.Controls.Add(detailKpis, 0, 3);

        foreach (var title in new[]
        {
            "On tables", "Total value P/L", "Total poker P/L", "Tournament value P/L", "Tournament P/L",
            "Cash P/L", "Today P/L", "This month P/L", "Total deposits", "Total withdrawals",
            "Best day", "Worst day", "Protect mode", "Bankroll tier"
        })
        {
            AddKpi(detailKpis, title);
        }

        var expanded = false;
        toggle.Click += (_, _) =>
        {
            expanded = !expanded;
            detailKpis.Visible = expanded;
            toggle.Text = expanded ? "Hide stats" : "More stats";
            shell.Parent?.PerformLayout();
        };

        return shell;
    }

    private static Control BuildOverviewPanel(
        string title,
        DataGridView grid,
        Action? editAction = null,
        string actionText = "Edit")
    {
        var shell = Theme.Card();
        shell.Dock = DockStyle.Fill;
        shell.Padding = new Padding(12);
        shell.Margin = new Padding(6);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Panel,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(layout);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = Theme.ButtonHeight,
            ColumnCount = editAction is null ? 1 : 2,
            RowCount = 1,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 10)
        };
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (editAction is not null)
        {
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        }

        var label = Theme.Label(title, Theme.SubHeaderFont, Theme.Text);
        label.Dock = DockStyle.Fill;
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = new Padding(0);
        header.Controls.Add(label, 0, 0);

        if (editAction is not null)
        {
            var edit = Theme.Button(actionText);
            edit.AutoSize = false;
            edit.Dock = DockStyle.Top;
            edit.Height = Theme.ButtonHeight;
            edit.Margin = new Padding(8, 0, 0, 0);
            edit.Click += (_, _) => editAction();
            header.Controls.Add(edit, 1, 0);
        }

        grid.Margin = new Padding(0);
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        return shell;
    }


    private void AddKpi(FlowLayoutPanel parent, string title)
    {
        var card = new KpiCard();
        card.SetData(title, "-", 0m);
        parent.Controls.Add(card);
        _kpiValues[title] = card;
    }

    private void SetKpi(string key, string text, decimal signValue)
    {
        if (!_kpiValues.TryGetValue(key, out var label))
        {
            return;
        }

        label.SetData(key, text, signValue);
    }

    private decimal DailyCommittedRisk(DateOnly today)
    {
        return _data.TournamentEntries
            .Where(entry => entry.Date == today)
            .Sum(entry => entry.CashCost)
            + _data.CashSessions
                .Where(entry => entry.Date == today)
                .Sum(entry => entry.SessionCost);
    }

    private decimal ActiveExposure()
    {
        return _data.TournamentEntries
            .Where(entry => entry.Status != TournamentStatus.Finished)
            .Sum(entry => entry.CashCost)
            + _data.CashSessions.Sum(entry => entry.ActiveTableCash);
    }

    private static decimal StopLossRemaining(decimal profitLoss, decimal limit)
    {
        return limit > 0m ? Math.Max(0m, limit + profitLoss) : 0m;
    }
}
