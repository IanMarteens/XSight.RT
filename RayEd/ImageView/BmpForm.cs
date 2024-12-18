using IntSight.RayTracing.Engine;
using System.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;

namespace RayEd;

using Encoder = System.Drawing.Imaging.Encoder;
using Rsc = Properties.Resources;

/// <summary>Image Window: a viewer for rendered images.</summary>
public partial class BmpForm : Form
{
    private enum ImageSource { Original, EdgeMap, Reference, Difference };

    private string suggested;
    private string filename;
    private string sceneTitle;
    private PixelMap pixelMap;
    private PixelMap savedBitmap;
    private IScene scene;
    private ImageFormat savedFormat;
    private Point lastMousePos;
    private ImageSource source;
    private readonly BmpSettings settings;
    private bool justCreated;

    #region Public static methods for creating and showing the Image Window.

    /// <summary>Singleton instance of the Image Window.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static BmpForm Instance { get; private set; }

    public static void ShowForm()
    {
        if (Instance != null)
        {
            Instance.Show();
            Instance.BringToFront();
        }
    }

    /// <summary>Displays the Image Window with a fully rendered bitmap.</summary>
    /// <param name="pixelMap">A pixel map.</param>
    /// <param name="suggested">Suggested file name.</param>
    public static void ShowForm(PixelMap pixelMap, string suggested)
    {
        Instance ??= new();
        Instance.Prepare(pixelMap, suggested);
        Instance.ResetImageState();
    }

    /// <summary>Displays the Image Window for a partially rendered bitmap.</summary>
    /// <param name="pixelMap">A partially rendered bitmap.</param>
    public static void ShowForm(PixelMap pixelMap)
    {
        if (Instance == null)
        {
            Instance = new();
            Instance.Prepare(pixelMap);
        }
        else if (!pixelMap.Prepared)
            Instance.Prepare(pixelMap);
        Instance.pictureBox.Invalidate(
            pixelMap.Draw(Instance.pictureBox.Image as Bitmap));
        Instance.ResetImageState();
    }

    /// <summary>Displays the Image Window for a preview band.</summary>
    /// <param name="pixelMap">Preview image to display.</param>
    /// <param name="fromRow">Initial row.</param>
    /// <param name="toRow">Final row.</param>
    public static void ShowPreview(PixelMap pixelMap, int fromRow, int toRow)
    {
        if (Instance == null)
        {
            Instance = new();
            Instance.Prepare(pixelMap);
        }
        else if (!pixelMap.Prepared)
            Instance.Prepare(pixelMap);
        Instance.pictureBox.Invalidate(
            pixelMap.Draw(Instance.pictureBox.Image as Bitmap, fromRow, toRow));
        Instance.ResetImageState();
    }

    #endregion

    #region Construction and destruction.

    /// <summary>Creates and initializes the Image Window.</summary>
    private BmpForm()
    {
        InitializeComponent();
        justCreated = true;
        suggested = string.Empty;
        filename = string.Empty;
        settings = new BmpSettings();
        if (!settings.ShowToolBar)
            toolBar.Hide();
        pictureBox.Gamma = settings.Gamma;
        PictureBox_GammaChanged(pictureBox, EventArgs.Empty);
        Application.Idle += UpdateCommands;
        Owner = MainForm.Instance;
    }

    private void SetFileName(string value)
    {
        filename = value;
        string effectiveName = string.IsNullOrEmpty(filename) ?
            Rsc.UntitledScene : Path.GetFileName(filename);
        Text = pictureBox.Zoom == 1.0 ?
            Rsc.BmpFormCaption.InvFormat(effectiveName) :
            Rsc.BmpFormCaptionZoom.InvFormat(effectiveName, pictureBox.Zoom);
    }

    private void ResetImageState()
    {
        source = ImageSource.Original;
        pictureBox.Inverted = false;
        pictureBox.Monochrome = false;
        pictureBox.Sepia = false;
        pictureBox.Zoom = 1.0;
    }

    private void Prepare(PixelMap pixelMap, string suggested)
    {
        if (!string.IsNullOrEmpty(suggested))
        {
            suggested = Path.ChangeExtension(suggested, string.Empty);
            if (suggested.EndsWith('.'))
                suggested = suggested[0..^1];
            this.suggested = suggested;
        }
        else
            this.suggested = string.Empty;
        SetFileName(string.Empty);
        scene = pixelMap.Scene;
        this.pixelMap = pixelMap;
        sceneTitle = pixelMap.Title;
        pictureBox.Text = settings.ShowWatermark ? sceneTitle : "";
        pictureBox.Image = pixelMap.ToBitmap();
        AdjustBounds();
        pixelMap.Prepared = true;
        if (MainForm.IsSceneTreeVisible)
            MainForm.LoadSceneTree(scene);
    }

    private void Prepare(PixelMap pixelMap)
    {
        suggested = string.Empty;
        SetFileName(string.Empty);
        scene = null;
        this.pixelMap = null;
        sceneTitle = pixelMap.Title;
        pictureBox.Text = settings.ShowWatermark ? sceneTitle : "";
        pictureBox.Image = new Bitmap(
            pixelMap.Width, pixelMap.Height, PixelFormat.Format32bppArgb);
        AdjustBounds();
        pixelMap.Prepared = true;
        xPos.Text = ""; yPos.Text = ""; zPos.Text = ""; objInfo.Text = "";
    }

    private void AdjustBounds()
    {
        int extraHeight = statusStrip.Height;
        if (settings.ShowToolBar)
            extraHeight += toolBar.Height;
        ClientSize = new(
            pictureBox.Image.Width, pictureBox.Image.Height + extraHeight);
        Size maxArea = Screen.GetWorkingArea(this).Size;
        if (Width > maxArea.Width)
        {
            Width = maxArea.Width;
            Left = 0;
        }
        if (Height > maxArea.Height)
        {
            Height = maxArea.Height;
            Top = 0;
        }
        if (justCreated)
        {
            if (MainForm.Instance.Width + Width + 8 <= maxArea.Width)
            {
                if (MainForm.Instance.Right + Width <= maxArea.Width - 4)
                    Left = MainForm.Instance.Right + 2;
                else if (MainForm.Instance.Left > Width + 4)
                    Left = MainForm.Instance.Left - Width - 2;
            }
            else if (MainForm.Instance.Left > maxArea.Width - MainForm.Instance.Right)
                Left = MainForm.Instance.Left - Width - 2;
            else
                Left = MainForm.Instance.Right + 2;
            if (Left >= maxArea.Width - 4)
                Left = Math.Max(0, maxArea.Width - Width);
            justCreated = false;
        }
        Show();
        BringToFront();
    }

    private void BmpForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        Application.Idle -= UpdateCommands;
        Instance = null;
    }

    #endregion

    #region Keyboard handling.

    private void BmpForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            MainForm.Instance.Focus();
            MainForm.Instance.BringToFront();
            e.Handled = true;
        }
        else if ((e.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt)) == 0)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    MainForm.Render();
                    break;
                case Keys.F12:
                    MainForm.Instance.Focus();
                    MainForm.Instance.BringToFront();
                    break;
                default:
                    return;
            }
            e.Handled = true;
        }
    }

    #endregion

    #region Scene feedback.

    private void PictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        lastMousePos = pictureBox.MouseCoordinates;
        if (lastMousePos.X >= 0 && lastMousePos.X < pictureBox.Width &&
            lastMousePos.Y >= 0 && lastMousePos.Y < pictureBox.Height)
        {
            pos2d.Text = Rsc.FmtStrRowColumn.InvFormat(lastMousePos.Y, lastMousePos.X);
            Keys mods = ModifierKeys & (Keys.Control | Keys.Shift);
            if (mods == Keys.Shift && pixelMap != null)
            {
                Bitmap bmp = (Bitmap)pictureBox.Image;
                Color c = bmp.GetPixel(lastMousePos.X, bmp.Height - lastMousePos.Y - 1);
                string colorName = ColorTree.Instance.FindNeighbor(c, out bool exact);
                objInfo.Text = string.IsNullOrEmpty(colorName) ?
                    Rsc.FmtStrColor.InvFormat(c.R, c.G, c.B) :
                    Rsc.FmtStrColorAndName.InvFormat(c.R, c.G, c.B,
                        exact ? "=" : "~", colorName);
                return;
            }
            IScene s = scene;
            if (s != null)
            {
                HitInfo info = new();
                if (FindPoint(s, lastMousePos.Y, lastMousePos.X, ref info, out Vector direction))
                {
                    xPos.Text = info.HitPoint.X.ToString("0.00", CultureInfo.InvariantCulture);
                    yPos.Text = info.HitPoint.Y.ToString("0.00", CultureInfo.InvariantCulture);
                    zPos.Text = info.HitPoint.Z.ToString("0.00", CultureInfo.InvariantCulture);
                    objInfo.Text =
                        mods == (Keys.Control | Keys.Shift) ?
                            Rsc.FmtStrCosine3.InvFormat(info.Normal * direction) :
                        mods == Keys.Control ?
                            Rsc.FmtStrVector2.InvFormat(
                                info.Normal.X, info.Normal.Y, info.Normal.Z) :
                            Rsc.FmtStrDistance3.InvFormat(info.Time);
                }
                else
                {
                    xPos.Text = yPos.Text = zPos.Text = string.Empty;
                    objInfo.Text = string.Empty;
                }
            }
            else
                objInfo.Text = "∞";
        }
    }

    /// <summary>Trace a single eye ray for a given pixel in the image.</summary>
    /// <param name="s">Scene to trace.</param>
    /// <param name="row">Vertical coordinate of the pixel.</param>
    /// <param name="column">Horizontal coordinate of the pixel.</param>
    /// <param name="info">Hit information.</param>
    /// <returns>True when an intersection is found.</returns>
    private static bool FindPoint(IScene s, int row, int column,
        ref HitInfo info, out Vector direction)
    {
        s.Camera.Focus(row, column);
        Ray r = s.Camera.PrimaryRay;
        direction = r.Direction;
        if (s.Root.HitTest(r, double.MaxValue, ref info))
        {
            Vector localPoint = info.HitPoint;
            info.HitPoint = r[info.Time];
            if (info.Material.GetColor(out _, localPoint))
                info.Material.Bump(ref info.Normal, info.HitPoint);
            return true;
        }
        else
            return false;
    }

    private void ShowTree_Click(object sender, EventArgs e) =>
        MainForm.LoadSceneTree(scene);

    #endregion

    #region Context menu.

    private void UpdateCommands(object sender, EventArgs e)
    {
        miCopyPos.Enabled = miShowTree.Enabled = scene != null;
        miShowToolbar.Checked = settings.ShowToolBar;
        miShowWatermark.Checked = settings.ShowWatermark;
        miSaveAlpha.Checked = bnSaveAlpha.Checked;
        // Image source commands.
        bnRemember.Enabled = miRemember.Enabled =
            source == ImageSource.Original && pixelMap != null;
        bnShowOriginal.Checked = miShowOriginal.Checked =
            source == ImageSource.Original;
        bnShowEdges.Checked = miShowEdges.Checked =
            source == ImageSource.EdgeMap;
        bnShowRef.Checked = miShowRef.Checked =
            source == ImageSource.Reference;
        bnShowDiff.Checked = miShowDiff.Checked =
            source == ImageSource.Difference;
        bnShowOriginal.Enabled = bnShowEdges.Enabled =
            miShowOriginal.Enabled = miShowEdges.Enabled =
            pixelMap != null;
        bnShowRef.Enabled = bnShowDiff.Enabled =
            miShowRef.Enabled = miShowDiff.Enabled =
            pixelMap != null && savedBitmap != null &&
            pixelMap.Width == savedBitmap.Width && pixelMap.Height == savedBitmap.Height;
        // Display mode commands.
        bnInvertColors.Checked = miInvertColors.Checked = pictureBox.Inverted;
        bnDesaturate.Checked = miDesaturate.Checked = pictureBox.Monochrome;
        bnSepia.Checked = miSepia.Checked = pictureBox.Sepia;
    }

    private void ShowToolbar_Click(object sender, EventArgs e)
    {
        Size size = ClientSize;
        if (settings.ShowToolBar = !settings.ShowToolBar)
        {
            ClientSize = new(size.Width, size.Height + toolBar.Height);
            toolBar.Show();
        }
        else
        {
            toolBar.Hide();
            ClientSize = new(size.Width, size.Height - toolBar.Height);
        }
    }

    private void ShowWatermark_Click(object sender, EventArgs e) =>
        pictureBox.Text = (settings.ShowWatermark = !settings.ShowWatermark) ?
            sceneTitle ?? string.Empty : string.Empty;

    #endregion

    #region Persistence.

    private void Save_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(filename))
            miSaveAs.PerformClick();
        else
            SaveImage(pictureBox.Image, filename, savedFormat);
    }

    private void SaveAs_Click(object sender, EventArgs e)
    {
        saveFileDialog.FileName = string.IsNullOrEmpty(filename) ?
            suggested :
            Path.ChangeExtension(filename, null);
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            savedFormat =
                saveFileDialog.FilterIndex == 3 ? ImageFormat.Jpeg :
                saveFileDialog.FilterIndex == 2 ? ImageFormat.Png : ImageFormat.Bmp;
            if (savedFormat == ImageFormat.Png && miSaveAlpha.Checked)
                SaveImage(pixelMap.ToBitmap(true), saveFileDialog.FileName, savedFormat);
            else
                SaveImage(pictureBox.Image, saveFileDialog.FileName, savedFormat);
            SetFileName(saveFileDialog.FileName);
        }
    }

    private void SaveAlpha_Click(object sender, EventArgs e) =>
        bnSaveAlpha.Checked = !bnSaveAlpha.Checked;

    private static void SaveImage(Image image, string fileName, ImageFormat format)
    {
        if (format != ImageFormat.Jpeg)
            image.Save(fileName, format);
        else
            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (string.Equals(info.MimeType, "image/jpeg",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    EncoderParameters encoderParams = new(1);
                    encoderParams.Param[0] = new(Encoder.Quality, 100L);
                    image.Save(fileName, info, encoderParams);
                    break;
                }
            }
    }

    #endregion

    #region Clipboard commands.

    private void Copy_Click(object sender, EventArgs e)
    {
        if (pixelMap != null)
            Clipboard.SetImage(pixelMap.ToBitmap());
    }

    private void CopyView_Click(object sender, EventArgs e) => pictureBox.Copy();

    private void CopyPos_Click(object sender, EventArgs e)
    {
        IScene s = scene;
        if (s != null)
        {
            HitInfo info = new();
            if (FindPoint(s, lastMousePos.Y, lastMousePos.X, ref info, out _))
                Clipboard.SetText(Rsc.FmtStrVector3.InvFormat(
                    info.HitPoint.X, info.HitPoint.Y, info.HitPoint.Z));
            else
                Clipboard.Clear();
        }
    }

    #endregion

    #region Image source commands.

    private void Remember_Click(object sender, EventArgs e) => savedBitmap = pixelMap;

    private void ShowOriginal_Click(object sender, EventArgs e)
    {
        if (pixelMap != null)
        {
            pictureBox.Image = pixelMap.ToBitmap();
            source = ImageSource.Original;
        }
    }

    private void ShowEdges_Click(object sender, EventArgs e)
    {
        if (pixelMap != null)
        {
            pictureBox.Image = pixelMap.EdgeMap().ToBitmap();
            source = ImageSource.EdgeMap;
        }
    }

    private void ShowRef_Click(object sender, EventArgs e)
    {
        if (savedBitmap != null && pixelMap != null &&
            pixelMap.Width == savedBitmap.Width && pixelMap.Height == savedBitmap.Height)
        {
            pictureBox.Image = savedBitmap.ToBitmap();
            source = ImageSource.Reference;
        }
    }

    private void ShowDiff_Click(object sender, EventArgs e)
    {
        if (savedBitmap != null && pixelMap != null &&
            pixelMap.Width == savedBitmap.Width && pixelMap.Height == savedBitmap.Height)
        {
            PixelMap diff = new(pixelMap.Width, pixelMap.Height);
            for (int row = 0; row < pixelMap.Height; row++)
                for (int col = 0; col < pixelMap.Width; col++)
                {
                    TransPixel p = pixelMap[row, col], s = savedBitmap[row, col];
                    diff[row, col] = TransPixel.FromArgb(
                        unchecked((byte)Math.Abs(p.R - s.R)),
                        unchecked((byte)Math.Abs(p.G - s.G)),
                        unchecked((byte)Math.Abs(p.B - s.B)));
                }
            pictureBox.Image = diff.ToBitmap();
            source = ImageSource.Difference;
        }
    }

    #endregion

    #region Display mode commands.

    private void InvertColors_Click(object sender, EventArgs e) =>
        pictureBox.Inverted = !pictureBox.Inverted;

    private void Desaturate_Click(object sender, EventArgs e) =>
        pictureBox.Monochrome = !pictureBox.Monochrome;

    private void Sepia_Click(object sender, EventArgs e) =>
        pictureBox.Sepia = !pictureBox.Sepia;

    private void Gamma_DropDownOpening(object sender, EventArgs e)
    {
        foreach (ToolStripMenuItem item in miGamma.DropDownItems)
            item.Checked = Math.Abs(
                Convert.ToInt32(item.Tag) / 100F - pictureBox.Gamma) <= 0.01F;
        foreach (ToolStripMenuItem item in bnGamma.DropDownItems)
            item.Checked = Math.Abs(
                Convert.ToInt32(item.Tag) / 100F - pictureBox.Gamma) <= 0.01F;
    }

    private void SetGammaClick(object sender, EventArgs e)
    {
        pictureBox.Gamma = Convert.ToInt32(((ToolStripMenuItem)sender).Tag) / 100F;
        settings.Gamma = pictureBox.Gamma;
    }

    private void PictureBox_GammaChanged(object sender, EventArgs e)
    {
        miGamma.Checked = pictureBox.Gamma != 1.0F;
        bnGamma.ToolTipText = Rsc.GammaCorrection.InvFormat(pictureBox.Gamma);
    }

    private void PictureBox_ZoomChanged(object sender, EventArgs e) => SetFileName(filename);

    #endregion

    private sealed class BmpSettings : ApplicationSettingsBase
    {
        public BmpSettings() => PropertyChanged += delegate { Save(); };

        [UserScopedSetting, DefaultSettingValue("True")]
        public bool ShowToolBar
        {
            get => (bool)this[nameof(ShowToolBar)];
            set => this[nameof(ShowToolBar)] = value;
        }

        [UserScopedSetting, DefaultSettingValue("True")]
        public bool ShowWatermark
        {
            get => (bool)this[nameof(ShowWatermark)];
            set => this[nameof(ShowWatermark)] = value;
        }

        [UserScopedSetting, DefaultSettingValue("1")]
        public float Gamma
        {
            get => (float)this[nameof(Gamma)];
            set => this[nameof(Gamma)] = value;
        }
    }
}

internal static class Extensions
{
    public static string InvFormat(this string formatString, params object[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, formatString, arguments);
}