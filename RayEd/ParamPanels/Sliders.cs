using System.ComponentModel;
using System.Windows.Forms.VisualStyles;

namespace RayEd;

public sealed class Slider : Control
{
    private int _value, maxValue;
    private readonly VisualStyleRenderer track;
    private readonly VisualStyleRenderer thumbNormal;
    private readonly VisualStyleRenderer thumbPressed;
    private readonly VisualStyleRenderer thumbHot;
    private int trackHeight, thumbWidth, thumbHeight;
    bool pressed, hot;

    public Slider()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        track = new VisualStyleRenderer(VisualStyleElement.TrackBar.Track.Normal);
        thumbNormal = new VisualStyleRenderer(VisualStyleElement.TrackBar.Thumb.Normal);
        thumbPressed = new VisualStyleRenderer(VisualStyleElement.TrackBar.Thumb.Pressed);
        thumbHot = new VisualStyleRenderer(VisualStyleElement.TrackBar.Thumb.Hot);
        maxValue = 100;
    }

    public event EventHandler ValueChanged;

    protected override void SetBoundsCore(int x, int y, int width, int height,
        BoundsSpecified specified)
    {
        base.SetBoundsCore(x, y, width, height, specified);
        using Graphics g = CreateGraphics();
        trackHeight = track.GetPartSize(g, ClientRectangle, ThemeSizeType.True).Height;
        Size s = thumbNormal.GetPartSize(g, ClientRectangle, ThemeSizeType.True);
        thumbWidth = s.Width;
        thumbHeight = s.Height;
    }

    [DefaultValue(0)]
    [Category("Behavior")]
    [Description("The position of the slider.")]
    public int Value
    {
        get => _value;
        set
        {
            if (value < 0)
                value = 0;
            else if (value > maxValue)
                value = maxValue;
            if (_value != value)
            {
                _value = value;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [DefaultValue(100)]
    [Category("Behavior")]
    [Description("Maximum allowed for the position of the slider.")]
    public int Maximum
    {
        get { return maxValue; }
        set
        {
            if (value < 1)
                value = 1;
            if (maxValue != value)
            {
                maxValue = value;
                EventHandler eh = ValueChanged;
                if (_value > maxValue)
                    _value = maxValue;
                else
                    eh = null;
                Invalidate();
                eh?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
        get { return base.Text; }
        set { base.Text = value; }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!pressed)
        {
            if (ThumbArea.Contains(e.X, e.Y))
            {
                pressed = true;
                Invalidate();
            }
            else
            {
                int pos = e.X;
                int half = thumbWidth / 2;
                if (pos <= half)
                    Value = 0;
                else
                {
                    int width = ClientSize.Width;
                    if (pos >= width - half)
                        Value = maxValue;
                    else
                        Value = maxValue * (pos - half) / (width - thumbWidth);
                }
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (pressed)
        {
            pressed = false;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (pressed)
        {
            int pos = e.X;
            int half = thumbWidth / 2;
            if (pos <= half)
                Value = 0;
            else
            {
                int width = ClientSize.Width;
                if (pos >= width - half)
                    Value = maxValue;
                else
                    Value = maxValue * (pos - half) / (width - thumbWidth);
            }
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        if (!hot)
        {
            hot = true;
            Invalidate();
        }
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (hot)
        {
            hot = false;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }

    private Rectangle ThumbArea =>
        new Rectangle(
                _value * (ClientSize.Width - thumbWidth) / maxValue,
                (ClientSize.Height - thumbHeight) / 2,
                thumbWidth, thumbHeight);

    protected override void OnPaint(PaintEventArgs pe)
    {
        OnPaintBackground(pe);
        track.DrawBackground(pe.Graphics, new Rectangle(
            0, (ClientSize.Height - trackHeight) / 2, ClientSize.Width, trackHeight));
        VisualStyleRenderer r = pressed ? thumbPressed : hot ? thumbHot : thumbNormal;
        r.DrawBackground(pe.Graphics, ThumbArea);
    }
}