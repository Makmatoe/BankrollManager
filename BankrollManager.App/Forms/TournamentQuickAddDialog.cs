using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class TournamentQuickAddDialog : Form
{
    private readonly TournamentPreset _preset;
    private readonly Panel _scrollHost;
    private readonly Label _remainingLabel;
    private readonly Button _nextButton;
    private readonly List<QuickAddTournamentRow> _rows = [];
    private int _currentIndex;

    public TournamentQuickAddDialog(TournamentPreset preset, BankrollSettings settings, int count)
    {
        _preset = preset;
        count = Math.Clamp(count, 1, 200);
        Entries = [];

        Text = "Quick Add Tournaments";
        Size = new Size(900, 760);
        MinimumSize = new Size(760, 560);
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

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            RowCount = 2,
            BackColor = Theme.Panel,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0)
        };
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = Theme.Label(TournamentPresetService.DisplayName(preset, settings), Theme.SubHeaderFont, Theme.Text);
        title.AutoSize = false;
        title.Dock = DockStyle.Top;
        title.Height = 26;
        title.AutoEllipsis = true;
        title.Margin = new Padding(0);
        _remainingLabel = Theme.Label(string.Empty, Theme.BodyFont, Theme.Muted);
        _remainingLabel.AutoSize = false;
        _remainingLabel.Dock = DockStyle.Top;
        _remainingLabel.Height = 24;
        _remainingLabel.Margin = new Padding(0);
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(_remainingLabel, 0, 1);
        root.Controls.Add(header, 0, 0);

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
            var row = new QuickAddTournamentRow(index, now);
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
        var cancel = Theme.Button("Cancel");
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.Add(addButton);
        footer.Controls.Add(_nextButton);
        footer.Controls.Add(cancel);
        AcceptButton = addButton;
        CancelButton = cancel;

        UpdateCurrentRow();
    }

    public IReadOnlyList<TournamentEntry> Entries { get; private set; }

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
        private readonly CheckBox _finished;
        private readonly NumericUpDown _wonAmount;
        private readonly Control _resultPanel;

        public QuickAddTournamentRow(int index, DateTime initialTime)
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
                RowCount = 2,
                BackColor = Theme.Panel,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Container.Controls.Add(layout);

            var line = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Theme.Panel,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            _title = Theme.Label($"Tournament {index + 1}", Theme.SubHeaderFont, Theme.Text);
            _title.AutoSize = false;
            _title.Width = 120;
            _title.Height = Theme.ControlHeight;
            _title.TextAlign = ContentAlignment.MiddleLeft;
            _title.Margin = new Padding(0, 4, 10, 4);

            _date = Theme.DatePicker(DateOnly.FromDateTime(initialTime));
            _time = Theme.TimePicker(TimeOnly.FromDateTime(initialTime));
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
            line.Controls.Add(_finished);
            layout.Controls.Add(line, 0, 0);

            _wonAmount = Theme.MoneyBox(0m);
            _wonAmount.Minimum = 0m;
            _resultPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Theme.Panel,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(130, 4, 0, 0),
                Visible = false,
                WrapContents = true
            };
            AddInlineField((FlowLayoutPanel)_resultPanel, "Won amount", _wonAmount);
            layout.Controls.Add(_resultPanel, 0, 1);
            _finished.CheckedChanged += (_, _) =>
            {
                _resultPanel.Visible = _finished.Checked;
                Container.PerformLayout();
            };
        }

        public Panel Container { get; }

        public TournamentEntry CreateEntry(TournamentPreset preset)
        {
            return TournamentPresetService.CreateQuickEntry(
                preset,
                DateOnly.FromDateTime(_date.Value),
                TimeOnly.FromDateTime(_time.Value),
                _finished.Checked,
                _wonAmount.Value);
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
    }
}
