using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class CashSessionStartDialog : Form
{
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

    public CashSessionStartDialog(CashSession entry)
    {
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
        _platform = Theme.EnumBox(Entry.Platform);
        _format = Theme.EnumBox(Entry.Format);
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
        _format.SelectedIndexChanged += (_, _) => UpdateFormatWarning();
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
}
