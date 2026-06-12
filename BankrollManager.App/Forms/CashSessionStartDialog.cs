using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class CashSessionStartDialog : Form
{
    private readonly BankrollSettings _settings;
    private readonly DateTimePicker _date;
    private readonly DateTimePicker _sessionTime;
    private readonly ComboBox _platform;
    private readonly ComboBox _format;
    private readonly TextBox _game;
    private readonly TextBox _stakes;
    private readonly NumericUpDown _smallBlind;
    private readonly NumericUpDown _bigBlind;
    private readonly NumericUpDown _startStackBuyIn;
    private readonly NumericUpDown _reloadCap;
    private readonly Label _formatWarning;
    private readonly DialogLayout.Row _formatWarningRow;
    private readonly TextBox _notes;
    private string _lastAutoStakes = string.Empty;
    private bool _syncingStakes;
    private bool _stakesEditedByUser;

    public CashSessionStartDialog(CashSession entry, BankrollSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();
        Entry = CashSessionDialogSupport.Clone(entry);
        CashSessionWorkflowService.MarkActive(Entry);
        Text = "Start Cash Session";
        Size = new Size(620, 640);
        MinimumSize = new Size(560, 540);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var layout = DialogLayout.Create(this, Save);
        _date = Theme.DatePicker(Entry.Date);
        _sessionTime = Theme.TimePicker(Entry.SessionTime ?? TimeOnly.FromDateTime(DateTime.Now));
        _platform = Theme.EnumBox(Entry.Platform, PlatformCatalog.EnabledPlatforms(_settings, Entry.Platform));
        _format = Theme.EnumBox(Entry.Format, PlatformCatalog.CashFormatsFor(Entry.Platform));
        _game = Theme.TextBox();
        _game.Text = Entry.Game;
        _stakes = Theme.TextBox();
        _stakes.Text = Entry.Stakes;
        _smallBlind = Theme.MoneyBox(Entry.SmallBlindAmount);
        _bigBlind = Theme.MoneyBox(Entry.BigBlindAmount);
        _startStackBuyIn = Theme.MoneyBox(Entry.StartStackBuyIn);
        _reloadCap = Theme.MoneyBox(Entry.ReloadCap);
        _formatWarning = Theme.Label(string.Empty, Theme.SubHeaderFont, Theme.Warning);
        _formatWarning.MaximumSize = new Size(390, 0);
        _notes = Theme.TextBox(multiline: true);
        _notes.Text = Entry.Notes;
        CashSessionDialogSupport.EnforceNonNegative(_smallBlind, _bigBlind, _startStackBuyIn, _reloadCap);

        DialogLayout.AddRow(layout, "Date", _date);
        DialogLayout.AddRow(layout, "Start time", _sessionTime);
        DialogLayout.AddRow(layout, "Platform", _platform);
        DialogLayout.AddRow(layout, "Cash format", _format);
        DialogLayout.AddRow(layout, "Game", _game);
        DialogLayout.AddRow(layout, "Stakes", _stakes);
        DialogLayout.AddRow(layout, "Small blind", _smallBlind);
        DialogLayout.AddRow(layout, "Big blind", _bigBlind);
        DialogLayout.AddRow(layout, "Buy-in", _startStackBuyIn);
        DialogLayout.AddRow(layout, "Reload cap", _reloadCap);
        _formatWarningRow = DialogLayout.AddRow(layout, "Format warning", _formatWarning);
        DialogLayout.AddRow(layout, "Notes", _notes);
        InitializeStakesAutoFill();
        _platform.SelectedIndexChanged += (_, _) =>
        {
            UpdateCashFormatChoices(includeCurrent: false);
            UpdateFormatWarning();
        };
        _format.SelectedIndexChanged += (_, _) => UpdateFormatWarning();
        UpdateCashFormatChoices(includeCurrent: true);
        UpdateFormatWarning();
    }

    public CashSession Entry { get; private set; }

    private void Save()
    {
        Entry.Date = DateOnly.FromDateTime(_date.Value);
        Entry.SessionTime = TimeOnly.FromDateTime(_sessionTime.Value);
        Entry.Platform = (Platform)_platform.SelectedItem!;
        Entry.Format = (CashFormat)_format.SelectedItem!;
        Entry.Game = _game.Text.Trim();
        Entry.Stakes = _stakes.Text.Trim();
        Entry.SmallBlindAmount = _smallBlind.Value;
        Entry.BigBlindAmount = _bigBlind.Value;
        Entry.StartStackBuyIn = _startStackBuyIn.Value;
        Entry.Reloads = 0m;
        Entry.ReloadCap = _reloadCap.Value;
        Entry.Notes = _notes.Text.Trim();
        CashSessionWorkflowService.MarkActive(Entry);

        var errors = EntryValidator.Validate(Entry);
        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateFormatWarning()
    {
        if (_format.SelectedItem is not CashFormat format)
        {
            return;
        }

        _formatWarning.Text = CashSessionDialogSupport.BuildFormatWarning(format);
        _formatWarningRow.SetVisible(!string.IsNullOrWhiteSpace(_formatWarning.Text));
    }

    private void UpdateCashFormatChoices(bool includeCurrent)
    {
        if (_platform.SelectedItem is not Platform platform)
        {
            return;
        }

        var selectedFormat = _format.SelectedItem is CashFormat format
            ? format
            : Entry.Format;
        Theme.SetEnumBoxItems(
            _format,
            PlatformCatalog.CashFormatsFor(platform),
            selectedFormat,
            includeCurrent);
    }

    private void InitializeStakesAutoFill()
    {
        _lastAutoStakes = CashSessionDialogSupport.FormatStakes(_smallBlind.Value, _bigBlind.Value, _settings);
        _stakesEditedByUser = !string.IsNullOrWhiteSpace(_stakes.Text)
            && !string.Equals(_stakes.Text.Trim(), _lastAutoStakes, StringComparison.Ordinal);
        _stakes.TextChanged += (_, _) => TrackStakesEdit();
        _smallBlind.ValueChanged += (_, _) => AutoFillStakes();
        _bigBlind.ValueChanged += (_, _) => AutoFillStakes();
        AutoFillStakes();
    }

    private void TrackStakesEdit()
    {
        if (_syncingStakes)
        {
            return;
        }

        var text = _stakes.Text.Trim();
        _stakesEditedByUser = !string.IsNullOrWhiteSpace(text)
            && !string.Equals(text, _lastAutoStakes, StringComparison.Ordinal);
    }

    private void AutoFillStakes()
    {
        var nextAutoStakes = CashSessionDialogSupport.FormatStakes(_smallBlind.Value, _bigBlind.Value, _settings);
        if (_stakesEditedByUser && !string.Equals(_stakes.Text.Trim(), _lastAutoStakes, StringComparison.Ordinal))
        {
            _lastAutoStakes = nextAutoStakes;
            return;
        }

        _syncingStakes = true;
        _stakes.Text = nextAutoStakes;
        _syncingStakes = false;
        _lastAutoStakes = nextAutoStakes;
        _stakesEditedByUser = false;
    }
}
