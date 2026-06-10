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

    private Control BuildCashTab()
    {
        var root = BuildGridShell(out var buttons);
        AddGridButton(buttons, "Start Cash", StartCash);
        AddGridButton(buttons, "Close Cash", CloseCash);
        AddGridButton(buttons, "Log Closed", AddCash);
        AddGridButton(buttons, "Edit", EditCash);
        _cashDetailsButton = AddGridButton(buttons, "Details", ToggleCashDetails);
        AddGridButton(buttons, "Delete", DeleteCash);

        _cashGrid = CreateGrid(_cashSource);
        _cashGrid.CellDoubleClick += (_, _) => EditCash();
        _cashGrid.SelectionChanged += (_, _) => UpdateCashInspector();
        AddTextColumn(_cashGrid, "Date", "Date", 92);
        AddTextColumn(_cashGrid, "SessionTime", "Time", 70);
        AddTextColumn(_cashGrid, "Status", "Status", 82);
        AddTextColumn(_cashGrid, "ClosedDate", "Closed Date", 104);
        AddTextColumn(_cashGrid, "ClosedTime", "Closed Time", 96);
        AddTextColumn(_cashGrid, "Platform", "Platform", 115);
        AddTextColumn(_cashGrid, "Format", "Format", 155);
        AddTextColumn(_cashGrid, "Game", "Game", 90);
        AddTextColumn(_cashGrid, "Stakes", "Stakes", 120);
        AddTextColumn(_cashGrid, "SmallBlindAmount", "SB", 72);
        AddTextColumn(_cashGrid, "BigBlindAmount", "BB", 72);
        AddTextColumn(_cashGrid, "StartStackBuyIn", "Buy-in", 82);
        AddTextColumn(_cashGrid, "Reloads", "Reloads", 82);
        AddTextColumn(_cashGrid, "ReloadCap", "Reload Cap", 92);
        AddTextColumn(_cashGrid, "ActiveTableCash", "On Table", 90);
        AddTextColumn(_cashGrid, "WalletCashImpact", "Wallet Move", 100);
        AddTextColumn(_cashGrid, "Cashout", "Cashout", 82);
        AddTextColumn(_cashGrid, "CashDropWon", "Cash Drop", 90);
        AddTextColumn(_cashGrid, "JackpotFortunePrizeWon", "Jackpot/Fortune", 125);
        AddTextColumn(_cashGrid, "Minutes", "Minutes", 78);
        AddTextColumn(_cashGrid, "Hands", "Hands", 72);
        AddTextColumn(_cashGrid, "SessionCost", "Cost", 82);
        AddTextColumn(_cashGrid, "NetProfit", "Net P/L", 86);
        AddTextColumn(_cashGrid, "BBWon", "BB Won", 82);
        AddTextColumn(_cashGrid, "BBPer100", "BB/100", 82);
        AddTextColumn(_cashGrid, "RiskPercentageOfBankrollAtSessionStart", "Risk %", 78);
        AddTextColumn(_cashGrid, "RuleCheckResult", "Rule", 96);
        AddTextColumn(_cashGrid, "BankrollBefore", "Bankroll Before", 115);
        AddTextColumn(_cashGrid, "BankrollAfter", "Bankroll After", 110);
        AddTextColumn(_cashGrid, "Notes", "Notes", 320);
        ApplyCashColumnMode();
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
        content.Controls.Add(_cashGrid, 0, 0);
        content.Controls.Add(BuildCashInspector(), 0, 1);
        root.Controls.Add(content, 0, 1);
        return root;
    }

    private Control BuildCashInspector()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            BackColor = Theme.Panel,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 6, 0, 0)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _cashInspectorTitle = BuildInspectorLabel(Theme.SubHeaderFont, Theme.Text);
        _cashInspectorResult = BuildInspectorLabel(Theme.BodyFont, Theme.Text);
        _cashInspectorMeta = BuildInspectorLabel(Theme.BodyFont, Theme.Muted);
        _cashInspectorNotes = BuildInspectorLabel(Theme.SmallFont, Theme.Muted);

        shell.Controls.Add(_cashInspectorTitle, 0, 0);
        shell.Controls.Add(_cashInspectorResult, 0, 1);
        shell.Controls.Add(_cashInspectorMeta, 0, 2);
        shell.Controls.Add(_cashInspectorNotes, 0, 3);
        UpdateCashInspector();
        return shell;
    }

    private void ToggleCashDetails()
    {
        _cashDetailColumnsVisible = !_cashDetailColumnsVisible;
        ApplyCashColumnMode();
        _statusLabel.Text = _cashDetailColumnsVisible
            ? "Cash details shown."
            : "Cash compact view shown.";
    }

    private void ApplyCashColumnMode()
    {
        if (_cashGrid is null)
        {
            return;
        }

        foreach (DataGridViewColumn column in _cashGrid.Columns)
        {
            column.Visible = _cashDetailColumnsVisible
                || CompactCashColumns.Contains(column.DataPropertyName);
        }

        if (_cashDetailsButton is not null)
        {
            _cashDetailsButton.Text = _cashDetailColumnsVisible ? "Compact" : "Details";
        }
    }

    private void UpdateCashInspector()
    {
        if (_cashInspectorTitle is null
            || _cashInspectorResult is null
            || _cashInspectorMeta is null
            || _cashInspectorNotes is null)
        {
            return;
        }

        if (Selected<CashSession>(_cashSource) is not { } entry)
        {
            _cashInspectorTitle.Text = "No cash session selected";
            _cashInspectorResult.Text = string.Empty;
            _cashInspectorMeta.Text = string.Empty;
            _cashInspectorNotes.Text = string.Empty;
            return;
        }

        var game = string.IsNullOrWhiteSpace(entry.Game) ? "Cash" : entry.Game.Trim();
        var stakes = string.IsNullOrWhiteSpace(entry.Stakes) ? "No stakes" : entry.Stakes.Trim();
        _cashInspectorTitle.Text = $"{entry.Platform} | {entry.Format} | {game} | {stakes} | {entry.Status}";

        if (entry.IsActive)
        {
            _cashInspectorResult.Text =
                $"On table {Money(entry.ActiveTableCash)}  Wallet move {Money(entry.WalletCashImpact)}  Buy-in {Money(entry.StartStackBuyIn)}  Reloads {Money(entry.Reloads)}  Cap {Money(entry.ReloadCap)}";
            _cashInspectorResult.ForeColor = Theme.Warning;
        }
        else
        {
            _cashInspectorResult.Text =
                $"Net {Money(entry.NetProfit)}  Cashout {Money(entry.Cashout)}  Extras {Money(entry.CashDropWon + entry.JackpotFortunePrizeWon)}  Cost {Money(entry.SessionCost)}  BB {entry.BBWon:0.##}  BB/100 {entry.BBPer100:0.##}";
            _cashInspectorResult.ForeColor = entry.NetProfit >= 0m ? Theme.Positive : Theme.Negative;
        }

        var closed = entry.ClosedDate is null
            ? "-"
            : $"{entry.ClosedDate:yyyy-MM-dd} {FormatTime(entry.ClosedTime)}";
        _cashInspectorMeta.Text =
            $"Started {entry.Date:yyyy-MM-dd} {FormatTime(entry.SessionTime)}  Closed {closed}  SB/BB {Money(entry.SmallBlindAmount)}/{Money(entry.BigBlindAmount)}  Minutes {NullableText(entry.Minutes)}  Hands {NullableText(entry.Hands)}  Risk {entry.RiskPercentageOfBankrollAtSessionStart:0.0}%  Bankroll {Money(entry.BankrollBefore)} -> {Money(entry.BankrollAfter)}";
        _cashInspectorNotes.Text = CashInspectorNotes(entry);
    }

    private static string FormatTime(TimeOnly? time)
    {
        return time?.ToString("HH:mm", CultureInfo.CurrentCulture) ?? "--:--";
    }

    private static string CashInspectorNotes(CashSession entry)
    {
        var parts = new[] { entry.RuleCheckResult, entry.Notes }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return parts.Count == 0 ? "No notes recorded." : string.Join(" | ", parts);
    }


    private void AddCash()
    {
        var entry = CashSessionWorkflowService.CreateClosedDraft(DateTime.Now, _data.Settings.DefaultPlatform);

        using var dialog = new CashSessionDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.CashSessions.Add(dialog.Entry);
        SaveData("Cash session added.");
    }

    private void StartCash()
    {
        var entry = CashSessionWorkflowService.CreateActiveDraft(DateTime.Now, _data.Settings.DefaultPlatform);

        using var dialog = new CashSessionStartDialog(entry);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.CashSessions.Add(dialog.Entry);
        SaveData("Cash session started.");
    }

    private void CloseCash()
    {
        if (Selected<CashSession>(_cashSource) is not { } selected)
        {
            return;
        }

        CloseCashSession(selected);
    }

    private void CloseCashSession(CashSession selected)
    {
        if (!selected.IsActive)
        {
            MessageBox.Show(
                "Select an active cash session to close.",
                "Close Cash Session",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new CashSessionCloseDialog(selected, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopyCash(dialog.Entry, _data.CashSessions.First(entry => entry.Id == selected.Id));
        SaveData("Cash session closed.");
    }

    private void EditCash()
    {
        if (Selected<CashSession>(_cashSource) is not { } selected)
        {
            return;
        }

        EditCashSession(selected);
    }

    private void EditCashSession(CashSession selected)
    {
        using var dialog = new CashSessionDialog(selected, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopyCash(dialog.Entry, _data.CashSessions.First(entry => entry.Id == selected.Id));
        SaveData("Cash session updated.");
    }

    private void DeleteCash()
    {
        if (Selected<CashSession>(_cashSource) is not { } selected || !ConfirmDelete("cash session"))
        {
            return;
        }

        _data.CashSessions.RemoveAll(entry => entry.Id == selected.Id);
        SaveData("Cash session deleted.");
    }

    private static void CopyCash(CashSession source, CashSession target)
    {
        target.Date = source.Date;
        target.SessionTime = source.SessionTime;
        target.Status = source.Status;
        target.ClosedDate = source.ClosedDate;
        target.ClosedTime = source.ClosedTime;
        target.Platform = source.Platform;
        target.Format = source.Format;
        target.Game = source.Game;
        target.Stakes = source.Stakes;
        target.SmallBlindAmount = source.SmallBlindAmount;
        target.BigBlindAmount = source.BigBlindAmount;
        target.StartStackBuyIn = source.StartStackBuyIn;
        target.Reloads = source.Reloads;
        target.ReloadCap = source.ReloadCap;
        target.Cashout = source.Cashout;
        target.CashDropWon = source.CashDropWon;
        target.JackpotFortunePrizeWon = source.JackpotFortunePrizeWon;
        target.Minutes = source.Minutes;
        target.Hands = source.Hands;
        target.BankrollBefore = source.BankrollBefore;
        target.BankrollAfter = source.BankrollAfter;
        target.Notes = source.Notes;
    }
}
