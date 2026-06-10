using System.Drawing.Drawing2D;

namespace BankrollManager.App.Controls;

public sealed class KpiCard : Control
{
    private string _title = string.Empty;
    private string _value = "-";
    private Color _accentColor = Theme.Text;

    public KpiCard()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Panel;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;
        Size = new Size(202, 92);
        MinimumSize = new Size(170, 84);
        Margin = new Padding(6);
    }

    public void SetData(string title, string value, decimal signValue)
    {
        _title = title;
        _value = value;
        _accentColor = signValue < 0m ? Theme.Negative : signValue > 0m ? Theme.Positive : Theme.Text;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        using var background = new SolidBrush(Theme.Panel);
        using var border = new Pen(Theme.Border);
        using var path = RoundedRect(bounds, 8);
        e.Graphics.FillPath(background, path);
        e.Graphics.DrawPath(border, path);

        using var accent = new SolidBrush(_accentColor);
        e.Graphics.FillRectangle(accent, 0, 14, 4, Height - 28);

        using var titleBrush = new SolidBrush(Theme.Muted);
        using var valueBrush = new SolidBrush(_accentColor);
        using var titleFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var valueFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
            LineAlignment = StringAlignment.Center
        };

        var titleRect = new RectangleF(16, 14, Width - 28, 18);
        var valueRect = new RectangleF(16, 36, Width - 28, Height - 44);
        e.Graphics.DrawString(_title, Theme.SmallFont, titleBrush, titleRect, titleFormat);
        using var valueFont = FitValueFont(e.Graphics, _value, valueRect.Size);
        e.Graphics.DrawString(_value, valueFont, valueBrush, valueRect, valueFormat);
    }

    private static Font FitValueFont(Graphics graphics, string value, SizeF size)
    {
        var preferred = 13.5f;
        for (var fontSize = preferred; fontSize >= 9f; fontSize -= 0.5f)
        {
            var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
            var measured = graphics.MeasureString(value, font);
            if (measured.Width <= size.Width && measured.Height <= size.Height)
            {
                return font;
            }

            font.Dispose();
        }

        return new Font("Segoe UI", 9f, FontStyle.Bold);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
