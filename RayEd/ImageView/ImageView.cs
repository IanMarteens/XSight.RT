using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RayEd;

/// <summary>Displays an image, with zoom and scrolling support.</summary>
public class ImageView : Control
{
    private static readonly Cursor grabCursor = new Cursor(typeof(ImageView), "Grab.cur");
    private static readonly Keys[] inputKeys = new[]
    {
        Keys.Down, Keys.Up, Keys.Right, Keys.Left,
        Keys.Home, Keys.End, Keys.PageDown, Keys.PageUp
    };

    private const int MIN_ZOOM = -6;
    private const int MAX_ZOOM = 14;

    private Image image;
    private int originX, originY, zoomWidth, zoomHeight;
    private int zoom, captureX, captureY;
    private Cursor savedCursor;
    private readonly ImageAttributes imgAttributes;
    private readonly ColorMatrix grayColorMatrix;
    private readonly ColorMatrix invColorMatrix;
    private readonly ColorMatrix grayInvColorMatrix;
    private readonly ColorMatrix sepiaColorMatrix;
    private readonly ColorMatrix sepiaInvColorMatrix;
    private readonly StringFormat stringFormat;
    private ContentAlignment textAlign;
    private float gamma;
    private InterpolationMode antialiasing;
    private bool monochrome;
    private bool sepia;
    private bool inverted;
    private bool moving;

    /// <summary>Creates a image view control with zooming capabilities.</summary>
    public ImageView()
    {
        antialiasing = InterpolationMode.HighQualityBilinear;
        stringFormat = new StringFormat();
        TextAlign = ContentAlignment.TopLeft;
        SetStyle(
            ControlStyles.Opaque |
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Cross;
        gamma = 1.0F;
        imgAttributes = new ImageAttributes();
        imgAttributes.SetGamma(gamma);
        grayColorMatrix = new ColorMatrix(new[]
        {
            new[]{ 0.212F,  0.212F,  0.212F, 0.0F, 0.0F},
            new[]{ 0.715F,  0.715F,  0.715F, 0.0F, 0.0F},
            new[]{ 0.073F,  0.073F,  0.073F, 0.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 1.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 0.0F, 1.0F}
        });
        invColorMatrix = new ColorMatrix(new[]
        {
            new[]{-1.000F,  0.000F,  0.000F, 0.0F, 0.0F},
            new[]{ 0.000F, -1.000F,  0.000F, 0.0F, 0.0F},
            new[]{ 0.000F,  0.000F, -1.000F, 0.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 1.0F, 0.0F},
            new[]{ 1.000F,  1.000F,  1.000F, 0.0F, 1.0F}
        });
        grayInvColorMatrix = new ColorMatrix(new[]
        {
            new[]{-0.212F, -0.212F, -0.212F, 0.0F, 0.0F},
            new[]{-0.715F, -0.715F, -0.715F, 0.0F, 0.0F},
            new[]{-0.073F, -0.073F, -0.073F, 0.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 1.0F, 0.0F},
            new[]{ 1.000F,  1.000F,  1.000F, 0.0F, 1.0F}
        });
        sepiaColorMatrix = new ColorMatrix(new[]{
            new[]{ 0.393F,  0.349F,  0.272F, 0.0F, 0.0F},
            new[]{ 0.769F,  0.686F,  0.534F, 0.0F, 0.0F},
            new[]{ 0.189F,  0.168F,  0.131F, 0.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 1.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 0.0F, 1.0F}
        });
        sepiaInvColorMatrix = new ColorMatrix(new[]{
            new[]{-0.393F, -0.349F, -0.272F, 0.0F, 0.0F},
            new[]{-0.769F, -0.686F, -0.534F, 0.0F, 0.0F},
            new[]{-0.189F, -0.168F, -0.131F, 0.0F, 0.0F},
            new[]{ 0.000F,  0.000F,  0.000F, 1.0F, 0.0F},
            new[]{ 1.000F,  1.000F,  1.000F, 0.0F, 1.0F}
        });
    }

    /// <summary>Occurs when the zoom factor changes.</summary>
    [Category("Property Changed")]
    [Description("Occurs when the zoom factor changes.")]
    public event EventHandler ZoomChanged;

    /// <summary>Occurs when the gamma correction changes.</summary>
    [Category("Property Changed")]
    [Description("Occurs when the gamma correction changes.")]
    public event EventHandler GammaChanged;

    /// <summary>Gets or sets the drawing quality when the image is scaled.</summary>
    [DefaultValue(InterpolationMode.HighQualityBilinear)]
    [Category("Appearance")]
    [Description("The drawing quality when the image is scaled.")]
    public InterpolationMode Antialiasing
    {
        get => antialiasing;
        set
        {
            if (antialiasing != value)
            {
                antialiasing = value;
                Invalidate();
            }
        }
    }

    /// <summary>Gets or sets the image displayed by this control.</summary>
    [DefaultValue(null)]
    [Category("Appearance")]
    [Description("The image displayed by this control.")]
    public Image Image
    {
        get => image;
        set
        {
            if (image != value)
            {
                image = value;
                if (image != null)
                {
                    double zoomFactor = Math.Pow(1.2, zoom);
                    zoomWidth = (int)(image.Width * zoomFactor);
                    zoomHeight = (int)(image.Height * zoomFactor);
                    SetViewOrigin(originX, originY);
                }
                Invalidate();
            }
        }
    }

    /// <summary>Gets or sets the current gamma correction factor.</summary>
    [DefaultValue(1.0F)]
    [Category("Appearance")]
    [Description("The current gamma correction factor.")]
    public float Gamma
    {
        get => gamma;
        set
        {
            if (gamma != value)
            {
                gamma = value;
                if (gamma <= 0F)
                    imgAttributes.ClearGamma();
                else
                    imgAttributes.SetGamma(gamma);
                Invalidate();
                GammaChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [DefaultValue(false)]
    [Category("Appearance")]
    public bool Monochrome
    {
        get => monochrome;
        set
        {
            if (monochrome != value)
            {
                monochrome = value;
                if (monochrome)
                    sepia = false;
                SetColorMatrix();
            }
        }
    }

    [DefaultValue(false)]
    [Category("Appearance")]
    public bool Sepia
    {
        get => sepia;
        set
        {
            if (sepia != value)
            {
                sepia = value;
                if (sepia)
                    monochrome = false;
                SetColorMatrix();
            }
        }
    }

    [DefaultValue(false)]
    [Category("Appearance")]
    public bool Inverted
    {
        get => inverted;
        set
        {
            if (inverted != value)
            {
                inverted = value;
                SetColorMatrix();
            }
        }
    }

    /// <summary>Assigns a color map to the image attributes.</summary>
    private void SetColorMatrix()
    {
        if (monochrome)
            if (inverted)
                imgAttributes.SetColorMatrix(grayInvColorMatrix);
            else
                imgAttributes.SetColorMatrix(grayColorMatrix);
        else if (sepia)
            if (inverted)
                imgAttributes.SetColorMatrix(sepiaInvColorMatrix);
            else
                imgAttributes.SetColorMatrix(sepiaColorMatrix);
        else
            if (inverted)
                imgAttributes.SetColorMatrix(invColorMatrix);
            else
                imgAttributes.ClearColorMatrix();
        Invalidate();
    }

    /// <summary>Gets or sets the current zoom factor.</summary>
    [Browsable(false)]
    [Category("Behavior")]
    [Description("The current zoom factor.")]
    public double Zoom
    {
        get => Math.Pow(1.2, zoom);
        set
        {
            double currentZoom = Math.Pow(1.2, zoom);
            if (currentZoom != value && value > 0.0)
            {
                int proposed = (int)Math.Round((Math.Log(value) / Math.Log(1.2)));
                SetZoomRect(proposed, Width / 2, Height / 2);
            }
        }
    }

    [DefaultValue(ContentAlignment.TopLeft)]
    public ContentAlignment TextAlign
    {
        get => textAlign;
        set
        {
            textAlign = value;
            switch (textAlign)
            {
                case ContentAlignment.BottomCenter:
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Far;
                    break;
                case ContentAlignment.BottomLeft:
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Far;
                    break;
                case ContentAlignment.BottomRight:
                    stringFormat.Alignment = StringAlignment.Far;
                    stringFormat.LineAlignment = StringAlignment.Far;
                    break;
                case ContentAlignment.MiddleCenter:
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;
                    break;
                case ContentAlignment.MiddleLeft:
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Center;
                    break;
                case ContentAlignment.MiddleRight:
                    stringFormat.Alignment = StringAlignment.Far;
                    stringFormat.LineAlignment = StringAlignment.Center;
                    break;
                case ContentAlignment.TopCenter:
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    break;
                case ContentAlignment.TopLeft:
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    break;
                case ContentAlignment.TopRight:
                    stringFormat.Alignment = StringAlignment.Far;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    break;
            }
            if (!string.IsNullOrEmpty(Text))
                Invalidate();
        }
    }

    [DefaultValue("")]
    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    public override Image BackgroundImage
    {
        get => base.BackgroundImage;
        set { base.BackgroundImage = value; }
    }

    /// <summary>Gets or sets the background image layout.</summary>
    [Browsable(false)]
    public override ImageLayout BackgroundImageLayout
    {
        get => base.BackgroundImageLayout;
        set => base.BackgroundImageLayout = value;
    }

    /// <summary>Handles zoom reset by double clicking.</summary>
    /// <param name="e">Information about the double click event.</param>
    protected override void OnDoubleClick(EventArgs e)
    {
        if (zoom != 0 || originX != 0 || originY != 0)
        {
            SetViewOrigin(0, 0);
            SetZoomRect(0, 0, 0);
        }
        base.OnDoubleClick(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            if (zoomWidth > this.Width || zoomHeight > this.Height)
            {
                Capture = true;
                savedCursor = Cursor;
                Cursor = grabCursor;
                captureX = e.X;
                captureY = e.Y;
                moving = true;
            }
        base.OnMouseDown(e);
    }

    /// <summary>Handles scrolling by mouse dragging.</summary>
    /// <param name="e">Information about the mouse move event.</param>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (moving)
        {
            int dx = originX + e.X - captureX;
            captureX = e.X;
            int dy = originY + e.Y - captureY;
            captureY = e.Y;
            SetViewOrigin(dx, dy);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (moving)
        {
            moving = false;
            Capture = false;
            Cursor = savedCursor;
        }
        base.OnMouseUp(e);
    }

    /// <summary>Handles scrolling and zooming with the mouse wheel.</summary>
    /// <param name="e">Information about the event.</param>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control)
        {
            int steps = e.Delta * SystemInformation.MouseWheelScrollLines /
                SystemInformation.MouseWheelScrollDelta;
            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                steps *= Height / 8;
            SetViewOrigin(originX, originY + steps);
        }
        else
        {
            int steps = e.Delta / SystemInformation.MouseWheelScrollDelta;
            SetZoomRect(zoom + steps, e.X, e.Y);
        }
        base.OnMouseWheel(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x20E)
        {
            int delta = (int)(long)m.WParam >> 16;
            int steps = delta * SystemInformation.MouseWheelScrollLines /
                SystemInformation.MouseWheelScrollDelta;
            SetViewOrigin(originX + steps, originY);
            m.Result = IntPtr.Zero;
        }
        base.WndProc(ref m);
    }

    /// <summary>Handles form resizing.</summary>
    /// <param name="e">Information about the resize event.</param>
    protected override void OnResize(EventArgs e)
    {
        SetViewOrigin(originX, originY);
        base.OnResize(e);
    }

    /// <summary>Assigns a new zoom factor and view origin.</summary>
    /// <param name="newZoom">New zoom factor.</param>
    /// <param name="x">New horizontal view origin.</param>
    /// <param name="y">New vertical view origin.</param>
    private void SetZoomRect(int newZoom, int x, int y)
    {
        if (newZoom > MAX_ZOOM)
            newZoom = MAX_ZOOM;
        else if (newZoom < MIN_ZOOM)
            newZoom = MIN_ZOOM;
        if (newZoom != zoom)
        {
            zoom = newZoom;
            double pX = (x - originX) / (double)zoomWidth;
            double pY = (y - originY) / (double)zoomHeight;
            double zoomFactor = Math.Pow(1.2, zoom);
            zoomWidth = (int)(image.Width * zoomFactor);
            zoomHeight = (int)(image.Height * zoomFactor);
            SetViewOrigin((int)(x - pX * zoomWidth), (int)(y - pY * zoomHeight));
            Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Assigns a new view origin.</summary>
    /// <param name="newX">New horizontal view origin.</param>
    /// <param name="newY">New vertical view origin.</param>
    private void SetViewOrigin(int newX, int newY)
    {
        if (newX > 0 || Width > zoomWidth)
            newX = 0;
        else if (newX < Width - zoomWidth)
            newX = Width - zoomWidth;
        if (newY > 0 || Height > zoomHeight)
            newY = 0;
        else if (newY < Height - zoomHeight)
            newY = Height - zoomHeight;
        if (newX != originX || newY != originY)
        {
            originX = newX;
            originY = newY;
            Invalidate();
        }
    }

    /// <summary>Copies a scaled copy of the image to the control's surface.</summary>
    /// <param name="e">Information for painting.</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        if (image != null)
        {
            Rectangle r = new Rectangle(originX, originY, zoomWidth, zoomHeight);
            e.Graphics.InterpolationMode = antialiasing;
            e.Graphics.DrawImage(image, r, 0, 0, image.Width, image.Height,
                GraphicsUnit.Pixel, imgAttributes);
            int deltaX = Width - r.Right;
            int deltaY = Height - r.Bottom;
            if (deltaX > 0 || deltaY > 0)
                using (Brush backBrush = new SolidBrush(BackColor))
                {
                    if (deltaX > 0)
                        e.Graphics.FillRectangle(backBrush,
                            r.Right, 0, deltaX, Height);
                    if (deltaY > 0)
                        e.Graphics.FillRectangle(backBrush,
                            0, r.Bottom, r.Right, deltaY);
                }
        }
        else
            using (Brush backBrush = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(backBrush, this.ClientRectangle);
        if (!string.IsNullOrEmpty(Text))
        {
            RectangleF r = new RectangleF(Padding.Left, Padding.Top,
                Width - 2 * Padding.Right, Height - 2 * Padding.Bottom);
            using Brush brush = new SolidBrush(ForeColor);
            e.Graphics.DrawString(Text, Font, brush, r, stringFormat);
        }
        base.OnPaint(e);
    }

    /// <summary>Copies the current displayed image to the Clipboard.</summary>
    public void Copy()
    {
        InterpolationMode saveAntialias = antialiasing;
        antialiasing = InterpolationMode.HighQualityBicubic;
        try
        {
            using Bitmap bmp = new Bitmap(Width, Height);
            DrawToBitmap(bmp, ClientRectangle);
            Clipboard.SetImage(bmp);
        }
        finally
        {
            antialiasing = saveAntialias;
        }
    }

    /// <summary>Check if a keystroke must be handled by this control.</summary>
    /// <param name="keyData">Key to be checked.</param>
    /// <returns>True, if the key must be handled by this control.</returns>
    protected override bool IsInputKey(Keys keyData) => 
        Array.IndexOf(inputKeys, keyData) != -1 ||
            base.IsInputKey(keyData);

    /// <summary>Handles zooming and panning using keyboard shortcuts.</summary>
    /// <param name="e">Information about the key down event.</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        Keys mods = e.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt);
        int delta = SystemInformation.MouseWheelScrollLines;
        switch (e.KeyCode)
        {
            case Keys.Add:
                if (mods == 0)
                {
                    Zoom *= 1.2;
                    e.Handled = true;
                }
                break;
            case Keys.Subtract:
                if (mods == 0)
                {
                    Zoom /= 1.2;
                    e.Handled = true;
                }
                break;
            case Keys.Multiply:
                if (mods == 0)
                {
                    Zoom = 1.0;
                    e.Handled = true;
                }
                break;
            case Keys.Up:
                if (mods == 0)
                {
                    SetViewOrigin(originX, originY + delta);
                    e.Handled = true;
                }
                else if (mods == Keys.Control)
                {
                    SetViewOrigin(originX, originY + delta * 4);
                    e.Handled = true;
                }
                break;
            case Keys.Down:
                if (mods == 0)
                {
                    SetViewOrigin(originX, originY - delta);
                    e.Handled = true;
                }
                else if (mods == Keys.Control)
                {
                    SetViewOrigin(originX, originY - delta * 4);
                    e.Handled = true;
                }
                break;
            case Keys.Left:
                if (mods == 0)
                {
                    SetViewOrigin(originX + delta, originY);
                    e.Handled = true;
                }
                else if (mods == Keys.Control)
                {
                    SetViewOrigin(originX + delta * 4, originY);
                    e.Handled = true;
                }
                break;
            case Keys.Right:
                if (mods == 0)
                {
                    SetViewOrigin(originX - delta, originY);
                    e.Handled = true;
                }
                else if (mods == Keys.Control)
                {
                    SetViewOrigin(originX - delta * 4, originY);
                    e.Handled = true;
                }
                break;
            case Keys.PageUp:
                if (mods == 0)
                {
                    SetViewOrigin(originX, originY + 4 * Height / 5);
                    e.Handled = true;
                }
                break;
            case Keys.PageDown:
                if (mods == 0)
                {
                    SetViewOrigin(originX, originY - 4 * Height / 5);
                    e.Handled = true;
                }
                break;
            case Keys.Home:
                if (mods == 0)
                {
                    SetViewOrigin(0, 0);
                    e.Handled = true;
                }
                break;
            case Keys.End:
                if (mods == 0)
                {
                    SetViewOrigin(int.MinValue, int.MinValue);
                    e.Handled = true;
                }
                break;
        }
        base.OnKeyDown(e);
    }

    /// <summary>Gets the row and column of the pixel below the mouse cursor.</summary>
    /// <remarks>Coordinates are scaled and offset to take account of the view.</remarks>
    [Browsable(false)]
    [Category("Mouse")]
    public Point MouseCoordinates
    {
        get
        {
            Point result = PointToClient(Control.MousePosition);
            if (image == null)
                return result;
            result.X -= originX;
            result.Y = zoomHeight - result.Y + originY - 1;
            if (zoom != 0)
            {
                result.X = image.Width * result.X / zoomWidth;
                result.Y = image.Height * result.Y / zoomHeight;
            }
            return result;
        }
    }
}
