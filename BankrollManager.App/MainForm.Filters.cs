using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.App;

public sealed partial class MainForm
{
    private DetailTableFilterControls AddDetailTableFilters(
        FlowLayoutPanel host,
        DetailTableKind kind,
        Action applyFilters)
    {
        var controls = new DetailTableFilterControls(kind, applyFilters);
        host.Controls.Add(FilterLabel("Range"));
        host.Controls.Add(controls.DateRange);
        host.Controls.Add(FilterLabel("From"));
        host.Controls.Add(controls.FromDate);
        host.Controls.Add(FilterLabel("To"));
        host.Controls.Add(controls.ToDate);
        host.Controls.Add(FilterLabel("Search"));
        host.Controls.Add(controls.Search);
        host.Controls.Add(controls.OpenOnly);
        host.Controls.Add(controls.FinishedOnly);
        host.Controls.Add(controls.FlipsOnly);
        host.Controls.Add(controls.TicketRelatedOnly);
        host.Controls.Add(controls.HighRiskOnly);
        host.Controls.Add(controls.ProfitableOnly);
        host.Controls.Add(controls.LosingOnly);
        host.Controls.Add(controls.ClearButton);
        return controls;
    }

    private static Label FilterLabel(string text)
    {
        var label = Theme.Label(text, Theme.SmallFont, Theme.Muted);
        label.AutoSize = false;
        label.Width = TextRenderer.MeasureText(text, Theme.SmallFont).Width + 8;
        label.Height = Theme.ControlHeight;
        label.Margin = new Padding(6, 6, 0, 0);
        label.TextAlign = ContentAlignment.MiddleLeft;
        return label;
    }

    private void RefreshTournamentRows()
    {
        _tournamentLoader.SetRows(FilteredTournamentRows(), IsNavigationPageSelected("MTTs"));
        UpdateTournamentInspector();
        FitGridColumns(_tournamentGrid);
    }

    private void RefreshCashRows()
    {
        _cashLoader.SetRows(FilteredCashRows(), IsNavigationPageSelected("Cash"));
        UpdateCashInspector();
        FitGridColumns(_cashGrid);
    }

    private void RefreshLedgerRows()
    {
        _ledgerLoader.SetRows(FilteredLedgerRows(), IsNavigationPageSelected("Ledger"));
        FitGridColumns(_ledgerGrid);
    }

    private void RefreshTimelineRows()
    {
        _timelineLoader.SetRows(FilteredTimelineRows(), IsNavigationPageSelected("Timeline"));
        FitGridColumns(_timelineGrid);
    }

    private IEnumerable<TournamentEntry> FilteredTournamentRows()
    {
        return DetailTableFilter.Apply(
            _data.TournamentEntries
                .OrderByDescending(entry => entry.Date)
                .ThenByDescending(entry => entry.RegistrationTime ?? TimeOnly.MinValue),
            _tournamentFilterControls.Criteria);
    }

    private IEnumerable<CashSession> FilteredCashRows()
    {
        return DetailTableFilter.Apply(
            _data.CashSessions
                .OrderByDescending(entry => entry.Date)
                .ThenByDescending(entry => entry.SessionTime ?? TimeOnly.MinValue),
            _cashFilterControls.Criteria);
    }

    private IEnumerable<LedgerEntry> FilteredLedgerRows()
    {
        return DetailTableFilter.Apply(
            _data.LedgerEntries.OrderByDescending(entry => entry.Date),
            _ledgerFilterControls.Criteria);
    }

    private IEnumerable<AuditTimelineEntry> FilteredTimelineRows()
    {
        var timeline = _currentViewData?.AuditTimeline
            ?? BankrollCalculator.GetAuditTimeline(_data);
        return DetailTableFilter.Apply(timeline, _timelineFilterControls.Criteria);
    }

    private sealed class DetailTableFilterControls
    {
        private static readonly IReadOnlyList<DateRangeOption> DateRangeOptions =
        [
            new(DetailTableDateRange.AllTime, "All time"),
            new(DetailTableDateRange.CurrentMonth, "Current month"),
            new(DetailTableDateRange.Last30Days, "Last 30 days"),
            new(DetailTableDateRange.Custom, "Custom")
        ];

        private readonly Action _applyFilters;
        private bool _syncing;

        public DetailTableFilterControls(DetailTableKind kind, Action applyFilters)
        {
            _applyFilters = applyFilters;
            DateRange = BuildDateRangeBox();
            FromDate = Theme.DatePicker(DateOnly.FromDateTime(DateTime.Today).AddDays(-29));
            FromDate.Width = 118;
            ToDate = Theme.DatePicker(DateOnly.FromDateTime(DateTime.Today));
            ToDate.Width = 118;
            Search = Theme.TextBox();
            Search.Width = 180;
            Search.Margin = new Padding(4, 4, 4, 0);
            Search.PlaceholderText = "Search entries";
            OpenOnly = BuildCheckBox("Open");
            FinishedOnly = BuildCheckBox("Finished");
            FlipsOnly = BuildCheckBox("Flips");
            TicketRelatedOnly = BuildCheckBox("Tickets");
            HighRiskOnly = BuildCheckBox("High risk");
            ProfitableOnly = BuildCheckBox("Profit");
            LosingOnly = BuildCheckBox("Loss");
            ClearButton = Theme.Button("Clear filters");
            ClearButton.AutoSize = false;
            ClearButton.Width = 112;
            ClearButton.Height = Theme.ControlHeight;
            ClearButton.Margin = new Padding(4, 4, 4, 0);

            ConfigureKind(kind);
            WireEvents();
            UpdateCustomDateState();
        }

        public ComboBox DateRange { get; }

        public DateTimePicker FromDate { get; }

        public DateTimePicker ToDate { get; }

        public TextBox Search { get; }

        public CheckBox OpenOnly { get; }

        public CheckBox FinishedOnly { get; }

        public CheckBox FlipsOnly { get; }

        public CheckBox TicketRelatedOnly { get; }

        public CheckBox HighRiskOnly { get; }

        public CheckBox ProfitableOnly { get; }

        public CheckBox LosingOnly { get; }

        public Button ClearButton { get; }

        public DetailTableFilterCriteria Criteria
        {
            get
            {
                var selectedRange = DateRange.SelectedItem is DateRangeOption option
                    ? option.Range
                    : DetailTableDateRange.AllTime;
                return new DetailTableFilterCriteria(
                    selectedRange,
                    DateOnly.FromDateTime(DateTime.Today),
                    DateOnly.FromDateTime(FromDate.Value),
                    DateOnly.FromDateTime(ToDate.Value),
                    Search.Text,
                    OpenOnly.Checked,
                    FinishedOnly.Checked,
                    FlipsOnly.Checked,
                    TicketRelatedOnly.Checked,
                    HighRiskOnly.Checked,
                    ProfitableOnly.Checked,
                    LosingOnly.Checked);
            }
        }

        public void ClearFilters()
        {
            Clear();
        }

        private static ComboBox BuildDateRangeBox()
        {
            var box = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.PanelAlt,
                ForeColor = Theme.Text,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.BodyFont,
                Width = 126,
                Height = Theme.ControlHeight,
                Margin = new Padding(4, 4, 4, 0)
            };
            box.Items.AddRange(DateRangeOptions.Cast<object>().ToArray());
            box.SelectedIndex = 0;
            return box;
        }

        private static CheckBox BuildCheckBox(string text)
        {
            var textWidth = TextRenderer.MeasureText(text, Theme.SmallFont).Width;
            var checkBox = new CheckBox
            {
                Text = text,
                Appearance = Appearance.Button,
                AutoSize = false,
                BackColor = Theme.Panel,
                ForeColor = Theme.Text,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.SmallFont,
                Height = Theme.ControlHeight,
                Margin = new Padding(4, 4, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Width = Math.Max(62, textWidth + 24)
            };
            checkBox.FlatAppearance.BorderColor = Theme.Border;
            checkBox.FlatAppearance.CheckedBackColor = Theme.AccentSurface;
            checkBox.FlatAppearance.MouseDownBackColor = Theme.ButtonDown;
            checkBox.FlatAppearance.MouseOverBackColor = Theme.PanelAlt;
            return checkBox;
        }

        private void ConfigureKind(DetailTableKind kind)
        {
            if (kind is DetailTableKind.Ledger or DetailTableKind.Timeline)
            {
                DisableUnavailableFilter(OpenOnly);
                DisableUnavailableFilter(FinishedOnly);
            }

            if (kind is DetailTableKind.Cash)
            {
                DisableUnavailableFilter(FlipsOnly);
                DisableUnavailableFilter(TicketRelatedOnly);
            }

            if (kind is DetailTableKind.Ledger)
            {
                DisableUnavailableFilter(HighRiskOnly);
            }
        }

        private static void DisableUnavailableFilter(CheckBox checkBox)
        {
            checkBox.Enabled = false;
            checkBox.ForeColor = Theme.Muted;
            checkBox.FlatAppearance.BorderColor = Theme.PanelRaised;
        }

        private void WireEvents()
        {
            DateRange.SelectedIndexChanged += (_, _) =>
            {
                UpdateCustomDateState();
                ApplyFilters();
            };
            FromDate.ValueChanged += (_, _) => ApplyFilters();
            ToDate.ValueChanged += (_, _) => ApplyFilters();
            Search.TextChanged += (_, _) => ApplyFilters();
            OpenOnly.CheckedChanged += (_, _) =>
            {
                ClearOpposingStatusFilter(OpenOnly, FinishedOnly);
                ApplyFilters();
            };
            FinishedOnly.CheckedChanged += (_, _) =>
            {
                ClearOpposingStatusFilter(FinishedOnly, OpenOnly);
                ApplyFilters();
            };
            FlipsOnly.CheckedChanged += (_, _) => ApplyFilters();
            TicketRelatedOnly.CheckedChanged += (_, _) => ApplyFilters();
            HighRiskOnly.CheckedChanged += (_, _) => ApplyFilters();
            ProfitableOnly.CheckedChanged += (_, _) => ApplyFilters();
            LosingOnly.CheckedChanged += (_, _) => ApplyFilters();
            ClearButton.Click += (_, _) => Clear();
        }

        private void Clear()
        {
            _syncing = true;
            try
            {
                DateRange.SelectedIndex = 0;
                FromDate.Value = DateTime.Today.AddDays(-29);
                ToDate.Value = DateTime.Today;
                Search.Clear();
                OpenOnly.Checked = false;
                FinishedOnly.Checked = false;
                FlipsOnly.Checked = false;
                TicketRelatedOnly.Checked = false;
                HighRiskOnly.Checked = false;
                ProfitableOnly.Checked = false;
                LosingOnly.Checked = false;
                UpdateCustomDateState();
            }
            finally
            {
                _syncing = false;
            }

            ApplyFilters();
        }

        private void UpdateCustomDateState()
        {
            var isCustom = DateRange.SelectedItem is DateRangeOption option
                && option.Range == DetailTableDateRange.Custom;
            FromDate.Enabled = isCustom;
            ToDate.Enabled = isCustom;
        }

        private void ApplyFilters()
        {
            if (!_syncing)
            {
                _applyFilters();
            }
        }

        private void ClearOpposingStatusFilter(CheckBox selected, CheckBox opposing)
        {
            if (_syncing || !selected.Checked || !opposing.Checked)
            {
                return;
            }

            _syncing = true;
            try
            {
                opposing.Checked = false;
            }
            finally
            {
                _syncing = false;
            }
        }

        private sealed record DateRangeOption(DetailTableDateRange Range, string Label)
        {
            public override string ToString()
            {
                return Label;
            }
        }
    }
}
