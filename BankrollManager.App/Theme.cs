using BankrollManager.Core.Models;
using BankrollManager.App.Controls;
using Microsoft.Win32;

namespace BankrollManager.App;

internal static class Theme
{
    public static AppearanceMode RequestedMode { get; private set; } = AppearanceMode.Dark;
    public static bool IsDark { get; private set; } = true;

    public static Color Back { get; private set; } = Color.FromArgb(14, 18, 24);
    public static Color Panel { get; private set; } = Color.FromArgb(24, 30, 38);
    public static Color PanelAlt { get; private set; } = Color.FromArgb(32, 40, 51);
    public static Color PanelRaised { get; private set; } = Color.FromArgb(38, 48, 60);
    public static Color Border { get; private set; } = Color.FromArgb(55, 66, 80);
    public static Color Text { get; private set; } = Color.FromArgb(240, 244, 249);
    public static Color Muted { get; private set; } = Color.FromArgb(150, 160, 173);
    public static Color Accent { get; private set; } = Color.FromArgb(74, 183, 145);
    public static Color Warning { get; private set; } = Color.FromArgb(245, 166, 35);
    public static Color Danger { get; private set; } = Color.FromArgb(236, 92, 92);
    public static Color Positive { get; private set; } = Color.FromArgb(74, 214, 135);
    public static Color Negative { get; private set; } = Color.FromArgb(255, 104, 104);
    public static Color SelectionBack { get; private set; } = Color.FromArgb(51, 82, 105);
    public static Color AlternatingRow { get; private set; } = Color.FromArgb(23, 28, 36);
    public static Color ButtonHover { get; private set; } = Color.FromArgb(50, 63, 78);
    public static Color ButtonDown { get; private set; } = Color.FromArgb(60, 78, 94);
    public static Color CommandPrimary { get; private set; } = Color.FromArgb(35, 91, 76);
    public static Color CommandPrimaryHover { get; private set; } = Color.FromArgb(42, 111, 92);
    public static Color CommandPrimaryDown { get; private set; } = Color.FromArgb(38, 126, 100);
    public static Color CommandPrimaryBorder { get; private set; } = Color.FromArgb(54, 133, 110);
    public static Color CommandDanger { get; private set; } = Color.FromArgb(58, 34, 40);
    public static Color CommandDangerHover { get; private set; } = Color.FromArgb(78, 40, 46);
    public static Color CommandDangerDown { get; private set; } = Color.FromArgb(97, 45, 51);
    public static Color CommandDangerBorder { get; private set; } = Color.FromArgb(91, 52, 58);
    public static Color CommandNeutralHover { get; private set; } = Color.FromArgb(46, 58, 72);
    public static Color CommandNeutralDown { get; private set; } = Color.FromArgb(58, 72, 88);
    public static Color CommandNeutralBorder { get; private set; } = Color.FromArgb(79, 95, 114);
    public static Color PositiveSurface { get; private set; } = Color.FromArgb(24, 47, 39);
    public static Color AccentSurface { get; private set; } = Color.FromArgb(24, 45, 42);
    public static Color WarningSurface { get; private set; } = Color.FromArgb(55, 43, 22);
    public static Color NegativeSurface { get; private set; } = Color.FromArgb(57, 28, 31);
    public static Color DangerSurface { get; private set; } = Color.FromArgb(68, 30, 35);
    public static Color ChartGrid { get; private set; } = Color.FromArgb(44, 52, 64);
    public static Color ChartZeroLine { get; private set; } = Color.FromArgb(76, 87, 103);

    public static Font HeaderFont => new("Segoe UI", 16f, FontStyle.Bold);
    public static Font SubHeaderFont => new("Segoe UI", 10.5f, FontStyle.Bold);
    public static Font BodyFont => new("Segoe UI", 9.25f, FontStyle.Regular);
    public static Font SmallFont => new("Segoe UI", 8.25f, FontStyle.Regular);
    public const int ButtonHeight = 40;
    public const int ControlHeight = 34;

    public static bool Configure(AppearanceMode requestedMode)
    {
        var useDark = requestedMode switch
        {
            AppearanceMode.Light => false,
            AppearanceMode.Dark => true,
            _ => SystemPrefersDark()
        };

        var paletteChanged = IsDark != useDark;
        RequestedMode = requestedMode;
        IsDark = useDark;
        ApplyPalette(useDark);
        return paletteChanged;
    }

    public static Button Button(string text)
    {
        return new UiButton
        {
            Text = text,
            AutoSize = true,
            Height = ButtonHeight,
            MinimumSize = new Size(0, ButtonHeight),
            Padding = new Padding(16, 0, 16, 1),
            BackColor = PanelAlt,
            ForeColor = Text,
            Font = BodyFont,
            Margin = new Padding(4)
        };
    }

    public static Label Label(string text, Font? font = null, Color? color = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = color ?? Text,
            Font = font ?? BodyFont,
            Margin = new Padding(4)
        };
    }

    public static Panel Card()
    {
        return new CardPanel
        {
            BackColor = Panel,
            Padding = new Padding(14),
            Margin = new Padding(6)
        };
    }

    public static TextBox TextBox(bool multiline = false)
    {
        return new TextBox
        {
            BackColor = PanelAlt,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = BodyFont,
            Multiline = multiline,
            Height = multiline ? 92 : ControlHeight,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };
    }

    public static NumericUpDown MoneyBox(decimal value = 0m, decimal maximum = 1_000_000m)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 2,
            Increment = 0.10m,
            Minimum = -maximum,
            Maximum = maximum,
            Value = Clamp(value, -maximum, maximum),
            BackColor = PanelAlt,
            ForeColor = Text,
            Font = BodyFont,
            Width = 150,
            Height = ControlHeight
        };
    }

    public static NumericUpDown IntBox(int value = 0, int maximum = 1_000_000)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 0,
            Minimum = 0,
            Maximum = maximum,
            Value = Math.Clamp(value, 0, maximum),
            BackColor = PanelAlt,
            ForeColor = Text,
            Font = BodyFont,
            Width = 150,
            Height = ControlHeight
        };
    }

    public static ComboBox EnumBox<TEnum>(TEnum selected, IEnumerable<TEnum>? choices = null) where TEnum : struct, Enum
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = PanelAlt,
            ForeColor = Text,
            FlatStyle = FlatStyle.Flat,
            Font = BodyFont,
            Width = 190,
            Height = ControlHeight
        };
        SetEnumBoxItems(box, choices ?? Enum.GetValues<TEnum>(), selected);
        return box;
    }

    public static void SetEnumBoxItems<TEnum>(
        ComboBox box,
        IEnumerable<TEnum> choices,
        TEnum selected,
        bool includeSelected = true)
        where TEnum : struct, Enum
    {
        var values = choices
            .Where(Enum.IsDefined)
            .Distinct()
            .OrderBy(value => value.ToString() ?? string.Empty, NaturalSortComparer.Instance)
            .ToList();

        if (includeSelected && !values.Contains(selected))
        {
            values.Add(selected);
            values = values
                .OrderBy(value => value.ToString() ?? string.Empty, NaturalSortComparer.Instance)
                .ToList();
        }

        if (values.Count == 0)
        {
            values.Add(selected);
        }

        box.BeginUpdate();
        box.Items.Clear();
        box.Items.AddRange(values.Cast<object>().ToArray());
        box.SelectedItem = values.Contains(selected)
            ? selected
            : values[0];
        box.EndUpdate();
    }

    public static void SelectEnumBoxItem<TEnum>(ComboBox box, TEnum selected, bool includeIfMissing = true)
        where TEnum : struct, Enum
    {
        if (!box.Items.Cast<object>().OfType<TEnum>().Contains(selected))
        {
            if (!includeIfMissing)
            {
                return;
            }

            var values = box.Items.Cast<object>().OfType<TEnum>().Append(selected);
            SetEnumBoxItems(box, values, selected);
            return;
        }

        box.SelectedItem = selected;
    }

    public static DateTimePicker DatePicker(DateOnly value)
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd-MM-yyyy",
            Value = value.ToDateTime(TimeOnly.MinValue),
            CalendarMonthBackground = PanelAlt,
            CalendarForeColor = Text,
            Font = BodyFont,
            Width = 180,
            Height = ControlHeight
        };
    }

    public static DateTimePicker TimePicker(TimeOnly value)
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Value = DateTime.Today.Add(value.ToTimeSpan()),
            CalendarMonthBackground = PanelAlt,
            CalendarForeColor = Text,
            Font = BodyFont,
            Width = 110,
            Height = ControlHeight
        };
    }

    public static void ApplyGrid(DataGridView grid)
    {
        grid.BackgroundColor = Panel;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PanelRaised;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = SubHeaderFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 38;
        grid.RowHeadersDefaultCellStyle.BackColor = Panel;
        grid.RowHeadersDefaultCellStyle.ForeColor = Muted;
        grid.RowHeadersVisible = false;
        grid.DefaultCellStyle.BackColor = Panel;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.SelectionBackColor = SelectionBack;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = AlternatingRow;
        grid.Font = BodyFont;
        grid.RowHeadersWidth = 24;
        grid.RowTemplate.MinimumHeight = 34;
        grid.RowTemplate.Height = 34;
        grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
    }

    private static void ApplyPalette(bool useDark)
    {
        if (useDark)
        {
            Back = Color.FromArgb(18, 20, 22);
            Panel = Color.FromArgb(29, 32, 35);
            PanelAlt = Color.FromArgb(37, 41, 45);
            PanelRaised = Color.FromArgb(45, 50, 55);
            Border = Color.FromArgb(62, 69, 74);
            Text = Color.FromArgb(239, 242, 236);
            Muted = Color.FromArgb(162, 169, 162);
            Accent = Color.FromArgb(83, 184, 137);
            Warning = Color.FromArgb(225, 169, 70);
            Danger = Color.FromArgb(231, 91, 88);
            Positive = Color.FromArgb(93, 211, 141);
            Negative = Color.FromArgb(239, 98, 96);
            SelectionBack = Color.FromArgb(58, 88, 82);
            AlternatingRow = Color.FromArgb(24, 27, 30);
            ButtonHover = Color.FromArgb(51, 57, 62);
            ButtonDown = Color.FromArgb(60, 70, 70);
            CommandPrimary = Color.FromArgb(38, 94, 72);
            CommandPrimaryHover = Color.FromArgb(45, 114, 87);
            CommandPrimaryDown = Color.FromArgb(47, 133, 99);
            CommandPrimaryBorder = Color.FromArgb(75, 151, 116);
            CommandDanger = Color.FromArgb(70, 37, 40);
            CommandDangerHover = Color.FromArgb(91, 43, 47);
            CommandDangerDown = Color.FromArgb(111, 48, 52);
            CommandDangerBorder = Color.FromArgb(126, 62, 67);
            CommandNeutralHover = Color.FromArgb(53, 59, 64);
            CommandNeutralDown = Color.FromArgb(61, 69, 74);
            CommandNeutralBorder = Color.FromArgb(86, 95, 101);
            PositiveSurface = Color.FromArgb(27, 53, 39);
            AccentSurface = Color.FromArgb(27, 50, 43);
            WarningSurface = Color.FromArgb(58, 45, 22);
            NegativeSurface = Color.FromArgb(63, 31, 34);
            DangerSurface = Color.FromArgb(76, 32, 36);
            ChartGrid = Color.FromArgb(47, 53, 58);
            ChartZeroLine = Color.FromArgb(86, 96, 98);
            return;
        }

        Back = Color.FromArgb(246, 247, 244);
        Panel = Color.FromArgb(255, 255, 252);
        PanelAlt = Color.FromArgb(239, 242, 238);
        PanelRaised = Color.FromArgb(231, 236, 230);
        Border = Color.FromArgb(204, 213, 205);
        Text = Color.FromArgb(30, 36, 31);
        Muted = Color.FromArgb(91, 101, 94);
        Accent = Color.FromArgb(34, 137, 101);
        Warning = Color.FromArgb(174, 115, 25);
        Danger = Color.FromArgb(194, 65, 62);
        Positive = Color.FromArgb(28, 128, 73);
        Negative = Color.FromArgb(202, 58, 56);
        SelectionBack = Color.FromArgb(216, 235, 224);
        AlternatingRow = Color.FromArgb(250, 251, 248);
        ButtonHover = Color.FromArgb(222, 229, 222);
        ButtonDown = Color.FromArgb(207, 218, 208);
        CommandPrimary = Color.FromArgb(210, 239, 225);
        CommandPrimaryHover = Color.FromArgb(188, 226, 207);
        CommandPrimaryDown = Color.FromArgb(166, 211, 190);
        CommandPrimaryBorder = Color.FromArgb(94, 171, 136);
        CommandDanger = Color.FromArgb(252, 226, 224);
        CommandDangerHover = Color.FromArgb(246, 205, 203);
        CommandDangerDown = Color.FromArgb(239, 184, 181);
        CommandDangerBorder = Color.FromArgb(211, 116, 113);
        CommandNeutralHover = Color.FromArgb(224, 231, 224);
        CommandNeutralDown = Color.FromArgb(209, 219, 210);
        CommandNeutralBorder = Color.FromArgb(150, 165, 153);
        PositiveSurface = Color.FromArgb(220, 246, 229);
        AccentSurface = Color.FromArgb(218, 242, 231);
        WarningSurface = Color.FromArgb(255, 238, 205);
        NegativeSurface = Color.FromArgb(255, 225, 223);
        DangerSurface = Color.FromArgb(255, 216, 214);
        ChartGrid = Color.FromArgb(214, 223, 215);
        ChartZeroLine = Color.FromArgb(156, 170, 159);
    }

    private static bool SystemPrefersDark()
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value switch
            {
                int intValue => intValue == 0,
                string text when int.TryParse(text, out var intValue) => intValue == 0,
                _ => true
            };
        }
        catch
        {
            return true;
        }
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

}
