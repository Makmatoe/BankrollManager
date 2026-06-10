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
            Padding = new Padding(14, 0, 14, 1),
            BackColor = PanelRaised,
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
        return new Panel
        {
            BackColor = Panel,
            Padding = new Padding(12),
            Margin = new Padding(6),
            BorderStyle = BorderStyle.FixedSingle
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
        grid.BackgroundColor = Back;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PanelAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = SubHeaderFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.RowHeadersDefaultCellStyle.BackColor = Panel;
        grid.RowHeadersDefaultCellStyle.ForeColor = Muted;
        grid.DefaultCellStyle.BackColor = Panel;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.SelectionBackColor = SelectionBack;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = AlternatingRow;
        grid.Font = BodyFont;
        grid.RowHeadersWidth = 24;
        grid.RowTemplate.MinimumHeight = 32;
        grid.AllowUserToResizeRows = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
    }

    private static void ApplyPalette(bool useDark)
    {
        if (useDark)
        {
            Back = Color.FromArgb(14, 18, 24);
            Panel = Color.FromArgb(24, 30, 38);
            PanelAlt = Color.FromArgb(32, 40, 51);
            PanelRaised = Color.FromArgb(38, 48, 60);
            Border = Color.FromArgb(55, 66, 80);
            Text = Color.FromArgb(240, 244, 249);
            Muted = Color.FromArgb(150, 160, 173);
            Accent = Color.FromArgb(74, 183, 145);
            Warning = Color.FromArgb(245, 166, 35);
            Danger = Color.FromArgb(236, 92, 92);
            Positive = Color.FromArgb(74, 214, 135);
            Negative = Color.FromArgb(255, 104, 104);
            SelectionBack = Color.FromArgb(51, 82, 105);
            AlternatingRow = Color.FromArgb(23, 28, 36);
            ButtonHover = Color.FromArgb(50, 63, 78);
            ButtonDown = Color.FromArgb(60, 78, 94);
            CommandPrimary = Color.FromArgb(35, 91, 76);
            CommandPrimaryHover = Color.FromArgb(42, 111, 92);
            CommandPrimaryDown = Color.FromArgb(38, 126, 100);
            CommandPrimaryBorder = Color.FromArgb(54, 133, 110);
            CommandDanger = Color.FromArgb(58, 34, 40);
            CommandDangerHover = Color.FromArgb(78, 40, 46);
            CommandDangerDown = Color.FromArgb(97, 45, 51);
            CommandDangerBorder = Color.FromArgb(91, 52, 58);
            CommandNeutralHover = Color.FromArgb(46, 58, 72);
            CommandNeutralDown = Color.FromArgb(58, 72, 88);
            CommandNeutralBorder = Color.FromArgb(79, 95, 114);
            PositiveSurface = Color.FromArgb(24, 47, 39);
            AccentSurface = Color.FromArgb(24, 45, 42);
            WarningSurface = Color.FromArgb(55, 43, 22);
            NegativeSurface = Color.FromArgb(57, 28, 31);
            DangerSurface = Color.FromArgb(68, 30, 35);
            ChartGrid = Color.FromArgb(44, 52, 64);
            ChartZeroLine = Color.FromArgb(76, 87, 103);
            return;
        }

        Back = Color.FromArgb(244, 247, 250);
        Panel = Color.White;
        PanelAlt = Color.FromArgb(235, 240, 246);
        PanelRaised = Color.FromArgb(225, 232, 240);
        Border = Color.FromArgb(197, 207, 219);
        Text = Color.FromArgb(30, 41, 59);
        Muted = Color.FromArgb(100, 116, 139);
        Accent = Color.FromArgb(14, 135, 99);
        Warning = Color.FromArgb(181, 118, 23);
        Danger = Color.FromArgb(194, 65, 65);
        Positive = Color.FromArgb(21, 128, 61);
        Negative = Color.FromArgb(220, 38, 38);
        SelectionBack = Color.FromArgb(212, 232, 242);
        AlternatingRow = Color.FromArgb(249, 251, 253);
        ButtonHover = Color.FromArgb(214, 222, 232);
        ButtonDown = Color.FromArgb(199, 210, 224);
        CommandPrimary = Color.FromArgb(212, 241, 231);
        CommandPrimaryHover = Color.FromArgb(190, 229, 216);
        CommandPrimaryDown = Color.FromArgb(165, 214, 198);
        CommandPrimaryBorder = Color.FromArgb(100, 176, 149);
        CommandDanger = Color.FromArgb(252, 226, 226);
        CommandDangerHover = Color.FromArgb(246, 206, 206);
        CommandDangerDown = Color.FromArgb(239, 184, 184);
        CommandDangerBorder = Color.FromArgb(214, 119, 119);
        CommandNeutralHover = Color.FromArgb(222, 230, 239);
        CommandNeutralDown = Color.FromArgb(206, 217, 229);
        CommandNeutralBorder = Color.FromArgb(154, 169, 188);
        PositiveSurface = Color.FromArgb(220, 246, 230);
        AccentSurface = Color.FromArgb(218, 244, 237);
        WarningSurface = Color.FromArgb(255, 239, 207);
        NegativeSurface = Color.FromArgb(255, 224, 224);
        DangerSurface = Color.FromArgb(255, 216, 216);
        ChartGrid = Color.FromArgb(213, 222, 232);
        ChartZeroLine = Color.FromArgb(159, 172, 188);
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
