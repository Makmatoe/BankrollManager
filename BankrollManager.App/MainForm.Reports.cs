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

    private Control BuildTimelineTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            BackColor = Theme.Back,
            Padding = new Padding(8)
        };

        _timelineGrid = CreateGrid(_timelineSource);
        AddTextColumn(_timelineGrid, "Date", "Date", 100);
        AddTextColumn(_timelineGrid, "Time", "Time", 70);
        AddTextColumn(_timelineGrid, "Type", "Type", 105);
        AddTextColumn(_timelineGrid, "Name", "Name", 240);
        AddTextColumn(_timelineGrid, "CostRisk", "Cost/Risk", 105);
        AddTextColumn(_timelineGrid, "Result", "Result", 105);
        AddTextColumn(_timelineGrid, "BankrollBefore", "Bankroll Before", 125);
        AddTextColumn(_timelineGrid, "BankrollAfter", "Bankroll After", 125);
        AddTextColumn(_timelineGrid, "Rule", "Rule", 110);
        root.Controls.Add(_timelineGrid, 0, 0);
        return root;
    }

    private Control BuildLedgerTab()
    {
        var root = BuildGridShell(out var buttons);
        AddGridButton(buttons, "Add", AddLedger);
        AddGridButton(buttons, "Edit", EditLedger);
        AddGridButton(buttons, "Delete", DeleteLedger);

        _ledgerGrid = CreateGrid(_ledgerSource);
        _ledgerGrid.CellDoubleClick += (_, _) => EditLedger();
        AddTextColumn(_ledgerGrid, "Date", "Date", 92);
        AddTextColumn(_ledgerGrid, "Type", "Type", 110);
        AddTextColumn(_ledgerGrid, "Platform", "Platform", 120);
        AddTextColumn(_ledgerGrid, "Description", "Description", 260);
        AddTextColumn(_ledgerGrid, "Amount", "Amount", 90);
        AddTextColumn(_ledgerGrid, "Category", "Category", 120);
        AddTextColumn(_ledgerGrid, "BankrollBefore", "Bankroll Before", 115);
        AddTextColumn(_ledgerGrid, "BankrollAfter", "Bankroll After", 110);
        AddTextColumn(_ledgerGrid, "Notes", "Notes", 320);
        root.Controls.Add(BuildGridWithEmptyState(
            _ledgerGrid,
            out _ledgerEmptyState,
            "No ledger entries yet. Add a deposit, withdrawal, bonus, rakeback, or correction here."), 0, 1);
        return root;
    }

    private Control BuildDailyTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Theme.Back, Padding = new Padding(8) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _dailyReviewChart = new MiniChart { Dock = DockStyle.Fill, Margin = new Padding(6) };
        _dailyReviewChart.PointActivated += (_, e) => OpenDailyChartPoint(e.Point);
        root.Controls.Add(_dailyReviewChart, 0, 0);

        _dailyGrid = CreateGrid(_dailySource);
        AddTextColumn(_dailyGrid, "Date", "Date", 100);
        AddTextColumn(_dailyGrid, "TournamentProfitLoss", "Tournament P/L", 130);
        AddTextColumn(_dailyGrid, "CashProfitLoss", "Cash P/L", 110);
        AddTextColumn(_dailyGrid, "TotalProfitLoss", "Total P/L", 110);
        AddTextColumn(_dailyGrid, "NumberOfSessions", "Sessions", 88);
        AddTextColumn(_dailyGrid, "RunningMonthProfitLoss", "Running Month P/L", 150);
        AddTextColumn(_dailyGrid, "RunningLifetimeBankroll", "Running Bankroll", 150);
        root.Controls.Add(_dailyGrid, 0, 1);
        return root;
    }

    private Control BuildMonthlyTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, BackColor = Theme.Back, Padding = new Padding(8) };
        _monthlyGrid = CreateGrid(_monthlySource);
        AddTextColumn(_monthlyGrid, "Month", "Month", 100);
        AddTextColumn(_monthlyGrid, "Deposits", "Deposits", 100);
        AddTextColumn(_monthlyGrid, "Withdrawals", "Withdrawals", 110);
        AddTextColumn(_monthlyGrid, "TournamentProfitLoss", "Tournament P/L", 130);
        AddTextColumn(_monthlyGrid, "CashProfitLoss", "Cash P/L", 110);
        AddTextColumn(_monthlyGrid, "TotalPokerProfitLoss", "Poker P/L", 110);
        AddTextColumn(_monthlyGrid, "NumberOfTournaments", "Tournaments", 105);
        AddTextColumn(_monthlyGrid, "NumberOfCashSessions", "Cash Sessions", 115);
        AddTextColumn(_monthlyGrid, "AverageTournamentBuyIn", "Avg Buy-in", 110);
        AddTextColumn(_monthlyGrid, "BiggestWin", "Biggest Win", 110);
        AddTextColumn(_monthlyGrid, "BiggestLoss", "Biggest Loss", 110);
        AddTextColumn(_monthlyGrid, "StopLossBreaches", "Stop-loss Breaches", 150);
        AddTextColumn(_monthlyGrid, "Notes", "Notes", 240);
        root.Controls.Add(_monthlyGrid, 0, 0);
        return root;
    }

    private Control BuildYearlyTab()
    {
        _yearlyGrid = CreateComparisonGrid(_yearlySource, yearly: true);
        _platformGrid = CreatePlatformGrid(_platformSource);
        _formatGrid = CreateComparisonGrid(_formatSource);
        _categoryGrid = CreateComparisonGrid(_categorySource);

        return BuildSegmentedContent(
        [
            ("Yearly", _yearlyGrid),
            ("Platforms", _platformGrid),
            ("Formats", _formatGrid),
            ("Categories", _categoryGrid)
        ]);
    }

    private static Control BuildSegmentedContent(IReadOnlyList<(string Title, Control Content)> pages)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Back,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 10),
            Margin = new Padding(0)
        };
        root.Controls.Add(navigation, 0, 0);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Back,
            Padding = new Padding(0)
        };
        root.Controls.Add(contentHost, 0, 1);

        var buttons = new List<Label>();
        for (var index = 0; index < pages.Count; index++)
        {
            var pageIndex = index;
            var button = BuildSegmentButton(pages[index].Title);
            button.Click += (_, _) => SelectSegmentPage(contentHost, pages, buttons, pageIndex);
            navigation.Controls.Add(button);
            buttons.Add(button);
        }

        SelectSegmentPage(contentHost, pages, buttons, 0);
        return root;
    }

    private static Label BuildSegmentButton(string title)
    {
        var measuredWidth = TextRenderer.MeasureText(title, Theme.BodyFont).Width + 30;
        var button = new Label
        {
            Text = title,
            AutoSize = false,
            AutoEllipsis = true,
            Width = Math.Max(96, measuredWidth),
            Height = 38,
            BackColor = Theme.Panel,
            Cursor = Cursors.Hand,
            Font = Theme.BodyFont,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 0, 10, 1),
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
        };

        button.Paint += (_, e) =>
        {
            using var border = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(border, 0, 0, button.Width - 1, button.Height - 1);

            if (button.Tag is not true)
            {
                return;
            }

            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, 8, button.Height - 4, Math.Max(8, button.Width - 16), 3);
        };
        button.MouseEnter += (_, _) =>
        {
            if (button.Tag is not true)
            {
                button.BackColor = Theme.PanelAlt;
                button.ForeColor = Theme.Text;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Tag is not true)
            {
                button.BackColor = Theme.Panel;
                button.ForeColor = Theme.Muted;
            }
        };

        return button;
    }

    private static void SelectSegmentPage(
        Panel contentHost,
        IReadOnlyList<(string Title, Control Content)> pages,
        IReadOnlyList<Label> buttons,
        int selectedIndex)
    {
        contentHost.SuspendLayout();
        contentHost.Controls.Clear();
        var content = pages[selectedIndex].Content;
        content.Dock = DockStyle.Fill;
        contentHost.Controls.Add(content);
        contentHost.ResumeLayout();

        for (var index = 0; index < buttons.Count; index++)
        {
            var selected = index == selectedIndex;
            buttons[index].Tag = selected;
            buttons[index].BackColor = selected ? Theme.PanelAlt : Theme.Panel;
            buttons[index].ForeColor = selected ? Theme.Text : Theme.Muted;
            buttons[index].Invalidate();
        }
    }


    private void AddLedger()
    {
        var entry = new LedgerEntry
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Platform = _data.Settings.DefaultPlatform
        };

        using var dialog = new LedgerEntryDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.LedgerEntries.Add(dialog.Entry);
        SaveData("Ledger entry added.");
    }

    private void EditLedger()
    {
        if (Selected<LedgerEntry>(_ledgerSource) is not { } selected)
        {
            return;
        }

        using var dialog = new LedgerEntryDialog(selected, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopyLedger(dialog.Entry, _data.LedgerEntries.First(entry => entry.Id == selected.Id));
        SaveData("Ledger entry updated.");
    }

    private void DeleteLedger()
    {
        if (Selected<LedgerEntry>(_ledgerSource) is not { } selected || !ConfirmDelete("ledger entry"))
        {
            return;
        }

        _data.LedgerEntries.RemoveAll(entry => entry.Id == selected.Id);
        SaveData("Ledger entry deleted.");
    }

    private static void CopyLedger(LedgerEntry source, LedgerEntry target)
    {
        target.Date = source.Date;
        target.Type = source.Type;
        target.Platform = source.Platform;
        target.Description = source.Description;
        target.Amount = source.Amount;
        target.Category = source.Category;
        target.BankrollBefore = source.BankrollBefore;
        target.BankrollAfter = source.BankrollAfter;
        target.Notes = source.Notes;
    }
}
