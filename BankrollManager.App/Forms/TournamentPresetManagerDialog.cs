using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.App.Forms;

internal sealed class TournamentPresetManagerDialog : Form
{
    private readonly BankrollSettings _settings;
    private readonly List<TournamentPreset> _presets;
    private readonly ListBox _list;
    private readonly TextBox _name;
    private readonly CheckBox _favorite;
    private bool _syncingSelection;

    public TournamentPresetManagerDialog(IReadOnlyList<TournamentPreset> presets, BankrollSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();
        _presets = TournamentPresetService.OrderedPresets(presets)
            .Select(TournamentPresetService.ClonePreset)
            .ToList();
        Presets = _presets;

        Text = "Manage Tournament Presets";
        Size = new Size(820, 560);
        MinimumSize = new Size(700, 460);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 2,
            BackColor = Theme.Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        Controls.Add(root);

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.BodyFont,
            IntegralHeight = false
        };
        _list.SelectedIndexChanged += (_, _) => LoadSelectedPreset();
        root.Controls.Add(_list, 0, 0);

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Panel,
            Padding = new Padding(12)
        };
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(editor, 1, 0);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Panel
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.Controls.Add(fields, 0, 0);

        _name = Theme.TextBox();
        _name.TextChanged += (_, _) => SaveSelectedName();
        _favorite = new CheckBox
        {
            Text = "Favorite",
            ForeColor = Theme.Text,
            AutoSize = true,
            Margin = new Padding(4, 9, 4, 9)
        };
        _favorite.CheckedChanged += (_, _) => SaveSelectedFavorite();
        DialogLayout.AddRow(fields, "Name", _name);
        DialogLayout.AddRow(fields, "Favorite", _favorite);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Theme.Panel,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 10, 0, 0),
            WrapContents = true
        };
        editor.Controls.Add(actions, 0, 1);

        AddAction(actions, "Edit fields", EditSelectedPreset);
        AddAction(actions, "Delete", DeleteSelectedPreset);
        AddAction(actions, "Move up", () => MoveSelectedPreset(-1));
        AddAction(actions, "Move down", () => MoveSelectedPreset(1));

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Theme.Panel
        };
        root.SetColumnSpan(footer, 2);
        root.Controls.Add(footer, 0, 1);

        var save = Theme.Button("Save");
        save.Click += (_, _) => Save();
        var cancel = Theme.Button("Cancel");
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;

        RefreshList(selectIndex: _presets.Count == 0 ? -1 : 0);
    }

    public IReadOnlyList<TournamentPreset> Presets { get; private set; }

    private void AddAction(FlowLayoutPanel parent, string text, Action action)
    {
        var button = Theme.Button(text);
        button.Click += (_, _) => action();
        parent.Controls.Add(button);
    }

    private void LoadSelectedPreset()
    {
        if (_syncingSelection)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            if (SelectedPreset() is not { } preset)
            {
                _name.Text = string.Empty;
                _favorite.Checked = false;
                _name.Enabled = false;
                _favorite.Enabled = false;
                return;
            }

            _name.Enabled = true;
            _favorite.Enabled = true;
            _name.Text = preset.Name;
            _favorite.Checked = preset.IsFavorite;
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void SaveSelectedName()
    {
        if (_syncingSelection || SelectedPreset() is not { } preset)
        {
            return;
        }

        preset.Name = _name.Text.Trim();
        if (string.IsNullOrWhiteSpace(preset.EventName))
        {
            preset.EventName = preset.Name;
        }

        preset.UpdatedUtc = DateTime.UtcNow;
        UpdateSelectedListItem();
    }

    private void SaveSelectedFavorite()
    {
        if (_syncingSelection || SelectedPreset() is not { } preset)
        {
            return;
        }

        preset.IsFavorite = _favorite.Checked;
        preset.UpdatedUtc = DateTime.UtcNow;
        UpdateSelectedListItem();
    }

    private void EditSelectedPreset()
    {
        if (SelectedPreset() is not { } preset)
        {
            return;
        }

        var entry = TournamentPresetService.CreateTemplateEntry(preset, DateTime.Now);
        using var dialog = new TournamentEntryDialog(entry, _settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(_name.Text)
            ? dialog.Entry.EventName
            : _name.Text;
        TournamentPresetService.UpdateFromEntry(preset, dialog.Entry, name, DateTime.UtcNow);
        RefreshList(_list.SelectedIndex);
        LoadSelectedPreset();
    }

    private void UpdateSelectedListItem()
    {
        var index = _list.SelectedIndex;
        if (index < 0 || index >= _presets.Count)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            _list.Items[index] = BuildListItem(_presets[index]);
            _list.SelectedIndex = index;
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void DeleteSelectedPreset()
    {
        var index = _list.SelectedIndex;
        if (index < 0 || index >= _presets.Count)
        {
            return;
        }

        if (MessageBox.Show(
            "Delete selected preset?",
            "Confirm delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _presets.RemoveAt(index);
        RefreshList(Math.Min(index, _presets.Count - 1));
    }

    private void MoveSelectedPreset(int direction)
    {
        var index = _list.SelectedIndex;
        var newIndex = index + direction;
        if (index < 0 || newIndex < 0 || newIndex >= _presets.Count)
        {
            return;
        }

        (_presets[index], _presets[newIndex]) = (_presets[newIndex], _presets[index]);
        RefreshList(newIndex);
    }

    private void RefreshList(int selectIndex)
    {
        _syncingSelection = true;
        try
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var preset in _presets)
            {
                _list.Items.Add(BuildListItem(preset));
            }

            if (_list.Items.Count > 0 && selectIndex >= 0)
            {
                _list.SelectedIndex = Math.Clamp(selectIndex, 0, _list.Items.Count - 1);
            }

            _list.EndUpdate();
        }
        finally
        {
            _syncingSelection = false;
        }

        LoadSelectedPreset();
    }

    private TournamentPresetManagerListItem BuildListItem(TournamentPreset preset)
    {
        return new TournamentPresetManagerListItem(
            preset,
            $"{(preset.IsFavorite ? "[Fav] " : string.Empty)}{TournamentPresetService.DisplayName(preset, _settings)}");
    }

    private TournamentPreset? SelectedPreset()
    {
        return _list.SelectedItem is TournamentPresetManagerListItem item ? item.Preset : null;
    }

    private void Save()
    {
        var errors = new List<string>();
        foreach (var preset in _presets)
        {
            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                errors.Add("Preset name is required.");
            }
        }

        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        TournamentPresetService.NormalizePresetOrder(_presets);
        Presets = _presets;
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record TournamentPresetManagerListItem(TournamentPreset Preset, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
