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
        const int minimumDashboardHeight = 800;
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

        var statsColumn = new ColumnStyle(SizeType.Absolute, 112);
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.ColumnStyles.Add(statsColumn);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            BackColor = Theme.Back,
            Padding = new Padding(8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 224));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(root, 0, 0);
        shell.Controls.Add(BuildStatsRail(statsColumn), 1, 0);

        _stopLossBanner = Theme.Label(string.Empty, Theme.HeaderFont, Theme.Text);
        _stopLossBanner.AutoSize = false;
        _stopLossBanner.AutoEllipsis = true;
        _stopLossBanner.Dock = DockStyle.Fill;
        _stopLossBanner.TextAlign = ContentAlignment.MiddleLeft;
        _stopLossBanner.Padding = new Padding(18, 8, 18, 8);
        _stopLossBanner.BackColor = Theme.Panel;
        _stopLossBanner.BorderStyle = BorderStyle.FixedSingle;
        root.Controls.Add(_stopLossBanner, 0, 0);

        var kpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back,
            WrapContents = true
        };
        root.Controls.Add(kpis, 0, 1);

        foreach (var title in new[]
        {
            "Overall value", "Cash bankroll", "Today value P/L", "This month value P/L", "Tickets available", "Stop-loss status"
        })
        {
            AddKpi(kpis, title);
        }

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
        root.Controls.Add(overview, 0, 2);

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
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Theme.Back
        };
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        charts.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        charts.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.Controls.Add(charts, 0, 3);

        _dailyChart = new MiniChart { Dock = DockStyle.Fill };
        _runningChart = new MiniChart { Dock = DockStyle.Fill };
        _comparisonChart = new MiniChart { Dock = DockStyle.Fill };
        _monthlyChart = new MiniChart { Dock = DockStyle.Fill };
        _dailyChart.PointActivated += (_, e) => OpenDailyChartPoint(e.Point);
        _runningChart.PointActivated += (_, e) => OpenRunningChartPoint(e.Point);
        _comparisonChart.PointActivated += (_, e) => OpenComparisonChartPoint(e.Point);
        _monthlyChart.PointActivated += (_, e) => OpenMonthlyChartPoint(e.Point);
        charts.Controls.Add(_dailyChart, 0, 0);
        charts.Controls.Add(_runningChart, 1, 0);
        charts.Controls.Add(_comparisonChart, 0, 1);
        charts.Controls.Add(_monthlyChart, 1, 1);

        return viewport;
    }


    private void RefreshDashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var summary = BankrollCalculator.GetDashboardSummary(_data, today);
        SetKpi("Overall value", Money(summary.CurrentBankrollValue), summary.CurrentBankrollValue);
        SetKpi("Cash bankroll", Money(summary.CurrentBankroll), summary.CurrentBankroll);
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

        var daily = BankrollCalculator.GetDailySummaries(_data).OrderBy(summary => summary.Date).ToList();
        var running = BankrollCalculator.GetRunningBankroll(_data).OrderBy(point => point.Date).ToList();
        var monthly = BankrollCalculator.GetMonthlySummaries(_data).OrderBy(summary => summary.Month).ToList();
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


    private Control BuildStatsRail(ColumnStyle statsColumn)
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Panel,
            Padding = new Padding(6),
            Margin = new Padding(6, 8, 0, 8)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toggle = Theme.Button("Stats");
        toggle.AutoSize = false;
        toggle.Dock = DockStyle.Top;
        toggle.Height = Theme.ButtonHeight;
        toggle.Margin = new Padding(0, 0, 0, 10);
        shell.Controls.Add(toggle, 0, 0);

        var detailKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Panel,
            WrapContents = true,
            Visible = false
        };
        shell.Controls.Add(detailKpis, 0, 1);

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
            statsColumn.Width = expanded ? 360 : 112;
            detailKpis.Visible = expanded;
            toggle.Text = expanded ? "Hide Stats" : "Stats";
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
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Panel,
            Padding = new Padding(10),
            Margin = new Padding(6)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
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
        shell.Controls.Add(header, 0, 0);
        shell.Controls.Add(grid, 0, 1);
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
}
