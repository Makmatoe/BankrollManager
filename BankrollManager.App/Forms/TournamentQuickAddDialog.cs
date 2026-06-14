using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class TournamentQuickAddDialog : Form
{
    private readonly TournamentPreset _preset;
    private readonly BankrollSettings _settings;
    private readonly Panel _scrollHost;
    private Label _remainingLabel = null!;
    private readonly Button _nextButton;
    private readonly List<QuickAddTournamentRow> _rows = [];
    private int _currentIndex;

    public TournamentQuickAddDialog(TournamentPreset preset, BankrollSettings settings, int count)
    {
        _preset = preset;
        _settings = settings;
        _settings.EnsureDefaults();
        count = Math.Clamp(count, 1, 200);
        Entries = [];

        Text = "Quick Add Tournaments";
        Size = new Size(980, 780);
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Theme.Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(preset, count), 0, 0);

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12),
            BackColor = Theme.Back
        };
        root.Controls.Add(_scrollHost, 0, 1);

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = count,
            BackColor = Theme.Back
        };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _scrollHost.Controls.Add(list);

        var now = DateTime.Now;
        for (var index = 0; index < count; index++)
        {
            var row = new QuickAddTournamentRow(index, preset, _settings, now);
            _rows.Add(row);
            list.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            list.Controls.Add(row.Container, 0, index);
        }

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Theme.Panel
        };
        root.Controls.Add(footer, 0, 2);

        var addButton = Theme.Button($"Add {count}");
        addButton.Click += (_, _) => Save();
        _nextButton = Theme.Button("Next");
        _nextButton.Click += (_, _) => MoveNext();
        var copyFirst = Theme.Button("Copy first down");
        copyFirst.Click += (_, _) => CopyFirstDown();
        var finishAll = Theme.Button("Finish all");
        finishAll.Click += (_, _) => SetFinishedForAll(true);
        var clearFinished = Theme.Button("Clear finishes");
        clearFinished.Click += (_, _) => SetFinishedForAll(false);
        var cancel = Theme.Button("Cancel");
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.Add(addButton);
        footer.Controls.Add(_nextButton);
        footer.Controls.Add(copyFirst);
        footer.Controls.Add(finishAll);
        footer.Controls.Add(clearFinished);
        footer.Controls.Add(cancel);
        AcceptButton = addButton;
        CancelButton = cancel;

        UpdateCurrentRow();
    }

    public IReadOnlyList<TournamentEntry> Entries { get; private set; }

    private Control BuildHeader(TournamentPreset preset, int count)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            RowCount = 3,
            BackColor = Theme.Panel,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0)
        };
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = Theme.Label(TournamentPresetService.DisplayName(preset, _settings), Theme.SubHeaderFont, Theme.Text);
        title.AutoSize = false;
        title.Dock = DockStyle.Top;
        title.Height = 26;
        title.AutoEllipsis = true;
        title.Margin = new Padding(0);

        var summary = Theme.Label(
            $"{count} rows | {preset.Platform} | {preset.Category} | {preset.Format}",
            Theme.SmallFont,
            Theme.Muted);
        summary.AutoSize = false;
        summary.Dock = DockStyle.Top;
        summary.Height = 22;
        summary.Margin = new Padding(0);

        _remainingLabel = Theme.Label(string.Empty, Theme.BodyFont, Theme.Muted);
        _remainingLabel.AutoSize = false;
        _remainingLabel.Dock = DockStyle.Top;
        _remainingLabel.Height = 24;
        _remainingLabel.Margin = new Padding(0);

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(summary, 0, 1);
        header.Controls.Add(_remainingLabel, 0, 2);
        return header;
    }

    private void MoveNext()
    {
        if (_currentIndex >= _rows.Count - 1)
        {
            return;
        }

        _currentIndex++;
        UpdateCurrentRow();
        _scrollHost.ScrollControlIntoView(_rows[_currentIndex].Container);
        _rows[_currentIndex].FocusDate();
    }

    private void SetFinishedForAll(bool finished)
    {
        foreach (var row in _rows)
        {
            row.SetFinished(finished);
        }
    }

    private void CopyFirstDown()
    {
        if (_rows.Count < 2)
        {
            return;
        }

        var source = _rows[0];
        foreach (var row in _rows.Skip(1))
        {
            row.CopyBatchValuesFrom(source);
        }
    }

    private void UpdateCurrentRow()
    {
        for (var index = 0; index < _rows.Count; index++)
        {
            _rows[index].SetActive(index == _currentIndex);
        }

        var remaining = Math.Max(0, _rows.Count - _currentIndex - 1);
        _remainingLabel.Text = $"Tournament {_currentIndex + 1} of {_rows.Count} | {remaining} remaining";
        _nextButton.Enabled = remaining > 0;
    }

    private void Save()
    {
        var entries = _rows
            .Select(row => row.CreateEntry(_preset))
            .ToList();
        var errors = new List<string>();
        for (var index = 0; index < entries.Count; index++)
        {
            errors.AddRange(EntryValidator.Validate(entries[index])
                .Select(error => $"Tournament {index + 1}: {error}"));
        }

        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        Entries = entries;
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed class QuickAddTournamentRow
    {
        private readonly Label _title;
        private readonly DateTimePicker _date;
        private readonly DateTimePicker _time;
        private readonly CheckBox _useTicket;
        private readonly NumericUpDown _ticketAmount;
        private readonly ComboBox _ticketPlatform;
        private readonly CheckBox _finished;
        private readonly DateTimePicker _finishedDate;
        private readonly DateTimePicker _finishedTime;
        private readonly ComboBox _resultKind;
        private readonly NumericUpDown _resultAmount;
        private readonly NumericUpDown _placement;
        private readonly NumericUpDown _fieldSize;
        private readonly Control _ticketPanel;
        private readonly Control _resultPanel;

        public QuickAddTournamentRow(
            int index,
            TournamentPreset preset,
            BankrollSettings settings,
            DateTime initialTime)
        {
            Container = new Panel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Theme.Panel,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.Panel,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Container.Controls.Add(layout);

            var line = BuildLinePanel();
            _title = Theme.Label($"Tournament {index + 1}", Theme.SubHeaderFont, Theme.Text);
            _title.AutoSize = false;
            _title.Width = 120;
            _title.Height = Theme.ControlHeight;
            _title.TextAlign = ContentAlignment.MiddleLeft;
            _title.Margin = new Padding(0, 4, 10, 4);

            _date = Theme.DatePicker(DateOnly.FromDateTime(initialTime));
            _time = Theme.TimePicker(TimeOnly.FromDateTime(initialTime));
            _useTicket = new CheckBox
            {
                Text = "Ticket",
                Checked = preset.TicketBuyInValue > 0m,
                ForeColor = Theme.Text,
                AutoSize = true,
                Height = Theme.ControlHeight,
                Margin = new Padding(12, 9, 4, 4)
            };
            _finished = new CheckBox
            {
                Text = "Finished",
                Checked = false,
                ForeColor = Theme.Text,
                AutoSize = true,
                Height = Theme.ControlHeight,
                Margin = new Padding(12, 9, 4, 4)
            };

            line.Controls.Add(_title);
            AddInlineField(line, "Date", _date);
            AddInlineField(line, "Time", _time);
            line.Controls.Add(_useTicket);
            line.Controls.Add(_finished);
            layout.Controls.Add(line, 0, 0);

            _ticketAmount = Theme.MoneyBox(preset.TicketBuyInValue);
            _ticketAmount.Minimum = 0m;
            _ticketAmount.Maximum = Math.Max(_ticketAmount.Maximum, Math.Max(0m, TicketMaximum(preset)));
            _ticketPlatform = Theme.EnumBox(
                preset.TicketBuyInPlatform ?? preset.Platform,
                PlatformCatalog.EnabledPlatforms(settings, preset.TicketBuyInPlatform ?? preset.Platform));
            _ticketPanel = BuildLinePanel(leftPadding: 130);
            AddInlineField((FlowLayoutPanel)_ticketPanel, "Ticket value", _ticketAmount);
            AddInlineField((FlowLayoutPanel)_ticketPanel, "From", _ticketPlatform);
            layout.Controls.Add(_ticketPanel, 0, 1);

            var defaultFinish = DefaultFinish(initialTime, preset);
            _finishedDate = Theme.DatePicker(DateOnly.FromDateTime(defaultFinish));
            _finishedTime = Theme.TimePicker(TimeOnly.FromDateTime(defaultFinish));
            _resultKind = Theme.EnumBox(DefaultResultKind(preset));
            _resultAmount = Theme.MoneyBox(0m);
            _resultAmount.Minimum = 0m;
            _placement = Theme.IntBox(0);
            _fieldSize = Theme.IntBox(preset.FieldSize ?? 0);
            _resultPanel = BuildLinePanel(leftPadding: 130);
            AddInlineField((FlowLayoutPanel)_resultPanel, "Finish date", _finishedDate);
            AddInlineField((FlowLayoutPanel)_resultPanel, "Finish time", _finishedTime);
            AddInlineField((FlowLayoutPanel)_resultPanel, "Result", _resultKind);
            AddInlineField((FlowLayoutPanel)_resultPanel, "Amount", _resultAmount);
            AddInlineField((FlowLayoutPanel)_resultPanel, "Place", _placement);
            AddInlineField((FlowLayoutPanel)_resultPanel, "Field", _fieldSize);
            layout.Controls.Add(_resultPanel, 0, 2);

            _useTicket.CheckedChanged += (_, _) => UpdatePanels();
            _finished.CheckedChanged += (_, _) => UpdatePanels();
            _date.ValueChanged += (_, _) => SyncDefaultFinishFromRegistration(preset);
            _time.ValueChanged += (_, _) => SyncDefaultFinishFromRegistration(preset);
            UpdatePanels();
        }

        public Panel Container { get; }

        public TournamentEntry CreateEntry(TournamentPreset preset)
        {
            var request = new TournamentQuickEntryRequest
            {
                RegistrationDate = DateOnly.FromDateTime(_date.Value),
                RegistrationTime = TimeOnly.FromDateTime(_time.Value),
                TicketBuyInValue = _useTicket.Checked ? _ticketAmount.Value : 0m,
                TicketBuyInPlatform = _useTicket.Checked ? (Platform)_ticketPlatform.SelectedItem! : null,
                Finished = _finished.Checked,
                FinishedDate = _finished.Checked ? DateOnly.FromDateTime(_finishedDate.Value) : null,
                FinishedTime = _finished.Checked ? TimeOnly.FromDateTime(_finishedTime.Value) : null,
                ResultKind = _finished.Checked ? (TournamentQuickResultKind)_resultKind.SelectedItem! : TournamentQuickResultKind.None,
                ResultAmount = _finished.Checked ? _resultAmount.Value : 0m,
                Placement = NullableInt(_placement),
                FieldSize = NullableInt(_fieldSize),
                ITM = _resultAmount.Value > 0m || NullableInt(_placement) is not null,
                FlipPhaseWon = _resultAmount.Value > 0m,
                GoPhaseReached = _resultAmount.Value > 0m
            };

            return TournamentPresetService.CreateQuickEntry(preset, request);
        }

        public void CopyBatchValuesFrom(QuickAddTournamentRow source)
        {
            _date.Value = source._date.Value;
            _time.Value = source._time.Value;
            _useTicket.Checked = source._useTicket.Checked;
            _ticketAmount.Value = ClampToBox(_ticketAmount, source._ticketAmount.Value);
            if (_ticketPlatform.Items.Contains(source._ticketPlatform.SelectedItem))
            {
                _ticketPlatform.SelectedItem = source._ticketPlatform.SelectedItem;
            }

            _finished.Checked = source._finished.Checked;
            _finishedDate.Value = source._finishedDate.Value;
            _finishedTime.Value = source._finishedTime.Value;
            _resultKind.SelectedItem = source._resultKind.SelectedItem;
            _resultAmount.Value = ClampToBox(_resultAmount, source._resultAmount.Value);
            _placement.Value = ClampToBox(_placement, source._placement.Value);
            _fieldSize.Value = ClampToBox(_fieldSize, source._fieldSize.Value);
        }

        public void FocusDate()
        {
            _date.Focus();
        }

        public void SetActive(bool active)
        {
            Container.BackColor = active ? Theme.PanelRaised : Theme.Panel;
            _title.ForeColor = active ? Theme.Accent : Theme.Text;
        }

        public void SetFinished(bool finished)
        {
            _finished.Checked = finished;
            UpdatePanels();
        }

        private void UpdatePanels()
        {
            _ticketPanel.Visible = _useTicket.Checked;
            _resultPanel.Visible = _finished.Checked;
            Container.PerformLayout();
        }

        private void SyncDefaultFinishFromRegistration(TournamentPreset preset)
        {
            if (!_finished.Checked)
            {
                return;
            }

            var finish = DefaultFinish(_date.Value.Date + _time.Value.TimeOfDay, preset);
            _finishedDate.Value = finish.Date;
            _finishedTime.Value = DateTime.Today + finish.TimeOfDay;
        }

        private static FlowLayoutPanel BuildLinePanel(int leftPadding = 0)
        {
            return new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Theme.Panel,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(leftPadding, 0, 0, 0),
                Visible = true,
                WrapContents = true,
                Margin = new Padding(0)
            };
        }

        private static void AddInlineField(FlowLayoutPanel parent, string labelText, Control control)
        {
            var wrapper = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = parent.BackColor,
                Margin = new Padding(0, 0, 14, 4)
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var label = Theme.Label(labelText, Theme.SmallFont, Theme.Muted);
            label.AutoSize = false;
            label.Width = TextRenderer.MeasureText(labelText, Theme.SmallFont).Width + 10;
            label.Height = Theme.ControlHeight;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Margin = new Padding(0, 4, 4, 4);

            control.Margin = new Padding(0, 4, 0, 4);
            wrapper.Controls.Add(label, 0, 0);
            wrapper.Controls.Add(control, 1, 0);
            parent.Controls.Add(wrapper);
        }

        private static int? NullableInt(NumericUpDown box)
        {
            return box.Value <= 0m ? null : (int)box.Value;
        }

        private static decimal ClampToBox(NumericUpDown box, decimal value)
        {
            return Math.Min(Math.Max(value, box.Minimum), box.Maximum);
        }

        private static decimal TicketMaximum(TournamentPreset preset)
        {
            return preset.Format == TournamentFormat.FlipAndGo
                ? Math.Max(preset.FlipBuyInPerStack * Math.Max(1, preset.FlipStacksBought), preset.BuyIn)
                : preset.BuyIn * Math.Max(1, preset.ActualBullets) + preset.AddOnsRebuys + preset.FeeRake;
        }

        private static DateTime DefaultFinish(DateTime registrationTime, TournamentPreset preset)
        {
            return IsFastFinishPreset(preset) ? registrationTime.AddMinutes(1) : registrationTime;
        }

        private static bool IsFastFinishPreset(TournamentPreset preset)
        {
            return preset.Category == TournamentCategory.FlipSatellite
                || preset.Format is TournamentFormat.Flip
                    or TournamentFormat.FlipAndGo
                    or TournamentFormat.Satellite
                    or TournamentFormat.TurboSatellite
                    or TournamentFormat.TargetStackSatellite
                    or TournamentFormat.FlashSatellite
                    or TournamentFormat.WSOPExpress
                || preset.EventTag is EventTag.FlipAndGo or EventTag.Ticket;
        }

        private static TournamentQuickResultKind DefaultResultKind(TournamentPreset preset)
        {
            return preset.Format is TournamentFormat.Satellite
                    or TournamentFormat.TurboSatellite
                    or TournamentFormat.TargetStackSatellite
                    or TournamentFormat.FlashSatellite
                    or TournamentFormat.WSOPExpress
                ? TournamentQuickResultKind.TicketWon
                : TournamentQuickResultKind.CashPrize;
        }
    }
}
