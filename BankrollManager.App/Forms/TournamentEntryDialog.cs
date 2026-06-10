using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class TournamentEntryDialog : Form
{
    private readonly BankrollSettings _settings;
    private readonly DateTimePicker _date;
    private readonly DateTimePicker _registrationTime;
    private readonly ComboBox _status;
    private readonly DateTimePicker _finishedDate;
    private readonly DateTimePicker _finishedTime;
    private readonly ComboBox _platform;
    private readonly ComboBox _category;
    private readonly ComboBox _format;
    private readonly TextBox _eventName;
    private readonly TextBox _currency;
    private readonly ComboBox _eventTag;
    private readonly CheckBox _isPromoFreebieTicketEvent;
    private readonly NumericUpDown _buyIn;
    private readonly NumericUpDown _feeRake;
    private readonly NumericUpDown _plannedBullets;
    private readonly NumericUpDown _actualBullets;
    private readonly NumericUpDown _addOnsRebuys;
    private readonly NumericUpDown _bountyTicketValue;
    private readonly NumericUpDown _ticketBuyInValue;
    private readonly ComboBox _ticketBuyInPlatform;
    private readonly NumericUpDown _ticketValueWon;
    private readonly NumericUpDown _cashPrize;
    private readonly NumericUpDown _tournamentDollarsWon;
    private readonly NumericUpDown _cashDollarsWon;
    private readonly NumericUpDown _regularCashPrize;
    private readonly NumericUpDown _mysteryBountyPrize;
    private readonly CheckBox _bountyPhaseReached;
    private readonly NumericUpDown _knockoutsAfterBountyPhase;
    private readonly TextBox _mysteryBountyNotes;
    private readonly NumericUpDown _bountyPrize;
    private readonly NumericUpDown _knockouts;
    private readonly NumericUpDown _spinPlayerCount;
    private readonly CheckBox _insuranceUsed;
    private readonly NumericUpDown _insuranceCost;
    private readonly NumericUpDown _multiplierHit;
    private readonly NumericUpDown _prizeWon;
    private readonly NumericUpDown _flipBuyInPerStack;
    private readonly NumericUpDown _flipStacksBought;
    private readonly CheckBox _flipPhaseWon;
    private readonly CheckBox _goPhaseReached;
    private readonly CheckBox _rushStageSurvived;
    private readonly CheckBox _battleRoyaleFinalTableReached;
    private readonly TextBox _targetEventName;
    private readonly NumericUpDown _targetEventBuyIn;
    private readonly CheckBox _ticketWon;
    private readonly CheckBox _qualified;
    private readonly CheckBox _ticketConvertedRealized;
    private readonly NumericUpDown _wsopExpressStepNumber;
    private readonly NumericUpDown _ticketUsedValue;
    private readonly TextBox _targetPackageEvent;
    private readonly Label _ggWarning;
    private readonly NumericUpDown _placement;
    private readonly NumericUpDown _fieldSize;
    private readonly CheckBox _itm;
    private readonly CheckBox _finalTable;
    private readonly TextBox _preGameFocus;
    private readonly TextBox _tags;
    private readonly TextBox _mistakeLesson;
    private readonly TextBox _notes;
    private readonly List<DialogLayout.Row> _ggRows = [];
    private readonly List<Control> _finishedOnlyGgControls = [];

    public TournamentEntryDialog(TournamentEntry entry, BankrollSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();
        Entry = Clone(entry);
        Text = Entry.Id == Guid.Empty ? "Add Tournament" : "Tournament Entry";
        Size = new Size(700, 760);
        MinimumSize = new Size(640, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var layout = DialogLayout.Create(this, Save);
        _date = Theme.DatePicker(Entry.Date);
        _registrationTime = Theme.TimePicker(Entry.RegistrationTime ?? TimeOnly.FromDateTime(DateTime.Now));
        _status = Theme.EnumBox(Entry.Status);
        _finishedDate = Theme.DatePicker(Entry.FinishedDate ?? Entry.Date);
        _finishedTime = Theme.TimePicker(Entry.FinishedTime ?? Entry.RegistrationTime ?? TimeOnly.FromDateTime(DateTime.Now));
        _platform = Theme.EnumBox(Entry.Platform, PlatformCatalog.EnabledPlatforms(_settings, Entry.Platform));
        _category = Theme.EnumBox(Entry.Category, PlatformCatalog.TournamentCategoriesFor(Entry.Platform));
        _format = Theme.EnumBox(Entry.Format, PlatformCatalog.TournamentFormatsFor(Entry.Platform));
        _eventName = Theme.TextBox();
        _eventName.Text = Entry.EventName;
        _currency = Theme.TextBox();
        _currency.Text = Entry.Currency;
        _eventTag = Theme.EnumBox(Entry.EventTag);
        _isPromoFreebieTicketEvent = CheckBox(Entry.IsPromoFreebieTicketEvent);
        _buyIn = Theme.MoneyBox(Entry.BuyIn);
        _feeRake = Theme.MoneyBox(Entry.FeeRake);
        _plannedBullets = Theme.IntBox(Entry.PlannedBullets);
        _actualBullets = Theme.IntBox(Entry.ActualBullets);
        _addOnsRebuys = Theme.MoneyBox(Entry.AddOnsRebuys);
        _bountyTicketValue = Theme.MoneyBox(Entry.BountyTicketValue);
        _ticketBuyInValue = Theme.MoneyBox(Entry.TicketBuyInValue);
        _ticketBuyInPlatform = Theme.EnumBox(
            Entry.EffectiveTicketBuyInPlatform,
            PlatformCatalog.EnabledPlatforms(_settings, Entry.EffectiveTicketBuyInPlatform));
        _ticketValueWon = Theme.MoneyBox(Entry.TicketValueWon);
        _cashPrize = Theme.MoneyBox(Entry.CashPrize);
        _tournamentDollarsWon = Theme.MoneyBox(Entry.TournamentDollarsWon);
        _cashDollarsWon = Theme.MoneyBox(Entry.CashDollarsWon);
        _regularCashPrize = Theme.MoneyBox(Entry.RegularCashPrize);
        _mysteryBountyPrize = Theme.MoneyBox(Entry.MysteryBountyPrize);
        _bountyPhaseReached = CheckBox(Entry.BountyPhaseReached);
        _knockoutsAfterBountyPhase = Theme.IntBox(Entry.KnockoutsAfterBountyPhase ?? 0);
        _mysteryBountyNotes = Theme.TextBox(multiline: true);
        _mysteryBountyNotes.Text = Entry.MysteryBountyNotes;
        _bountyPrize = Theme.MoneyBox(Entry.BountyPrize);
        _knockouts = Theme.IntBox(Entry.Knockouts ?? 0);
        _spinPlayerCount = Theme.IntBox(Entry.SpinPlayerCount ?? 3, 6);
        _insuranceUsed = CheckBox(Entry.InsuranceUsed);
        _insuranceCost = Theme.MoneyBox(Entry.InsuranceCost);
        _multiplierHit = Theme.MoneyBox(Entry.MultiplierHit);
        _prizeWon = Theme.MoneyBox(Entry.PrizeWon);
        _flipBuyInPerStack = Theme.MoneyBox(Entry.FlipBuyInPerStack);
        _flipStacksBought = Theme.IntBox(Entry.FlipStacksBought <= 0 ? 1 : Entry.FlipStacksBought, 99);
        _flipPhaseWon = CheckBox(Entry.FlipPhaseWon);
        _goPhaseReached = CheckBox(Entry.GoPhaseReached);
        _rushStageSurvived = CheckBox(Entry.RushStageSurvived);
        _battleRoyaleFinalTableReached = CheckBox(Entry.BattleRoyaleFinalTableReached);
        _targetEventName = Theme.TextBox();
        _targetEventName.Text = Entry.TargetEventName;
        _targetEventBuyIn = Theme.MoneyBox(Entry.TargetEventBuyIn);
        _ticketWon = CheckBox(Entry.TicketWon);
        _qualified = CheckBox(Entry.Qualified);
        _ticketConvertedRealized = CheckBox(Entry.TicketConvertedRealized);
        _wsopExpressStepNumber = Theme.IntBox(Entry.WsopExpressStepNumber ?? 0, 99);
        _ticketUsedValue = Theme.MoneyBox(Entry.TicketUsedValue);
        _targetPackageEvent = Theme.TextBox();
        _targetPackageEvent.Text = Entry.TargetPackageEvent;
        _ggWarning = Theme.Label(string.Empty, Theme.SubHeaderFont, Theme.Warning);
        _ggWarning.MaximumSize = new Size(460, 0);
        _placement = Theme.IntBox(Entry.Placement ?? 0);
        _fieldSize = Theme.IntBox(Entry.FieldSize ?? 0);
        _itm = new CheckBox { Checked = Entry.ITM, ForeColor = Theme.Text, AutoSize = true };
        _finalTable = new CheckBox { Checked = Entry.FinalTable, ForeColor = Theme.Text, AutoSize = true };
        _preGameFocus = Theme.TextBox();
        _preGameFocus.Text = Entry.PreGameFocus;
        _tags = Theme.TextBox();
        _tags.Text = Entry.Tags;
        _mistakeLesson = Theme.TextBox(multiline: true);
        _mistakeLesson.Text = Entry.MistakeLesson;
        _notes = Theme.TextBox(multiline: true);
        _notes.Text = Entry.Notes;

        DialogLayout.AddRow(layout, "Date", _date);
        DialogLayout.AddRow(layout, "Registration time", _registrationTime);
        DialogLayout.AddRow(layout, "Status", _status);
        DialogLayout.AddRow(layout, "Finished date", _finishedDate);
        DialogLayout.AddRow(layout, "Finished time", _finishedTime);
        DialogLayout.AddRow(layout, "Platform", _platform);
        DialogLayout.AddRow(layout, "Category", _category);
        DialogLayout.AddRow(layout, "Format", _format);
        DialogLayout.AddRow(layout, "Tournament/Event", _eventName);
        AddGgRow(layout, "Currency", _currency);
        AddGgRow(layout, "Event tag", _eventTag);
        AddGgRow(layout, "Promo/freebie/ticket", _isPromoFreebieTicketEvent);
        DialogLayout.AddRow(layout, "Buy-in", _buyIn);
        AddGgRow(layout, "Fee/Rake", _feeRake);
        DialogLayout.AddRow(layout, "Planned bullets", _plannedBullets);
        DialogLayout.AddRow(layout, "Actual bullets", _actualBullets);
        DialogLayout.AddRow(layout, "Add-ons/Rebuys", _addOnsRebuys);
        DialogLayout.AddRow(layout, "Bounty cash", _bountyTicketValue);
        DialogLayout.AddRow(layout, "Ticket buy-in", _ticketBuyInValue);
        DialogLayout.AddRow(layout, "Ticket platform", _ticketBuyInPlatform);
        DialogLayout.AddRow(layout, "Ticket value won", _ticketValueWon);
        DialogLayout.AddRow(layout, "Cash prize", _cashPrize);
        AddGgRow(layout, "Tournament $ won", _tournamentDollarsWon, finishedOnly: true);
        AddGgRow(layout, "Cash $ won", _cashDollarsWon, finishedOnly: true);
        AddGgRow(layout, "Regular cash prize", _regularCashPrize, finishedOnly: true);
        AddGgRow(layout, "Mystery bounty prize", _mysteryBountyPrize, finishedOnly: true);
        AddGgRow(layout, "Bounty phase reached", _bountyPhaseReached, finishedOnly: true);
        AddGgRow(layout, "KOs after bounty", _knockoutsAfterBountyPhase, finishedOnly: true);
        AddGgRow(layout, "Mystery bounty notes", _mysteryBountyNotes, finishedOnly: true);
        AddGgRow(layout, "Bounty prize", _bountyPrize, finishedOnly: true);
        AddGgRow(layout, "Knockouts", _knockouts, finishedOnly: true);
        AddGgRow(layout, "Spin players", _spinPlayerCount);
        AddGgRow(layout, "Insurance used", _insuranceUsed);
        AddGgRow(layout, "Insurance cost", _insuranceCost);
        AddGgRow(layout, "Multiplier hit", _multiplierHit, finishedOnly: true);
        AddGgRow(layout, "Prize won", _prizeWon, finishedOnly: true);
        AddGgRow(layout, "Buy-in per stack", _flipBuyInPerStack);
        AddGgRow(layout, "Stacks bought", _flipStacksBought);
        AddGgRow(layout, "Flip phase won", _flipPhaseWon, finishedOnly: true);
        AddGgRow(layout, "Go phase reached", _goPhaseReached, finishedOnly: true);
        AddGgRow(layout, "Rush stage survived", _rushStageSurvived, finishedOnly: true);
        AddGgRow(layout, "Battle Royale FT", _battleRoyaleFinalTableReached, finishedOnly: true);
        AddGgRow(layout, "Target event", _targetEventName);
        AddGgRow(layout, "Target buy-in", _targetEventBuyIn);
        AddGgRow(layout, "Ticket won", _ticketWon, finishedOnly: true);
        AddGgRow(layout, "Qualified", _qualified, finishedOnly: true);
        AddGgRow(layout, "Ticket realized", _ticketConvertedRealized, finishedOnly: true);
        AddGgRow(layout, "WSOP step", _wsopExpressStepNumber);
        AddGgRow(layout, "Ticket used", _ticketUsedValue);
        AddGgRow(layout, "Package/event", _targetPackageEvent);
        AddGgRow(layout, "GGPoker warning", _ggWarning);
        DialogLayout.AddRow(layout, "Placement (0 unknown)", _placement);
        DialogLayout.AddRow(layout, "Field size (0 unknown)", _fieldSize);
        DialogLayout.AddRow(layout, "ITM", _itm);
        DialogLayout.AddRow(layout, "Final table", _finalTable);
        DialogLayout.AddRow(layout, "Pre-game focus", _preGameFocus);
        DialogLayout.AddRow(layout, "Tags", _tags);
        DialogLayout.AddRow(layout, "Mistake/Lesson", _mistakeLesson);
        DialogLayout.AddRow(layout, "Notes", _notes);
        _status.SelectedIndexChanged += (_, _) => UpdateStatusControls();
        _platform.SelectedIndexChanged += (_, _) =>
        {
            UpdatePlatformScopedChoices(includeCurrent: false);
            UpdateGgControls();
        };
        _format.SelectedIndexChanged += (_, _) => UpdateGgControls();
        _insuranceUsed.CheckedChanged += (_, _) => UpdateGgControls();
        _flipStacksBought.ValueChanged += (_, _) => UpdateGgControls();
        UpdatePlatformScopedChoices(includeCurrent: true);
        UpdateStatusControls();
        UpdateGgControls();
    }

    public TournamentEntry Entry { get; private set; }

    private void Save()
    {
        Entry.Date = DateOnly.FromDateTime(_date.Value);
        Entry.RegistrationTime = TimeOnly.FromDateTime(_registrationTime.Value);
        Entry.Status = (TournamentStatus)_status.SelectedItem!;
        Entry.FinishedDate = Entry.Status == TournamentStatus.Finished ? DateOnly.FromDateTime(_finishedDate.Value) : null;
        Entry.FinishedTime = Entry.Status == TournamentStatus.Finished ? TimeOnly.FromDateTime(_finishedTime.Value) : null;
        Entry.Platform = (Platform)_platform.SelectedItem!;
        Entry.Category = (TournamentCategory)_category.SelectedItem!;
        Entry.Format = (TournamentFormat)_format.SelectedItem!;
        Entry.EventName = _eventName.Text.Trim();
        Entry.Currency = _currency.Text.Trim();
        Entry.EventTag = (EventTag)_eventTag.SelectedItem!;
        Entry.IsPromoFreebieTicketEvent = _isPromoFreebieTicketEvent.Checked;
        Entry.BuyIn = _buyIn.Value;
        Entry.FeeRake = _feeRake.Value;
        Entry.PlannedBullets = (int)_plannedBullets.Value;
        Entry.ActualBullets = (int)_actualBullets.Value;
        Entry.AddOnsRebuys = _addOnsRebuys.Value;
        Entry.BountyTicketValue = _bountyTicketValue.Value;
        Entry.TicketBuyInValue = _ticketBuyInValue.Value;
        Entry.TicketBuyInPlatform = Entry.TicketBuyInValue > 0m ? (Platform)_ticketBuyInPlatform.SelectedItem! : null;
        Entry.TicketValueWon = _ticketValueWon.Value;
        Entry.CashPrize = _cashPrize.Value;
        Entry.TournamentDollarsWon = _tournamentDollarsWon.Value;
        Entry.CashDollarsWon = _cashDollarsWon.Value;
        Entry.RegularCashPrize = _regularCashPrize.Value;
        Entry.MysteryBountyPrize = _mysteryBountyPrize.Value;
        Entry.BountyPhaseReached = _bountyPhaseReached.Checked;
        Entry.KnockoutsAfterBountyPhase = NullableInt(_knockoutsAfterBountyPhase);
        Entry.MysteryBountyNotes = _mysteryBountyNotes.Text.Trim();
        Entry.BountyPrize = _bountyPrize.Value;
        Entry.Knockouts = NullableInt(_knockouts);
        Entry.SpinPlayerCount = NullableInt(_spinPlayerCount);
        Entry.InsuranceUsed = _insuranceUsed.Checked;
        Entry.InsuranceCost = _insuranceCost.Value;
        Entry.MultiplierHit = _multiplierHit.Value;
        Entry.PrizeWon = _prizeWon.Value;
        Entry.FlipBuyInPerStack = _flipBuyInPerStack.Value;
        Entry.FlipStacksBought = (int)_flipStacksBought.Value;
        Entry.FlipPhaseWon = _flipPhaseWon.Checked;
        Entry.GoPhaseReached = _goPhaseReached.Checked;
        Entry.RushStageSurvived = _rushStageSurvived.Checked;
        Entry.BattleRoyaleFinalTableReached = _battleRoyaleFinalTableReached.Checked;
        Entry.TargetEventName = _targetEventName.Text.Trim();
        Entry.TargetEventBuyIn = _targetEventBuyIn.Value;
        Entry.TicketWon = _ticketWon.Checked;
        Entry.Qualified = _qualified.Checked;
        Entry.TicketConvertedRealized = _ticketConvertedRealized.Checked;
        Entry.WsopExpressStepNumber = NullableInt(_wsopExpressStepNumber);
        Entry.TicketUsedValue = _ticketUsedValue.Value;
        Entry.TargetPackageEvent = _targetPackageEvent.Text.Trim();
        Entry.Placement = NullableInt(_placement);
        Entry.FieldSize = NullableInt(_fieldSize);
        Entry.ITM = _itm.Checked;
        Entry.FinalTable = _finalTable.Checked;
        Entry.PreGameFocus = _preGameFocus.Text.Trim();
        Entry.Tags = _tags.Text.Trim();
        Entry.MistakeLesson = _mistakeLesson.Text.Trim();
        Entry.Notes = _notes.Text.Trim();

        var errors = EntryValidator.Validate(Entry);
        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static int? NullableInt(NumericUpDown box)
    {
        return box.Value <= 0m ? null : (int)box.Value;
    }

    private void UpdateStatusControls()
    {
        var isFinished = _status.SelectedItem is TournamentStatus status && status == TournamentStatus.Finished;
        _finishedDate.Enabled = isFinished;
        _finishedTime.Enabled = isFinished;
        _bountyTicketValue.Enabled = isFinished;
        _ticketValueWon.Enabled = isFinished;
        _cashPrize.Enabled = isFinished;
        _placement.Enabled = isFinished;
        _fieldSize.Enabled = isFinished;
        _itm.Enabled = isFinished;
        _finalTable.Enabled = isFinished;
        foreach (var control in _finishedOnlyGgControls)
        {
            control.Enabled = isFinished;
        }

        UpdateGgControls();
    }

    private static CheckBox CheckBox(bool isChecked)
    {
        return new CheckBox { Checked = isChecked, ForeColor = Theme.Text, AutoSize = true };
    }

    private void AddGgRow(TableLayoutPanel layout, string label, Control control, bool finishedOnly = false)
    {
        _ggRows.Add(DialogLayout.AddRow(layout, label, control));
        if (finishedOnly)
        {
            _finishedOnlyGgControls.Add(control);
        }
    }

    private void UpdateGgControls()
    {
        if (_platform.SelectedItem is not Platform platform || _format.SelectedItem is not TournamentFormat format)
        {
            return;
        }

        var showGeneral = platform == Platform.GGPoker || IsGgFormat(format);
        var showMysteryBounty = format == TournamentFormat.MysteryBounty;
        var showPko = format == TournamentFormat.PKO;
        var showSpin = format == TournamentFormat.SpinAndGold;
        var showFlipAndGo = format == TournamentFormat.FlipAndGo;
        var showBattleRoyale = format == TournamentFormat.MysteryBattleRoyale;
        var showSatellite = IsSatelliteFormat(format);
        foreach (var row in _ggRows)
        {
            row.SetVisible(false);
        }

        SetVisible(_currency, showGeneral);
        SetVisible(_eventTag, showGeneral);
        SetVisible(_isPromoFreebieTicketEvent, showGeneral);
        SetVisible(_feeRake, showGeneral || showBattleRoyale);
        SetVisible(_tournamentDollarsWon, showGeneral);
        SetVisible(_cashDollarsWon, showGeneral);
        SetVisible(_ggWarning, showGeneral);

        SetVisible(_regularCashPrize, showMysteryBounty);
        SetVisible(_mysteryBountyPrize, showMysteryBounty || showBattleRoyale);
        SetVisible(_bountyPhaseReached, showMysteryBounty);
        SetVisible(_knockoutsAfterBountyPhase, showMysteryBounty);
        SetVisible(_mysteryBountyNotes, showMysteryBounty);
        SetVisible(_bountyPrize, showPko);
        SetVisible(_knockouts, showPko);

        SetVisible(_spinPlayerCount, showSpin);
        SetVisible(_insuranceUsed, showSpin);
        SetVisible(_insuranceCost, showSpin);
        SetVisible(_multiplierHit, showSpin);
        SetVisible(_prizeWon, showSpin || showFlipAndGo || showBattleRoyale);

        SetVisible(_flipBuyInPerStack, showFlipAndGo);
        SetVisible(_flipStacksBought, showFlipAndGo);
        SetVisible(_flipPhaseWon, showFlipAndGo);
        SetVisible(_goPhaseReached, showFlipAndGo);

        SetVisible(_rushStageSurvived, showBattleRoyale);
        SetVisible(_battleRoyaleFinalTableReached, showBattleRoyale);

        SetVisible(_targetEventName, showSatellite);
        SetVisible(_targetEventBuyIn, showSatellite);
        SetVisible(_ticketWon, showSatellite);
        SetVisible(_qualified, showSatellite);
        SetVisible(_ticketConvertedRealized, showSatellite);
        SetVisible(_wsopExpressStepNumber, format == TournamentFormat.WSOPExpress);
        SetVisible(_ticketUsedValue, format == TournamentFormat.WSOPExpress);
        SetVisible(_targetPackageEvent, format == TournamentFormat.WSOPExpress);

        _ggWarning.Text = BuildGgWarning(format);
        SetVisible(_ggWarning, !string.IsNullOrWhiteSpace(_ggWarning.Text));
    }

    private void UpdatePlatformScopedChoices(bool includeCurrent)
    {
        if (_platform.SelectedItem is not Platform platform)
        {
            return;
        }

        var selectedCategory = _category.SelectedItem is TournamentCategory category
            ? category
            : Entry.Category;
        var selectedFormat = _format.SelectedItem is TournamentFormat format
            ? format
            : Entry.Format;
        var selectedTicketPlatform = _ticketBuyInPlatform.SelectedItem is Platform ticketPlatform
            ? ticketPlatform
            : Entry.EffectiveTicketBuyInPlatform;

        Theme.SetEnumBoxItems(
            _category,
            PlatformCatalog.TournamentCategoriesFor(platform),
            selectedCategory,
            includeCurrent);
        Theme.SetEnumBoxItems(
            _format,
            PlatformCatalog.TournamentFormatsFor(platform),
            selectedFormat,
            includeCurrent);
        Theme.SetEnumBoxItems(
            _ticketBuyInPlatform,
            PlatformCatalog.EnabledPlatforms(_settings, selectedTicketPlatform),
            selectedTicketPlatform);
    }

    private void SetVisible(Control control, bool visible)
    {
        foreach (var row in _ggRows.Where(row => ReferenceEquals(row.Control, control)))
        {
            row.SetVisible(visible);
        }
    }

    private static bool IsGgFormat(TournamentFormat format)
    {
        return format is TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.Freezeout
            or TournamentFormat.ReEntry
            or TournamentFormat.RebuyAddon
            or TournamentFormat.PKO
            or TournamentFormat.MysteryBounty
            or TournamentFormat.SpinAndGold
            or TournamentFormat.FlipAndGo
            or TournamentFormat.MysteryBattleRoyale
            or TournamentFormat.AoFSitAndGo
            or TournamentFormat.GGMasters
            or TournamentFormat.GGMillion
            or TournamentFormat.WSOPExpress;
    }

    private static bool IsSatelliteFormat(TournamentFormat format)
    {
        return format is TournamentFormat.Satellite
            or TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.WSOPExpress;
    }

    private string BuildGgWarning(TournamentFormat format)
    {
        var warnings = new List<string>();
        if (format is TournamentFormat.MysteryBounty or TournamentFormat.MysteryBattleRoyale or TournamentFormat.SpinAndGold or TournamentFormat.FlipAndGo or TournamentFormat.AoFSitAndGo)
        {
            warnings.Add("Extra variance: do not use jackpot/multiplier potential to justify oversized buy-ins.");
        }

        if (format is TournamentFormat.ReEntry or TournamentFormat.RebuyAddon)
        {
            warnings.Add("Re-entry/rebuy formats must use planned maximum cost.");
        }

        if (format == TournamentFormat.FlipAndGo && _flipStacksBought.Value > 1)
        {
            warnings.Add("Flip & Go stacks multiply your real buy-in.");
        }

        if (format == TournamentFormat.SpinAndGold && _insuranceUsed.Checked)
        {
            warnings.Add("Spin & Gold insurance increases total cost.");
        }

        if (IsSatelliteFormat(format))
        {
            warnings.Add("Satellite ticket value is not the same as withdrawable cash.");
        }

        return string.Join(Environment.NewLine, warnings);
    }

    private static TournamentEntry Clone(TournamentEntry entry)
    {
        return new TournamentEntry
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            Date = entry.Date,
            RegistrationTime = entry.RegistrationTime,
            Status = entry.Status,
            FinishedDate = entry.FinishedDate,
            FinishedTime = entry.FinishedTime,
            Platform = entry.Platform,
            Category = entry.Category,
            Format = entry.Format,
            EventName = entry.EventName,
            Currency = entry.Currency,
            EventTag = entry.EventTag,
            IsPromoFreebieTicketEvent = entry.IsPromoFreebieTicketEvent,
            BuyIn = entry.BuyIn,
            FeeRake = entry.FeeRake,
            PlannedBullets = entry.PlannedBullets,
            ActualBullets = entry.ActualBullets,
            AddOnsRebuys = entry.AddOnsRebuys,
            BountyTicketValue = entry.BountyTicketValue,
            TicketBuyInValue = entry.TicketBuyInValue,
            TicketBuyInPlatform = entry.TicketBuyInPlatform,
            TicketValueWon = entry.TicketValueWon,
            CashPrize = entry.CashPrize,
            TournamentDollarsWon = entry.TournamentDollarsWon,
            CashDollarsWon = entry.CashDollarsWon,
            RegularCashPrize = entry.RegularCashPrize,
            MysteryBountyPrize = entry.MysteryBountyPrize,
            BountyPhaseReached = entry.BountyPhaseReached,
            KnockoutsAfterBountyPhase = entry.KnockoutsAfterBountyPhase,
            MysteryBountyNotes = entry.MysteryBountyNotes,
            BountyPrize = entry.BountyPrize,
            Knockouts = entry.Knockouts,
            SpinPlayerCount = entry.SpinPlayerCount,
            InsuranceUsed = entry.InsuranceUsed,
            InsuranceCost = entry.InsuranceCost,
            MultiplierHit = entry.MultiplierHit,
            PrizeWon = entry.PrizeWon,
            FlipBuyInPerStack = entry.FlipBuyInPerStack,
            FlipStacksBought = entry.FlipStacksBought,
            FlipPhaseWon = entry.FlipPhaseWon,
            GoPhaseReached = entry.GoPhaseReached,
            RushStageSurvived = entry.RushStageSurvived,
            BattleRoyaleFinalTableReached = entry.BattleRoyaleFinalTableReached,
            TargetEventName = entry.TargetEventName,
            TargetEventBuyIn = entry.TargetEventBuyIn,
            TicketWon = entry.TicketWon,
            Qualified = entry.Qualified,
            TicketConvertedRealized = entry.TicketConvertedRealized,
            WsopExpressStepNumber = entry.WsopExpressStepNumber,
            TicketUsedValue = entry.TicketUsedValue,
            TargetPackageEvent = entry.TargetPackageEvent,
            Placement = entry.Placement,
            FieldSize = entry.FieldSize,
            ITM = entry.ITM,
            FinalTable = entry.FinalTable,
            RiskPercentageOfBankrollAtRegistration = entry.RiskPercentageOfBankrollAtRegistration,
            RuleCheckResult = entry.RuleCheckResult,
            BankrollBefore = entry.BankrollBefore,
            BankrollAfter = entry.BankrollAfter,
            PreGameFocus = entry.PreGameFocus,
            Tags = entry.Tags,
            MistakeLesson = entry.MistakeLesson,
            Notes = entry.Notes
        };
    }
}
