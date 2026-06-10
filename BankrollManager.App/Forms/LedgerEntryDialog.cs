using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class LedgerEntryDialog : Form
{
    private readonly DateTimePicker _date;
    private readonly ComboBox _type;
    private readonly ComboBox _platform;
    private readonly TextBox _description;
    private readonly NumericUpDown _amount;
    private readonly ComboBox _category;
    private readonly TextBox _notes;

    public LedgerEntryDialog(LedgerEntry entry)
    {
        Entry = Clone(entry);
        Text = Entry.Id == Guid.Empty ? "Add Ledger Entry" : "Ledger Entry";
        Size = new Size(560, 520);
        MinimumSize = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var layout = DialogLayout.Create(this, Save);
        _date = Theme.DatePicker(Entry.Date);
        _type = Theme.EnumBox(Entry.Type);
        _platform = Theme.EnumBox(Entry.Platform);
        _description = Theme.TextBox();
        _description.Text = Entry.Description;
        _amount = Theme.MoneyBox(Entry.Amount);
        _category = Theme.EnumBox(Entry.Category);
        _notes = Theme.TextBox(multiline: true);
        _notes.Text = Entry.Notes;

        DialogLayout.AddRow(layout, "Date", _date);
        DialogLayout.AddRow(layout, "Type", _type);
        DialogLayout.AddRow(layout, "Platform", _platform);
        DialogLayout.AddRow(layout, "Description", _description);
        DialogLayout.AddRow(layout, "Amount", _amount);
        DialogLayout.AddRow(layout, "Category", _category);
        DialogLayout.AddRow(layout, "Notes", _notes);
    }

    public LedgerEntry Entry { get; private set; }

    private void Save()
    {
        Entry.Date = DateOnly.FromDateTime(_date.Value);
        Entry.Type = (LedgerType)_type.SelectedItem!;
        Entry.Platform = (Platform)_platform.SelectedItem!;
        Entry.Description = _description.Text.Trim();
        Entry.Amount = _amount.Value;
        Entry.Category = (TournamentCategory)_category.SelectedItem!;
        Entry.Notes = _notes.Text.Trim();

        var errors = EntryValidator.Validate(Entry);
        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static LedgerEntry Clone(LedgerEntry entry)
    {
        return new LedgerEntry
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            Date = entry.Date,
            Type = entry.Type,
            Platform = entry.Platform,
            Description = entry.Description,
            Amount = entry.Amount,
            Category = entry.Category,
            BankrollBefore = entry.BankrollBefore,
            BankrollAfter = entry.BankrollAfter,
            Notes = entry.Notes
        };
    }
}
