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
    private Control BuildTournamentEvTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var formScroller = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        root.Controls.Add(formScroller, 0, 0);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Back
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        formScroller.Controls.Add(form);

        AddTournamentEvTitleRow(form, "Tournament EV Checker");

        _tournamentEvName = Theme.TextBox();
        _tournamentEvBuyIn = Theme.MoneyBox(0.04m);
        _tournamentEvBuyIn.Minimum = 0.01m;
        _tournamentEvPrizeType = BuildTournamentEvPrizeTypeBox();
        _tournamentEvNumberOfTickets = Theme.IntBox(5);
        _tournamentEvTicketValue = Theme.MoneyBox(0.40m);
        _tournamentEvTicketValue.Minimum = 0m;
        _tournamentEvManualPrizeValue = Theme.MoneyBox(2.00m);
        _tournamentEvManualPrizeValue.Minimum = 0m;
        _tournamentEvCurrentEntries = Theme.IntBox(1);
        _tournamentEvCurrentEntries.Minimum = 1m;
        _tournamentEvTicketDiscount = PercentBox(100m);

        AddDecisionRow(form, "Tournament name", _tournamentEvName);
        AddDecisionRow(form, "Buy-in, including fee", _tournamentEvBuyIn);
        AddDecisionRow(form, "Prize type", _tournamentEvPrizeType);
        AddDecisionRow(form, "Number of tickets", _tournamentEvNumberOfTickets);
        AddDecisionRow(form, "Ticket value", _tournamentEvTicketValue);
        AddDecisionRow(form, "Manual total prize value", _tournamentEvManualPrizeValue);
        AddDecisionRow(form, "Current players/entries", _tournamentEvCurrentEntries);
        AddTournamentEvHelpRow(form, "For re-entry tournaments, use total entries/buy-ins if available.");
        AddDecisionRow(form, "Ticket value discount %", _tournamentEvTicketDiscount);

        foreach (Control control in new Control[]
        {
            _tournamentEvName,
            _tournamentEvBuyIn,
            _tournamentEvPrizeType,
            _tournamentEvNumberOfTickets,
            _tournamentEvTicketValue,
            _tournamentEvManualPrizeValue,
            _tournamentEvCurrentEntries,
            _tournamentEvTicketDiscount
        })
        {
            switch (control)
            {
                case NumericUpDown numeric:
                    numeric.ValueChanged += (_, _) => RefreshTournamentEv();
                    break;
                case ComboBox combo:
                    combo.SelectedIndexChanged += (_, _) => RefreshTournamentEv();
                    break;
                case TextBox textBox:
                    textBox.TextChanged += (_, _) => RefreshTournamentEv();
                    break;
            }
        }

        var result = Theme.Card();
        result.Dock = DockStyle.Fill;
        result.Margin = new Padding(12, 0, 0, 0);
        var resultLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Theme.Panel
        };
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        result.Controls.Add(resultLayout);
        root.Controls.Add(result, 1, 0);

        _tournamentEvStatusLabel = BuildTournamentEvStatusLabel();
        var statusRow = resultLayout.RowCount++;
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        resultLayout.Controls.Add(_tournamentEvStatusLabel, 0, statusRow);
        resultLayout.SetColumnSpan(_tournamentEvStatusLabel, 2);

        _tournamentEvPrizeValueLabel = AddTournamentEvResultRow(resultLayout, "Total prize value");
        _tournamentEvGrossLabel = AddTournamentEvResultRow(resultLayout, "Current gross EV");
        _tournamentEvNetLabel = AddTournamentEvResultRow(resultLayout, "Current net EV");
        _tournamentEvRoiLabel = AddTournamentEvResultRow(resultLayout, "ROI %");
        _tournamentEvBreakevenLabel = AddTournamentEvResultRow(resultLayout, "Exact breakeven entry count");
        _tournamentEvPositiveUntilLabel = AddTournamentEvResultRow(resultLayout, "Positive until");
        _tournamentEvNegativeFromLabel = AddTournamentEvResultRow(resultLayout, "Negative EV from");

        RefreshTournamentEv();
        return root;
    }

    private void RefreshTournamentEv()
    {
        if (_tournamentEvStatusLabel is null)
        {
            return;
        }

        var request = BuildTournamentEvRequest();
        var ticketsMode = request.PrizeType == TournamentEvPrizeType.Tickets;
        _tournamentEvNumberOfTickets.Enabled = ticketsMode;
        _tournamentEvTicketValue.Enabled = ticketsMode;
        _tournamentEvTicketDiscount.Enabled = ticketsMode;
        _tournamentEvManualPrizeValue.Enabled = !ticketsMode;

        var result = TournamentEvCalculator.Evaluate(request);
        _tournamentEvStatusLabel.Text = TournamentEvStatusText(result.Status);
        _tournamentEvStatusLabel.ForeColor = TournamentEvStatusForeColor(result.Status);
        _tournamentEvStatusLabel.BackColor = TournamentEvStatusBackColor(result.Status);
        _tournamentEvPrizeValueLabel.Text = Money(result.TotalPrizeValue);
        _tournamentEvGrossLabel.Text = Money(result.GrossEv);
        _tournamentEvNetLabel.Text = Money(result.NetEv);
        _tournamentEvNetLabel.ForeColor = SignColor(result.NetEv);
        _tournamentEvRoiLabel.Text = result.Roi.ToString("P1", CultureInfo.CurrentCulture);
        _tournamentEvRoiLabel.ForeColor = SignColor(result.Roi);
        _tournamentEvBreakevenLabel.Text = FormatEntryCount(result.ExactBreakEvenEntries);
        _tournamentEvPositiveUntilLabel.Text = FormatEntries(result.MaxPositiveEntries);
        _tournamentEvNegativeFromLabel.Text = FormatEntries(result.NegativeEvStartsAt);
    }

    private TournamentEvRequest BuildTournamentEvRequest()
    {
        var selectedPrizeType = _tournamentEvPrizeType.SelectedItem is TournamentEvPrizeTypeOption option
            ? option.PrizeType
            : TournamentEvPrizeType.Tickets;

        return new TournamentEvRequest
        {
            TournamentName = _tournamentEvName.Text.Trim(),
            BuyIn = _tournamentEvBuyIn.Value,
            PrizeType = selectedPrizeType,
            NumberOfTickets = (int)_tournamentEvNumberOfTickets.Value,
            TicketValue = _tournamentEvTicketValue.Value,
            ManualPrizeValue = _tournamentEvManualPrizeValue.Value,
            CurrentEntries = (int)_tournamentEvCurrentEntries.Value,
            TicketValueDiscountPercent = _tournamentEvTicketDiscount.Value
        };
    }

    private static ComboBox BuildTournamentEvPrizeTypeBox()
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyFont,
            Width = 190,
            Height = Theme.ControlHeight
        };
        box.Items.Add(new TournamentEvPrizeTypeOption(TournamentEvPrizeType.Tickets, "Tickets"));
        box.Items.Add(new TournamentEvPrizeTypeOption(TournamentEvPrizeType.CashPrizePool, "Cash Prize Pool"));
        box.SelectedIndex = 0;
        return box;
    }

    private static void AddTournamentEvTitleRow(TableLayoutPanel layout, string text)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        var label = Theme.Label(text, Theme.HeaderFont, Theme.Text);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 0, 4, 8);
        label.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 2);
    }

    private static void AddTournamentEvHelpRow(TableLayoutPanel layout, string text)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        var label = Theme.Label(text, Theme.SmallFont, Theme.Warning);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(4, 0, 4, 6);
        label.Padding = new Padding(0, 2, 0, 0);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.UseMnemonic = false;
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 2);
    }

    private static Label BuildTournamentEvStatusLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Panel,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(8, 0, 8, 2),
            UseMnemonic = false
        };
    }

    private static Label AddTournamentEvResultRow(TableLayoutPanel layout, string labelText)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var label = Theme.Label(labelText, Theme.BodyFont, Theme.Muted);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0);
        label.Padding = new Padding(0, 0, 12, 0);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.UseMnemonic = false;

        var value = Theme.Label("-", Theme.SubHeaderFont, Theme.Text);
        value.AutoSize = false;
        value.Dock = DockStyle.Fill;
        value.Margin = new Padding(0);
        value.TextAlign = ContentAlignment.MiddleRight;
        value.UseMnemonic = false;

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(value, 1, row);
        return value;
    }

    private static string TournamentEvStatusText(TournamentEvStatus status)
    {
        return status switch
        {
            TournamentEvStatus.Positive => "+EV",
            TournamentEvStatus.Breakeven => "breakeven",
            TournamentEvStatus.Negative => "-EV",
            _ => string.Empty
        };
    }

    private static Color TournamentEvStatusBackColor(TournamentEvStatus status)
    {
        return status switch
        {
            TournamentEvStatus.Positive => Theme.PositiveSurface,
            TournamentEvStatus.Breakeven => Theme.WarningSurface,
            TournamentEvStatus.Negative => Theme.NegativeSurface,
            _ => Theme.Panel
        };
    }

    private static Color TournamentEvStatusForeColor(TournamentEvStatus status)
    {
        return status switch
        {
            TournamentEvStatus.Positive => Theme.Positive,
            TournamentEvStatus.Breakeven => Theme.Warning,
            TournamentEvStatus.Negative => Theme.Negative,
            _ => Theme.Text
        };
    }

    private static Color SignColor(decimal value)
    {
        return value switch
        {
            > 0m => Theme.Positive,
            < 0m => Theme.Negative,
            _ => Theme.Text
        };
    }

    private static string FormatEntryCount(decimal value)
    {
        var format = value == decimal.Truncate(value) ? "0" : "0.##";
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private static string FormatEntries(long count)
    {
        return count == 1
            ? "1 entry"
            : $"{count.ToString(CultureInfo.CurrentCulture)} entries";
    }

    private sealed record TournamentEvPrizeTypeOption(TournamentEvPrizeType PrizeType, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
