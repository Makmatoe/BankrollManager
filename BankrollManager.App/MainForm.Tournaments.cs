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

    private Control BuildTournamentTab()
    {
        var root = BuildGridShell(out var buttons);
        AddGridButton(buttons, "Add", AddTournament);
        AddGridButton(buttons, "Edit", EditTournament);
        AddGridButton(buttons, "Use Preset", UseTournamentPreset);
        AddGridButton(buttons, "Quick Add", QuickAddTournaments);
        AddGridButton(buttons, "Save Preset", SaveTournamentPreset);
        _tournamentDetailsButton = AddGridButton(buttons, "Details", ToggleTournamentDetails);
        AddGridButton(buttons, "Start", StartTournament);
        AddGridButton(buttons, "Finish", FinishTournament);
        AddGridButton(buttons, "Use Ticket", UseTicketForTournament);
        AddGridButton(buttons, "Ticket Won", MarkTournamentTicketWon);
        AddGridButton(buttons, "Delete", DeleteTournament);

        _tournamentLoader = new GridLoadController<TournamentEntry>(_tournamentSource);
        _tournamentGrid = CreateGrid(_tournamentSource, loadController: _tournamentLoader);
        _tournamentGrid.CellDoubleClick += (_, _) => EditTournament();
        _tournamentGrid.SelectionChanged += (_, _) => UpdateTournamentInspector();
        AddTextColumn(_tournamentGrid, "Date", "Date", 92);
        AddTextColumn(_tournamentGrid, "RegistrationTime", "Time", 70);
        AddTextColumn(_tournamentGrid, "Status", "Status", 88);
        AddTextColumn(_tournamentGrid, "FinishedDate", "Finished Date", 104);
        AddTextColumn(_tournamentGrid, "FinishedTime", "Finished Time", 96);
        AddTextColumn(_tournamentGrid, "Platform", "Platform", 115);
        AddTextColumn(_tournamentGrid, "Category", "Category", 115);
        AddTextColumn(_tournamentGrid, "Format", "Format", 92);
        AddTextColumn(_tournamentGrid, "EventName", "Tournament/Event", 190);
        AddTextColumn(_tournamentGrid, "Currency", "Currency", 82);
        AddTextColumn(_tournamentGrid, "EventTag", "Tag", 110);
        AddTextColumn(_tournamentGrid, "BuyIn", "Buy-in", 82);
        AddTextColumn(_tournamentGrid, "FeeRake", "Fee/Rake", 86);
        AddTextColumn(_tournamentGrid, "PlannedBullets", "Planned", 76);
        AddTextColumn(_tournamentGrid, "ActualBullets", "Actual", 70);
        AddTextColumn(_tournamentGrid, "AddOnsRebuys", "Add-ons", 82);
        AddTextColumn(_tournamentGrid, "BountyTicketValue", "Bounty Cash", 100);
        AddTextColumn(_tournamentGrid, "TicketBuyInValue", "Ticket Buy-in", 104);
        AddTextColumn(_tournamentGrid, "EffectiveTicketBuyInPlatform", "Ticket Platform", 112);
        AddTextColumn(_tournamentGrid, "TicketValueWon", "Ticket Won", 96);
        AddTextColumn(_tournamentGrid, "CashPrize", "Cash Prize", 90);
        AddTextColumn(_tournamentGrid, "TournamentDollarsWon", "T$ Won", 82);
        AddTextColumn(_tournamentGrid, "CashDollarsWon", "C$ Won", 82);
        AddTextColumn(_tournamentGrid, "BountyPrize", "Bounty", 82);
        AddTextColumn(_tournamentGrid, "MysteryBountyPrize", "Mystery", 82);
        AddTextColumn(_tournamentGrid, "PrizeWon", "Prize Won", 90);
        AddTextColumn(_tournamentGrid, "TotalCost", "Total Cost", 88);
        AddTextColumn(_tournamentGrid, "CashCost", "Cash Cost", 88);
        AddTextColumn(_tournamentGrid, "TicketBalanceImpact", "Ticket Net", 88);
        AddTextColumn(_tournamentGrid, "TotalValueProfitLoss", "Net P/L", 86);
        AddTextColumn(_tournamentGrid, "ValueROI", "ROI", 72);
        AddTextColumn(_tournamentGrid, "CashProfitLoss", "Cash P/L", 86);
        AddTextColumn(_tournamentGrid, "Placement", "Place", 70);
        AddTextColumn(_tournamentGrid, "FieldSize", "Field", 70);
        AddCheckColumn(_tournamentGrid, "ITM", "ITM", 54);
        AddCheckColumn(_tournamentGrid, "FinalTable", "FT", 54);
        AddTextColumn(_tournamentGrid, "RiskPercentageOfBankrollAtRegistration", "Risk %", 78);
        AddTextColumn(_tournamentGrid, "RuleCheckResult", "Rule", 96);
        AddTextColumn(_tournamentGrid, "BankrollBefore", "Cash BR Before", 125);
        AddTextColumn(_tournamentGrid, "BankrollAfter", "Cash BR After", 120);
        AddTextColumn(_tournamentGrid, "PreGameFocus", "Focus", 150);
        AddTextColumn(_tournamentGrid, "Tags", "Tags", 120);
        AddTextColumn(_tournamentGrid, "MistakeLesson", "Mistake/Lesson", 190);
        AddTextColumn(_tournamentGrid, "Notes", "Notes", 220);
        ApplyTournamentColumnMode();
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
        content.Controls.Add(BuildPagedGridWithEmptyState(
            _tournamentGrid,
            _tournamentLoader,
            out _tournamentEmptyState,
            "No tournaments yet. Add one manually or use Decide to evaluate the next registration."), 0, 0);
        content.Controls.Add(BuildTournamentInspector(), 0, 1);
        root.Controls.Add(content, 0, 1);
        return root;
    }

    private Control BuildTournamentInspector()
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

        _tournamentInspectorTitle = BuildInspectorLabel(Theme.SubHeaderFont, Theme.Text);
        _tournamentInspectorResult = BuildInspectorLabel(Theme.BodyFont, Theme.Text);
        _tournamentInspectorMeta = BuildInspectorLabel(Theme.BodyFont, Theme.Muted);
        _tournamentInspectorNotes = BuildInspectorLabel(Theme.SmallFont, Theme.Muted);

        shell.Controls.Add(_tournamentInspectorTitle, 0, 0);
        shell.Controls.Add(_tournamentInspectorResult, 0, 1);
        shell.Controls.Add(_tournamentInspectorMeta, 0, 2);
        shell.Controls.Add(_tournamentInspectorNotes, 0, 3);
        UpdateTournamentInspector();
        return shell;
    }

    private static Label BuildInspectorLabel(Font font, Color color)
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            AutoEllipsis = false,
            Font = font,
            ForeColor = color,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 2),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };
    }

    private void ToggleTournamentDetails()
    {
        _tournamentDetailColumnsVisible = !_tournamentDetailColumnsVisible;
        ApplyTournamentColumnMode();
        _statusLabel.Text = _tournamentDetailColumnsVisible
            ? "MTT details shown."
            : "MTT compact view shown.";
    }

    private void ApplyTournamentColumnMode()
    {
        if (_tournamentGrid is null)
        {
            return;
        }

        foreach (DataGridViewColumn column in _tournamentGrid.Columns)
        {
            column.Visible = _tournamentDetailColumnsVisible
                || CompactTournamentColumns.Contains(column.DataPropertyName);
        }

        if (_tournamentDetailsButton is not null)
        {
            _tournamentDetailsButton.Text = _tournamentDetailColumnsVisible ? "Compact" : "Details";
        }

        FitGridColumns(_tournamentGrid);
    }

    private void UpdateTournamentInspector()
    {
        if (_tournamentInspectorTitle is null
            || _tournamentInspectorResult is null
            || _tournamentInspectorMeta is null
            || _tournamentInspectorNotes is null)
        {
            return;
        }

        if (Selected<TournamentEntry>(_tournamentSource) is not { } entry)
        {
            _tournamentInspectorTitle.Text = "No tournament selected";
            _tournamentInspectorResult.Text = string.Empty;
            _tournamentInspectorMeta.Text = string.Empty;
            _tournamentInspectorNotes.Text = string.Empty;
            return;
        }

        var eventName = string.IsNullOrWhiteSpace(entry.EventName) ? entry.Format.ToString() : entry.EventName;
        _tournamentInspectorTitle.Text = $"{eventName} | {entry.Platform} | {entry.Category} | {entry.Format}";
        _tournamentInspectorResult.Text =
            $"Net {Money(entry.TotalValueProfitLoss)}  ROI {entry.ValueROI:P1}  Cash {Money(entry.CashProfitLoss)}  Ticket {Money(entry.TicketBalanceImpact)}  Cost {Money(entry.TotalCost)}";
        _tournamentInspectorResult.ForeColor = entry.TotalValueProfitLoss >= 0m ? Theme.Positive : Theme.Negative;
        _tournamentInspectorMeta.Text =
            $"Status {entry.Status}  Place {NullableText(entry.Placement)} / {NullableText(entry.FieldSize)}  ITM {YesNo(entry.ITM)}  FT {YesNo(entry.FinalTable)}  Cash bankroll {Money(entry.BankrollBefore)} -> {Money(entry.BankrollAfter)}";
        _tournamentInspectorNotes.Text = InspectorNotes(entry);
    }

    private static string NullableText(int? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture) ?? "-";
    }

    private static string YesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string InspectorNotes(TournamentEntry entry)
    {
        var parts = new[]
            {
                entry.PreGameFocus,
                entry.Tags,
                entry.MistakeLesson,
                entry.Notes
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return parts.Count == 0 ? "No notes recorded." : string.Join(" | ", parts);
    }


    private void AddTournament()
    {
        var entry = new TournamentEntry
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            RegistrationTime = TimeOnly.FromDateTime(DateTime.Now),
            Status = TournamentStatus.Registered,
            Platform = _data.Settings.DefaultPlatform,
            PlannedBullets = _data.Settings.DefaultMaxBullets,
            ActualBullets = _data.Settings.DefaultMaxBullets
        };

        using var dialog = new TournamentEntryDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.TournamentEntries.Add(dialog.Entry);
        SaveData("Tournament added.");
    }

    private void EditTournament()
    {
        EditSelectedTournament(_tournamentSource);
    }

    private void EditSelectedTournament(BindingSource source)
    {
        if (Selected<TournamentEntry>(source) is not { } selected)
        {
            return;
        }

        EditTournamentEntry(selected);
    }

    private void EditTournamentEntry(TournamentEntry selected)
    {
        using var dialog = new TournamentEntryDialog(selected, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopyTournament(dialog.Entry, _data.TournamentEntries.First(entry => entry.Id == selected.Id));
        SaveData("Tournament updated.");
    }

    private void UseTournamentPreset()
    {
        if (_data.TournamentPresets.Count == 0)
        {
            MessageBox.Show(
                "Save a tournament as a preset first.",
                "No presets",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (PromptTournamentPreset() is not { } preset)
        {
            return;
        }

        var entry = TournamentPresetService.CreateEntry(preset, DateTime.Now);
        using var dialog = new TournamentEntryDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.TournamentEntries.Add(dialog.Entry);
        preset.LastUsedUtc = DateTime.UtcNow;
        preset.UpdatedUtc = DateTime.UtcNow;
        SaveData($"Tournament added from {TournamentPresetService.DisplayName(preset, _data.Settings)}.");
    }

    private void QuickAddTournaments()
    {
        if (_data.TournamentPresets.Count == 0)
        {
            MessageBox.Show(
                "Save a tournament as a preset first.",
                "No presets",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (PromptTournamentQuickAddSetup() is not { } setup)
        {
            return;
        }

        using var dialog = new TournamentQuickAddDialog(setup.Preset, _data.Settings, setup.Count);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.TournamentEntries.AddRange(dialog.Entries);
        setup.Preset.LastUsedUtc = DateTime.UtcNow;
        setup.Preset.UpdatedUtc = DateTime.UtcNow;

        var addedText = dialog.Entries.Count == 1 ? "Tournament added" : $"{dialog.Entries.Count} tournaments added";
        SaveData($"{addedText} from {TournamentPresetService.DisplayName(setup.Preset, _data.Settings)}.");
    }

    private void SaveTournamentPreset()
    {
        if (Selected<TournamentEntry>(_tournamentSource) is not { } selected)
        {
            MessageBox.Show(
                "Select a tournament first.",
                "No tournament selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var fallbackName = string.IsNullOrWhiteSpace(selected.EventName)
            ? selected.Format.ToString()
            : selected.EventName;
        var name = PromptText("Save Tournament Preset", "Preset name", fallbackName);
        if (name is null)
        {
            return;
        }

        var preset = TournamentPresetService.UpsertFromEntry(_data.TournamentPresets, selected, name, DateTime.UtcNow);
        SaveData($"Preset saved: {TournamentPresetService.DisplayName(preset, _data.Settings)}.");
    }

    private void StartTournament()
    {
        if (Selected<TournamentEntry>(_tournamentSource) is not { } selected)
        {
            return;
        }

        var target = _data.TournamentEntries.First(entry => entry.Id == selected.Id);
        if (target.Status == TournamentStatus.Finished)
        {
            MessageBox.Show("Finished tournaments cannot be marked active.", "Tournament already finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        target.Status = TournamentStatus.Active;
        SaveData("Tournament marked active.");
    }

    private void FinishTournament()
    {
        if (Selected<TournamentEntry>(_tournamentSource) is not { } selected)
        {
            return;
        }

        var draft = new TournamentEntry();
        CopyTournament(selected, draft);
        draft.Status = TournamentStatus.Finished;
        draft.FinishedDate ??= DateOnly.FromDateTime(DateTime.Today);
        draft.FinishedTime ??= TimeOnly.FromDateTime(DateTime.Now);

        using var dialog = new TournamentEntryDialog(draft, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopyTournament(dialog.Entry, _data.TournamentEntries.First(entry => entry.Id == selected.Id));
        SaveData("Tournament finished.");
    }

    private void UseTicketForTournament()
    {
        if (Selected<TournamentEntry>(_tournamentSource) is not { } selected)
        {
            return;
        }

        var target = _data.TournamentEntries.First(entry => entry.Id == selected.Id);
        if (target.TotalCost <= 0m)
        {
            MessageBox.Show("This tournament has no recorded cost to cover with a ticket.", "No cost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var currentTicketPlatform = target.EffectiveTicketBuyInPlatform;
        var ticketPlatforms = Enum.GetValues<Platform>()
            .Select(platform =>
            {
                var available = AvailableTicketValueForUse(target, platform);
                return new TicketPlatformPromptItem(
                    platform,
                    available,
                    $"{platform} ({Money(available)} available)");
            })
            .Where(item => item.AvailableTicketValue > 0m
                && _data.Settings.IsPlatformEnabled(item.Platform)
                || (target.TicketBuyInValue > 0m && item.Platform == currentTicketPlatform))
            .OrderBy(item => item.Platform.ToString(), NaturalSortComparer.Instance)
            .ToList();
        if (ticketPlatforms.Count == 0)
        {
            MessageBox.Show("No ticket balance is available on any platform.", "No tickets", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var prompt = PromptTicketBuyIn(
            ticketPlatforms,
            target.EffectiveTicketBuyInPlatform,
            target.TicketBuyInValue,
            target.TotalCost);
        if (prompt is null)
        {
            return;
        }

        target.TicketBuyInValue = prompt.Amount;
        target.TicketBuyInPlatform = prompt.Amount > 0m ? prompt.Platform : null;
        SaveData(prompt.Amount > 0m
            ? $"Ticket buy-in applied from {prompt.Platform}."
            : "Ticket buy-in cleared.");
    }

    private decimal AvailableTicketValueForUse(TournamentEntry target, Platform platform)
    {
        var available = BankrollCalculator.TicketBalance(_data, platform);
        if (target.TicketBuyInValue > 0m && target.EffectiveTicketBuyInPlatform == platform)
        {
            available += target.TicketBuyInValue;
        }

        return Math.Max(0m, available);
    }

    private void MarkTournamentTicketWon()
    {
        if (Selected<TournamentEntry>(_tournamentSource) is not { } selected)
        {
            return;
        }

        var target = _data.TournamentEntries.First(entry => entry.Id == selected.Id);
        var initial = target.TicketValueWon > 0m ? target.TicketValueWon : Math.Max(target.BuyIn, target.CashCost);
        var amount = PromptMoney("Ticket Won", "Ticket value won", initial, 0m, 1_000_000m);
        if (amount is null)
        {
            return;
        }

        target.Status = TournamentStatus.Finished;
        target.FinishedDate ??= DateOnly.FromDateTime(DateTime.Today);
        target.FinishedTime ??= TimeOnly.FromDateTime(DateTime.Now);
        target.ITM = amount.Value > 0m || target.ITM;
        target.TicketValueWon = amount.Value;
        SaveData("Tournament marked as ticket won.");
    }

    private void DeleteTournament()
    {
        if (Selected<TournamentEntry>(_tournamentSource) is not { } selected || !ConfirmDelete("tournament"))
        {
            return;
        }

        _data.TournamentEntries.RemoveAll(entry => entry.Id == selected.Id);
        SaveData("Tournament deleted.");
    }

    private static void CopyTournament(TournamentEntry source, TournamentEntry target)
    {
        target.Date = source.Date;
        target.RegistrationTime = source.RegistrationTime;
        target.Status = source.Status;
        target.FinishedDate = source.FinishedDate;
        target.FinishedTime = source.FinishedTime;
        target.Platform = source.Platform;
        target.Category = source.Category;
        target.Format = source.Format;
        target.EventName = source.EventName;
        target.Currency = source.Currency;
        target.EventTag = source.EventTag;
        target.IsPromoFreebieTicketEvent = source.IsPromoFreebieTicketEvent;
        target.BuyIn = source.BuyIn;
        target.FeeRake = source.FeeRake;
        target.PlannedBullets = source.PlannedBullets;
        target.ActualBullets = source.ActualBullets;
        target.AddOnsRebuys = source.AddOnsRebuys;
        target.BountyTicketValue = source.BountyTicketValue;
        target.TicketBuyInValue = source.TicketBuyInValue;
        target.TicketBuyInPlatform = source.TicketBuyInPlatform;
        target.TicketValueWon = source.TicketValueWon;
        target.CashPrize = source.CashPrize;
        target.TournamentDollarsWon = source.TournamentDollarsWon;
        target.CashDollarsWon = source.CashDollarsWon;
        target.RegularCashPrize = source.RegularCashPrize;
        target.MysteryBountyPrize = source.MysteryBountyPrize;
        target.BountyPhaseReached = source.BountyPhaseReached;
        target.KnockoutsAfterBountyPhase = source.KnockoutsAfterBountyPhase;
        target.MysteryBountyNotes = source.MysteryBountyNotes;
        target.BountyPrize = source.BountyPrize;
        target.Knockouts = source.Knockouts;
        target.SpinPlayerCount = source.SpinPlayerCount;
        target.InsuranceUsed = source.InsuranceUsed;
        target.InsuranceCost = source.InsuranceCost;
        target.MultiplierHit = source.MultiplierHit;
        target.PrizeWon = source.PrizeWon;
        target.FlipBuyInPerStack = source.FlipBuyInPerStack;
        target.FlipStacksBought = source.FlipStacksBought;
        target.FlipPhaseWon = source.FlipPhaseWon;
        target.GoPhaseReached = source.GoPhaseReached;
        target.RushStageSurvived = source.RushStageSurvived;
        target.BattleRoyaleFinalTableReached = source.BattleRoyaleFinalTableReached;
        target.TargetEventName = source.TargetEventName;
        target.TargetEventBuyIn = source.TargetEventBuyIn;
        target.TicketWon = source.TicketWon;
        target.Qualified = source.Qualified;
        target.TicketConvertedRealized = source.TicketConvertedRealized;
        target.WsopExpressStepNumber = source.WsopExpressStepNumber;
        target.TicketUsedValue = source.TicketUsedValue;
        target.TargetPackageEvent = source.TargetPackageEvent;
        target.Placement = source.Placement;
        target.FieldSize = source.FieldSize;
        target.ITM = source.ITM;
        target.FinalTable = source.FinalTable;
        target.BankrollBefore = source.BankrollBefore;
        target.BankrollAfter = source.BankrollAfter;
        target.PreGameFocus = source.PreGameFocus;
        target.Tags = source.Tags;
        target.MistakeLesson = source.MistakeLesson;
        target.Notes = source.Notes;
    }

    private sealed record TournamentPresetListItem(TournamentPreset Preset, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
