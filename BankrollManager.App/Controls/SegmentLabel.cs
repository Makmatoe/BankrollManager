using System.Drawing.Drawing2D;

namespace BankrollManager.App.Controls;

internal sealed class SegmentLabel : Label
{
    private bool _hovered;

    public SegmentLabel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        AutoSize = false;
        Cursor = Cursors.Hand;
        Font = Theme.BodyFont;
        TextAlign = ContentAlignment.MiddleCenter;
        UseMnemonic = false;
    }

    public bool Outlined { get; set; } = true;

    public int Radius { get; set; } = 8;

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
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

        var selected = Tag is true;
        var drawSurface = selected || _hovered || Outlined;
        var bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        if (drawSurface && bounds.Width > 0 && bounds.Height > 0)
        {
            var fillColor = Enabled
                ? selected
                    ? Theme.AccentSurface
                    : _hovered
                        ? Theme.PanelAlt
                        : Theme.Panel
                : Theme.Panel;
            var borderColor = Enabled
                ? selected
                    ? Theme.Accent
                    : _hovered
                        ? Theme.CommandNeutralBorder
                        : Theme.Border
                : Theme.Border;

            using var path = RoundedRectangle(bounds, Radius);
            using var fill = new SolidBrush(fillColor);
            using var border = new Pen(borderColor);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        var textColor = Enabled
            ? selected || _hovered
                ? Theme.Text
                : Theme.Muted
            : Theme.Muted;
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
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
        var adjustedRadius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
        var diameter = adjustedRadius * 2;

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
