using System.ComponentModel;
using System.Windows.Forms.VisualStyles;

namespace RayEd;

/// <summary>Collapsible panels for task panels.</summary>
public class TaskPanel : Panel
{
    private readonly VisualStyleRenderer renderer;
    private readonly CollapseButton button;
    private readonly System.Windows.Forms.Timer timer;
    private int oldHeight;
    private int accelerator;
    private bool collapsed;

    /// <summary>Creates a collapsible task panel.</summary>
    public TaskPanel()
    {
        renderer = new(VisualStyleElement.ExplorerBar.NormalGroupBackground.Normal);
        SetStyle(
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        DoubleBuffered = true;

        // Setup button.
        button = new(this);
        Controls.Add(button);

        // Setup timer.
        timer = new() { Interval = 25 };
        timer.Tick += Timer_Tick;
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Collapsed
    {
        get => collapsed;
        set
        {
            if (collapsed != value)
            {
                collapsed = value;
                SuspendLayout();
                timer.Enabled = true;
            }
        }
    }

    [Browsable(true)]
    public override string Text
    {
        get => button.Text;
        set => button.Text = value;
    }

    protected override void OnPaint(PaintEventArgs e) =>
        renderer.DrawBackground(e.Graphics, ClientRectangle);

    /// <summary>Performs the work of setting the specified bounds of this control.</summary>
    /// <param name="x">The new value for the Left property.</param>
    /// <param name="y">The new value for the Top property.</param>
    /// <param name="width">The new value for the Width property.</param>
    /// <param name="height">The new value for the Height property.</param>
    /// <param name="specified">Bitset with properties to be changed.</param>
    protected override void SetBoundsCore(int x, int y, int width, int height,
        BoundsSpecified specified)
    {
        base.SetBoundsCore(x, y, width, height, specified);
        if (!timer.Enabled && !collapsed)
            oldHeight = Height;
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (collapsed)
        {
            Size = new(Width, Height - 2 - accelerator);
            if (Height <= 25)
            {
                Size = new(Width, 25);
                timer.Enabled = false;
                button.Collapsed = true;
                accelerator = 0;
                ResumeLayout();
            }
        }
        else
        {
            Size = new(Width, Height + 2 + accelerator);
            if (Height >= oldHeight)
            {
                Size = new(Width, oldHeight);
                timer.Enabled = false;
                button.Collapsed = false;
                accelerator = 0;
                ResumeLayout();
            }
        }
        accelerator++;
    }

    private class CollapseButton : Control, IButtonControl
    {
        private bool collapsed, hovering, pressed;
        private readonly TaskPanel panel;

        public CollapseButton(TaskPanel panel)
        {
            this.panel = panel;
            SetStyle(
                ControlStyles.StandardClick |
                ControlStyles.StandardDoubleClick,
                false);
            SetStyle(
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            DoubleBuffered = true;
            Size = new Size(panel.Width, 25);
            Location = new Point(0, 0);
            panel.FontChanged += new EventHandler(Panel_FontChanged);
            Panel_FontChanged(null, null);
            Dock = DockStyle.Top;
        }

        private void Panel_FontChanged(object sender, EventArgs e) =>
            Font = new(panel.Font.Name, panel.Font.Size, FontStyle.Bold);

        DialogResult IButtonControl.DialogResult { get; set; }

        void IButtonControl.NotifyDefault(bool isDefault) { }

        void IButtonControl.PerformClick() => OnClick(EventArgs.Empty);

        public bool Collapsed
        {
            get => collapsed;
            set
            {
                if (value != collapsed)
                {
                    collapsed = value;
                    Invalidate();
                }
            }
        }

        protected override Size DefaultSize => new(75, 23);

        protected override void OnClick(EventArgs e)
        {
            panel.Collapsed = !panel.Collapsed;
            base.OnClick(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!pressed)
            {
                pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool click = pressed;
            if (pressed)
            {
                pressed = false;
                Invalidate();
            }
            base.OnMouseUp(e);
            if (click)
            {
                Update();
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Capture)
                if (pressed != ClientRectangle.Contains(e.X, e.Y))
                {
                    pressed = !pressed;
                    Invalidate();
                }
            base.OnMouseMove(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (!hovering)
            {
                hovering = true;
                Invalidate();
            }
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hovering)
            {
                hovering = false;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Paint background.
            VisualStyleRenderer renderer =
                new(VisualStyleElement.ExplorerBar.NormalGroupHead.Normal);
            renderer.DrawBackground(e.Graphics, ClientRectangle);

            // Draw text.
            const int indent = 8;
            Rectangle fontRect = new(indent, 6, Width - indent - 24, Height);
            if (pressed || hovering)
                TextRenderer.DrawText(e.Graphics, Text, Font, fontRect,
                    SystemColors.HotTrack, TextFormatFlags.Top | TextFormatFlags.Left);
            else if (!Enabled)
                TextRenderer.DrawText(e.Graphics, Text, Font, fontRect,
                    SystemColors.GrayText, TextFormatFlags.Top | TextFormatFlags.Left);
            else
                TextRenderer.DrawText(e.Graphics, Text, Font, fontRect,
                    SystemColors.MenuText, TextFormatFlags.Top | TextFormatFlags.Left);

            // Draw button.
            if (!collapsed)
            {
                if (pressed)
                    renderer = new(VisualStyleElement.ExplorerBar.NormalGroupCollapse.Pressed);
                else if (hovering)
                    renderer = new(VisualStyleElement.ExplorerBar.NormalGroupCollapse.Hot);
                else
                    renderer = new(VisualStyleElement.ExplorerBar.NormalGroupCollapse.Normal);
            }
            else
            {
                if (pressed)
                    renderer = new(VisualStyleElement.ExplorerBar.NormalGroupExpand.Pressed);
                else if (hovering)
                    renderer = new (VisualStyleElement.ExplorerBar.NormalGroupExpand.Hot);
                else
                    renderer = new(VisualStyleElement.ExplorerBar.NormalGroupExpand.Normal);
            }
            renderer.DrawBackground(e.Graphics, new Rectangle(Width - 22, 3, 20, 20));
        }
    }
}