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
        const int portraitBreakpoint = 1180;
        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Back,
            Padding = new Padding(8)
        };
        viewport.Controls.Add(root);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

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
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        formScroller.Controls.Add(form);

        AddTournamentEvTitleRow(form, "Tournament EV Checker");

        _tournamentEvName = Theme.TextBox();
        _tournamentEvBuyIn = Theme.MoneyBox(0.04m);
        _tournamentEvBuyIn.Minimum = 0.01m;
        _tournamentEvPrizeType = BuildTournamentEvPrizeTypeBox();
        _tournamentEvTournamentType = BuildTournamentEvTournamentTypeBox();
        _tournamentEvNumberOfTickets = Theme.IntBox(5);
        _tournamentEvTicketValue = Theme.MoneyBox(0.40m);
        _tournamentEvTicketValue.Minimum = 0m;
        _tournamentEvManualPrizeValue = Theme.MoneyBox(2.00m);
        _tournamentEvManualPrizeValue.Minimum = 0m;
        _tournamentEvCurrentEntries = Theme.IntBox(50);
        _tournamentEvCurrentEntries.Minimum = 1m;
        _tournamentEvTotalEntries = Theme.IntBox(50);
        _tournamentEvTotalEntries.Minimum = 1m;
        _tournamentEvPaidPlaces = Theme.IntBox(5);
        _tournamentEvTicketDiscount = PercentBox(100m);
        _tournamentEvSampleSize = Theme.IntBox(100, 100_000);
        _tournamentEvSampleSize.Minimum = 1m;
        _tournamentEvBankrollSize = Theme.MoneyBox(0m);
        _tournamentEvBankrollSize.Minimum = 0m;
        _tournamentEvPayoutStructure = Theme.TextBox(multiline: true);
        _tournamentEvPayoutStructure.Height = 84;

        AddTournamentEvInputRow(form, "Tournament", _tournamentEvName);
        AddTournamentEvInputRow(form, "Buy-in", _tournamentEvBuyIn);
        AddTournamentEvInputRow(form, "Type", _tournamentEvTournamentType);
        AddTournamentEvInputRow(form, "Prize type", _tournamentEvPrizeType);
        AddTournamentEvInputRow(form, "Tickets paid", _tournamentEvNumberOfTickets);
        AddTournamentEvInputRow(form, "Ticket value", _tournamentEvTicketValue);
        AddTournamentEvInputRow(form, "Cash prize pool", _tournamentEvManualPrizeValue);
        AddTournamentEvInputRow(form, "Entries now", _tournamentEvCurrentEntries);
        AddTournamentEvInputRow(form, "Total entries", _tournamentEvTotalEntries);
        AddTournamentEvInputRow(form, "Paid places", _tournamentEvPaidPlaces);
        AddTournamentEvHelpRow(form, "For re-entry tournaments, use total entries/buy-ins if available.");
        AddTournamentEvInputRow(form, "Ticket value %", _tournamentEvTicketDiscount);
        AddTournamentEvInputRow(form, "Sample size", _tournamentEvSampleSize);
        AddTournamentEvInputRow(form, "Bankroll", _tournamentEvBankrollSize);
        AddTournamentEvTallInputRow(form, "Payouts", _tournamentEvPayoutStructure, 88);
        AddTournamentEvHelpRow(form, "+EV does not mean guaranteed short-term profit. Higher field size and top-heavy payouts increase variance and can create long downswings even when the tournament is profitable.");

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        _tournamentEvCheckButton = Theme.Button("Check EV");
        _tournamentEvCheckButton.AutoSize = false;
        _tournamentEvCheckButton.BackColor = Theme.CommandPrimary;
        _tournamentEvCheckButton.Height = 32;
        _tournamentEvCheckButton.Width = 96;
        _tournamentEvCheckButton.Click += (_, _) => RefreshTournamentEv();
        actions.Controls.Add(_tournamentEvCheckButton);
        AddTournamentEvInputRow(form, string.Empty, actions);

        foreach (Control control in new Control[]
        {
            _tournamentEvName,
            _tournamentEvBuyIn,
            _tournamentEvPrizeType,
            _tournamentEvTournamentType,
            _tournamentEvNumberOfTickets,
            _tournamentEvTicketValue,
            _tournamentEvManualPrizeValue,
            _tournamentEvCurrentEntries,
            _tournamentEvTotalEntries,
            _tournamentEvPaidPlaces,
            _tournamentEvTicketDiscount,
            _tournamentEvSampleSize,
            _tournamentEvBankrollSize,
            _tournamentEvPayoutStructure
        })
        {
            switch (control)
            {
                case NumericUpDown numeric:
                    numeric.ValueChanged += (_, _) => TournamentEvInputChanged();
                    break;
                case ComboBox combo:
                    combo.SelectedIndexChanged += (_, _) => TournamentEvInputChanged();
                    break;
                case TextBox textBox:
                    textBox.TextChanged += (_, _) => TournamentEvInputChanged();
                    break;
            }
        }

        var result = Theme.Card();
        result.Dock = DockStyle.Top;
        result.Height = 338;
        result.Margin = new Padding(8, 0, 0, 0);
        var resultLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Panel
        };
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 174));
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        result.Controls.Add(resultLayout);
        root.Controls.Add(result, 1, 0);

        _tournamentEvStatusLabel = BuildTournamentEvStatusLabel();
        var statusRow = resultLayout.RowCount++;
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        resultLayout.Controls.Add(_tournamentEvStatusLabel, 0, statusRow);
        resultLayout.SetColumnSpan(_tournamentEvStatusLabel, 2);

        _tournamentEvPrizeValueLabel = AddTournamentEvResultRow(resultLayout, "Total pool");
        _tournamentEvMaxPrizeLabel = AddTournamentEvResultRow(resultLayout, "Max single prize");
        _tournamentEvUncappedGrossLabel = AddTournamentEvResultRow(resultLayout, "Pool EV / entry");
        _tournamentEvGrossLabel = AddTournamentEvResultRow(resultLayout, "Gross EV capped");
        _tournamentEvNetLabel = AddTournamentEvResultRow(resultLayout, "Net EV capped");
        _tournamentEvRoiLabel = AddTournamentEvResultRow(resultLayout, "ROI %");
        _tournamentEvBreakevenLabel = AddTournamentEvResultRow(resultLayout, "Breakeven entries");
        _tournamentEvPositiveUntilLabel = AddTournamentEvResultRow(resultLayout, "Positive until");
        _tournamentEvNegativeFromLabel = AddTournamentEvResultRow(resultLayout, "Negative EV from");

        var variance = Theme.Card();
        variance.Dock = DockStyle.Top;
        variance.Height = 366;
        variance.Margin = new Padding(8, 0, 0, 0);
        var varianceLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Panel
        };
        varianceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        varianceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        variance.Controls.Add(varianceLayout);
        root.Controls.Add(variance, 2, 0);

        _tournamentEvVarianceRatingLabel = BuildTournamentEvStatusLabel();
        var varianceStatusRow = varianceLayout.RowCount++;
        varianceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        varianceLayout.Controls.Add(_tournamentEvVarianceRatingLabel, 0, varianceStatusRow);
        varianceLayout.SetColumnSpan(_tournamentEvVarianceRatingLabel, 2);

        _tournamentEvVarianceEvLabel = AddTournamentEvResultRow(varianceLayout, "EV / tournament");
        _tournamentEvVarianceRoiLabel = AddTournamentEvResultRow(varianceLayout, "ROI %");
        _tournamentEvCashProbabilityLabel = AddTournamentEvResultRow(varianceLayout, "Win/cash probability");
        _tournamentEvStdDevLabel = AddTournamentEvResultRow(varianceLayout, "Std dev / tournament");
        _tournamentEvStdDevBuyInsLabel = AddTournamentEvResultRow(varianceLayout, "Std dev in buy-ins");
        _tournamentEvExpectedAfterSampleLabel = AddTournamentEvResultRow(varianceLayout, "Expected after N");
        _tournamentEvLikelyRangeLabel = AddTournamentEvResultRow(varianceLayout, "Likely range after N");
        _tournamentEvChanceNotAheadLabel = AddTournamentEvResultRow(varianceLayout, "Chance still down");
        _tournamentEvBankrollSwingLabel = AddTournamentEvResultRow(varianceLayout, "1 SD / bankroll");

        RefreshTournamentEv();

        var panels = new Control[] { formScroller, result, variance };
        void ApplyResponsiveTournamentEvLayout()
        {
            var width = viewport.ClientSize.Width <= 0 ? ClientSize.Width : viewport.ClientSize.Width;
            var stacked = width < portraitBreakpoint;
            ConfigureTournamentEvRoot(root, stacked, panels);
            root.Height = Math.Max(viewport.ClientSize.Height, stacked ? 1580 : viewport.ClientSize.Height);
        }

        viewport.Resize += (_, _) => ApplyResponsiveTournamentEvLayout();
        ApplyResponsiveTournamentEvLayout();
        return viewport;
    }

    private static void ConfigureTournamentEvRoot(
        TableLayoutPanel root,
        bool stacked,
        IReadOnlyList<Control> panels)
    {
        root.SuspendLayout();
        try
        {
            root.ColumnStyles.Clear();
            root.RowStyles.Clear();
            root.ColumnCount = stacked ? 1 : 3;
            root.RowCount = stacked ? 3 : 1;

            if (stacked)
            {
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 820));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 354));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 382));
            }
            else
            {
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440));
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            }

            for (var index = 0; index < panels.Count; index++)
            {
                var panel = panels[index];
                root.SetColumn(panel, stacked ? 0 : index);
                root.SetRow(panel, stacked ? index : 0);
                panel.Margin = stacked && index > 0
                    ? new Padding(0, 8, 0, 0)
                    : index > 0
                        ? new Padding(8, 0, 0, 0)
                        : new Padding(0);
            }
        }
        finally
        {
            root.ResumeLayout();
        }
    }

    private void RefreshTournamentEv()
    {
        if (_tournamentEvStatusLabel is null)
        {
            return;
        }

        var request = BuildTournamentEvRequest();
        UpdateTournamentEvInputState(request.PrizeType, request.TournamentType);

        var result = TournamentEvCalculator.Evaluate(request);
        _tournamentEvStatusLabel.Text = TournamentEvStatusText(result.Status);
        _tournamentEvStatusLabel.ForeColor = TournamentEvStatusForeColor(result.Status);
        _tournamentEvStatusLabel.BackColor = TournamentEvStatusBackColor(result.Status);
        _tournamentEvPrizeValueLabel.Text = Money(result.TotalPrizeValue);
        _tournamentEvMaxPrizeLabel.Text = Money(result.MaxSinglePrizeValue);
        _tournamentEvUncappedGrossLabel.Text = Money(result.UncappedGrossEv);
        _tournamentEvGrossLabel.Text = Money(result.GrossEv);
        _tournamentEvNetLabel.Text = Money(result.NetEv);
        _tournamentEvNetLabel.ForeColor = SignColor(result.NetEv);
        _tournamentEvRoiLabel.Text = result.Roi.ToString("P1", CultureInfo.CurrentCulture);
        _tournamentEvRoiLabel.ForeColor = SignColor(result.Roi);
        _tournamentEvBreakevenLabel.Text = result.CanBreakEven
            ? FormatEntryCount(result.ExactBreakEvenEntries)
            : "No breakeven";
        _tournamentEvPositiveUntilLabel.Text = FormatPositiveEntries(result.MaxPositiveEntries);
        _tournamentEvNegativeFromLabel.Text = FormatEntries(result.NegativeEvStartsAt);

        var variance = result.Variance;
        _tournamentEvVarianceRatingLabel.Text = VarianceRatingText(variance.Rating);
        _tournamentEvVarianceRatingLabel.ForeColor = VarianceRatingForeColor(variance.Rating);
        _tournamentEvVarianceRatingLabel.BackColor = VarianceRatingBackColor(variance.Rating);
        _tournamentEvVarianceEvLabel.Text = Money(variance.EvPerTournament);
        _tournamentEvVarianceEvLabel.ForeColor = SignColor(variance.EvPerTournament);
        _tournamentEvVarianceRoiLabel.Text = FormatPercent(variance.Roi);
        _tournamentEvVarianceRoiLabel.ForeColor = SignColor(variance.Roi);
        _tournamentEvCashProbabilityLabel.Text = FormatPercent(variance.WinOrCashProbability);
        _tournamentEvStdDevLabel.Text = Money(variance.StandardDeviation);
        _tournamentEvStdDevBuyInsLabel.Text = $"{variance.StandardDeviationInBuyIns:0.00} BI";
        _tournamentEvExpectedAfterSampleLabel.Text = Money(variance.ExpectedProfitAfterSample);
        _tournamentEvExpectedAfterSampleLabel.ForeColor = SignColor(variance.ExpectedProfitAfterSample);
        _tournamentEvLikelyRangeLabel.Text =
            $"{Money(variance.LikelyResultLowAfterSample)} to {Money(variance.LikelyResultHighAfterSample)}";
        _tournamentEvChanceNotAheadLabel.Text =
            $"{FormatPercent(variance.ChanceNotAheadAfterSample)} {(variance.ChanceNotAheadIsExact ? "exact" : "approx")}";
        _tournamentEvBankrollSwingLabel.Text = request.BankrollSize > 0m
            ? FormatPercent(variance.BankrollSwingPercentAfterSample)
            : "-";
        _statusLabel.Text = "Tournament EV checked.";
    }

    private void TournamentEvInputChanged()
    {
        if (_tournamentEvPrizeType is null)
        {
            return;
        }

        var selectedPrizeType = _tournamentEvPrizeType.SelectedItem is TournamentEvPrizeTypeOption option
            ? option.PrizeType
            : TournamentEvPrizeType.Tickets;
        var selectedTournamentType = _tournamentEvTournamentType.SelectedItem is TournamentEvTournamentTypeOption typeOption
            ? typeOption.TournamentType
            : TournamentEvTournamentType.FlatTicketSatellite;
        UpdateTournamentEvInputState(selectedPrizeType, selectedTournamentType);
        RefreshTournamentEv();
    }

    private void UpdateTournamentEvInputState(TournamentEvPrizeType prizeType, TournamentEvTournamentType tournamentType)
    {
        var ticketsMode = prizeType == TournamentEvPrizeType.Tickets;
        _tournamentEvNumberOfTickets.Enabled = ticketsMode;
        _tournamentEvTicketValue.Enabled = ticketsMode;
        _tournamentEvTicketDiscount.Enabled = ticketsMode;
        _tournamentEvManualPrizeValue.Enabled = !ticketsMode;
        _tournamentEvPayoutStructure.Enabled = tournamentType != TournamentEvTournamentType.FlatTicketSatellite;
    }

    private TournamentEvRequest BuildTournamentEvRequest()
    {
        var selectedPrizeType = _tournamentEvPrizeType.SelectedItem is TournamentEvPrizeTypeOption option
            ? option.PrizeType
            : TournamentEvPrizeType.Tickets;
        var selectedTournamentType = _tournamentEvTournamentType.SelectedItem is TournamentEvTournamentTypeOption typeOption
            ? typeOption.TournamentType
            : TournamentEvTournamentType.FlatTicketSatellite;

        return new TournamentEvRequest
        {
            TournamentName = _tournamentEvName.Text.Trim(),
            BuyIn = _tournamentEvBuyIn.Value,
            PrizeType = selectedPrizeType,
            TournamentType = selectedTournamentType,
            NumberOfTickets = (int)_tournamentEvNumberOfTickets.Value,
            TicketValue = _tournamentEvTicketValue.Value,
            ManualPrizeValue = _tournamentEvManualPrizeValue.Value,
            CurrentEntries = (int)_tournamentEvCurrentEntries.Value,
            TotalEntries = (int)_tournamentEvTotalEntries.Value,
            PaidPlaces = (int)_tournamentEvPaidPlaces.Value,
            TicketValueDiscountPercent = _tournamentEvTicketDiscount.Value,
            SampleSize = (int)_tournamentEvSampleSize.Value,
            BankrollSize = _tournamentEvBankrollSize.Value,
            PayoutStructure = _tournamentEvPayoutStructure.Text
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

    private static ComboBox BuildTournamentEvTournamentTypeBox()
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
        box.Items.Add(new TournamentEvTournamentTypeOption(TournamentEvTournamentType.FlatTicketSatellite, "Flat Ticket/Satellite"));
        box.Items.Add(new TournamentEvTournamentTypeOption(TournamentEvTournamentType.NormalMtt, "Normal MTT"));
        box.Items.Add(new TournamentEvTournamentTypeOption(TournamentEvTournamentType.TopHeavyMtt, "Top-Heavy MTT"));
        box.Items.Add(new TournamentEvTournamentTypeOption(TournamentEvTournamentType.WinnerTakeAll, "Winner-Take-All"));
        box.Items.Add(new TournamentEvTournamentTypeOption(TournamentEvTournamentType.CustomPayouts, "Custom Payouts"));
        box.SelectedIndex = 0;
        return box;
    }

    private static void AddTournamentEvTitleRow(TableLayoutPanel layout, string text)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var label = Theme.Label(text, Theme.SubHeaderFont, Theme.Text);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 0, 4, 4);
        label.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 2);
    }

    private static void AddTournamentEvInputRow(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.AutoSize = false;
        labelControl.AutoEllipsis = true;
        labelControl.Dock = DockStyle.Fill;
        labelControl.Margin = new Padding(0);
        labelControl.Padding = new Padding(0, 0, 8, 0);
        labelControl.TextAlign = ContentAlignment.MiddleLeft;
        labelControl.UseMnemonic = false;

        control.Margin = new Padding(2, 1, 2, 1);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddTournamentEvTallInputRow(TableLayoutPanel layout, string label, Control control, int height)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.AutoSize = false;
        labelControl.AutoEllipsis = true;
        labelControl.Dock = DockStyle.Fill;
        labelControl.Margin = new Padding(0);
        labelControl.Padding = new Padding(0, 4, 8, 0);
        labelControl.TextAlign = ContentAlignment.TopLeft;
        labelControl.UseMnemonic = false;

        control.Margin = new Padding(2, 1, 2, 5);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddTournamentEvHelpRow(TableLayoutPanel layout, string text)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var label = Theme.Label(text, Theme.SmallFont, Theme.Warning);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 0, 2, 6);
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
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.Panel,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(8, 0, 8, 2),
            UseMnemonic = false
        };
    }

    private static Label AddTournamentEvResultRow(TableLayoutPanel layout, string labelText)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var label = Theme.Label(labelText, Theme.BodyFont, Theme.Muted);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0);
        label.Padding = new Padding(0, 0, 8, 0);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.UseMnemonic = false;

        var value = Theme.Label("-", Theme.BodyFont, Theme.Text);
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

    private static string VarianceRatingText(TournamentEvVarianceRating rating)
    {
        return rating switch
        {
            TournamentEvVarianceRating.Low => "Low Variance",
            TournamentEvVarianceRating.Medium => "Medium Variance",
            TournamentEvVarianceRating.High => "High Variance",
            TournamentEvVarianceRating.Extreme => "Extreme Variance",
            _ => string.Empty
        };
    }

    private static Color VarianceRatingBackColor(TournamentEvVarianceRating rating)
    {
        return rating switch
        {
            TournamentEvVarianceRating.Low => Theme.PositiveSurface,
            TournamentEvVarianceRating.Medium => Theme.AccentSurface,
            TournamentEvVarianceRating.High => Theme.WarningSurface,
            TournamentEvVarianceRating.Extreme => Theme.NegativeSurface,
            _ => Theme.Panel
        };
    }

    private static Color VarianceRatingForeColor(TournamentEvVarianceRating rating)
    {
        return rating switch
        {
            TournamentEvVarianceRating.Low => Theme.Positive,
            TournamentEvVarianceRating.Medium => Theme.Accent,
            TournamentEvVarianceRating.High => Theme.Warning,
            TournamentEvVarianceRating.Extreme => Theme.Negative,
            _ => Theme.Text
        };
    }

    private static string FormatPercent(decimal value)
    {
        return value.ToString("P1", CultureInfo.CurrentCulture);
    }

    private static string FormatEntryCount(decimal value)
    {
        var format = value == decimal.Truncate(value) ? "0" : "0.##";
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private static string FormatPositiveEntries(long count)
    {
        if (count == long.MaxValue)
        {
            return "Always +EV";
        }

        return count <= 0
            ? "Never +EV"
            : FormatEntries(count);
    }

    private static string FormatEntries(long count)
    {
        if (count == long.MaxValue)
        {
            return "Never";
        }

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

    private sealed record TournamentEvTournamentTypeOption(TournamentEvTournamentType TournamentType, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
