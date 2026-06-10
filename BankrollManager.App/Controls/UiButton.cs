using System.Drawing.Drawing2D;

namespace BankrollManager.App.Controls;

internal sealed class UiButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public UiButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);
        AutoSize = true;
        FlatStyle = FlatStyle.Flat;
        Height = Theme.ButtonHeight;
        MinimumSize = new Size(0, Theme.ButtonHeight);
        Padding = new Padding(14, 0, 14, 1);
        TextAlign = ContentAlignment.MiddleCenter;
        UseVisualStyleBackColor = false;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var textSize = TextRenderer.MeasureText(Text, Font);
        return new Size(
            Math.Max(MinimumSize.Width, textSize.Width + Padding.Horizontal + 8),
            Math.Max(MinimumSize.Height, Theme.ButtonHeight));
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Back);

        var bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;
        using var path = RoundedRectangle(bounds, 4);

        var background = Enabled
            ? _pressed
                ? Theme.ButtonDown
                : _hovered
                    ? Theme.ButtonHover
                    : BackColor
            : Theme.PanelAlt;
        using var backgroundBrush = new SolidBrush(background);
        using var borderPen = new Pen(Enabled ? Theme.Border : Theme.PanelRaised);
        e.Graphics.FillPath(backgroundBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        var textColor = Enabled ? ForeColor : Theme.Muted;
        var textBounds = new Rectangle(
            Padding.Left,
            0,
            Math.Max(0, Width - Padding.Horizontal),
            Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            textColor,
            TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis
            | TextFormatFlags.NoPrefix);

        if (Focused && ShowFocusCues)
        {
            var focusBounds = bounds;
            focusBounds.Inflate(-4, -4);
            ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds);
        }
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
