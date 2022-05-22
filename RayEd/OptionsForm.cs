using XSightSettings = IntSight.RayTracing.Engine.Properties.Settings;

namespace RayEd;

public partial class OptionsForm : Form
{
    private static string[] monospacedFonts;
    private static int lastSelectedTab;

    private OptionsForm()
    {
        InitializeComponent();
        // Scene optimizer settings
        edLoops.Value = XSightSettings.Default.LoopThreshold;
        edUnions.Value = XSightSettings.Default.UnionThreshold;
        edBSpheres.Value = Convert.ToDecimal(XSightSettings.Default.BoundingSphereThreshold);
        edDiffusion.Value = XSightSettings.Default.DiffusionLevels;
        // Sonar mode colors
        colorBack.BackColor = XSightSettings.Default.SonarBackColor;
        colorNear.BackColor = XSightSettings.Default.SonarForeColor;
        colorFar.BackColor = XSightSettings.Default.SonarFarColor;
        // Scene editor settings
        edTabSize.Value = Properties.Settings.Default.TabLength;
        edLeftMargin.Value = Properties.Settings.Default.LeftMargin;
        if (monospacedFonts == null)
            monospacedFonts = IntSight.Controls.FontInfo.GetMonospacedFonts(Handle);
        cbFamilies.Items.AddRange(monospacedFonts);
        cbFamilies.SelectedIndex = cbFamilies.FindString(Properties.Settings.Default.FontName);
        edFontSize.Value = Convert.ToDecimal(Properties.Settings.Default.FontSize);
        bxSmartIndent.Checked = Properties.Settings.Default.SmartIndentation;
        cbVisualStyle.SelectedIndex = Properties.Settings.Default.VisualStyle;
        tabControl.SelectedIndex = lastSelectedTab;
    }

    private void ApplyChanges()
    {
        lastSelectedTab = tabControl.SelectedIndex;
        // Ray tracer settings
        XSightSettings.Default.LoopThreshold = Convert.ToInt32(edLoops.Value);
        XSightSettings.Default.UnionThreshold = Convert.ToInt32(edUnions.Value);
        XSightSettings.Default.BoundingSphereThreshold =
            Convert.ToDouble(edBSpheres.Value);
        XSightSettings.Default.DiffusionLevels = Convert.ToInt32(edDiffusion.Value);
        XSightSettings.Default.SonarBackColor = colorBack.BackColor;
        XSightSettings.Default.SonarFarColor = colorFar.BackColor;
        XSightSettings.Default.SonarForeColor = colorNear.BackColor;
        // Scene editor settings
        Properties.Settings.Default.SmartIndentation = bxSmartIndent.Checked;
        Properties.Settings.Default.TabLength = Convert.ToInt32(edTabSize.Value);
        Properties.Settings.Default.LeftMargin = Convert.ToInt32(edLeftMargin.Value);
        Properties.Settings.Default.FontName = cbFamilies.Text;
        Properties.Settings.Default.FontSize = Convert.ToSingle(edFontSize.Value);
        Properties.Settings.Default.VisualStyle = cbVisualStyle.SelectedIndex;
        XSightSettings.Default.Save();
        Properties.Settings.Default.Save();
    }

    public static bool Execute(Form parentForm)
    {
        using var form = new OptionsForm();
        if (form.ShowDialog(parentForm) == DialogResult.OK)
        {
            form.ApplyChanges();
            return true;
        }
        else
            return false;
    }

    private void ColorPanel_MouseEnter(object sender, EventArgs e) =>
        ((Panel)sender).BorderStyle = BorderStyle.Fixed3D;

    private void ColorPanel_MouseLeave(object sender, EventArgs e) =>
        ((Panel)sender).BorderStyle = BorderStyle.FixedSingle;

    private void ColorBack_DoubleClick(object sender, EventArgs e)
    {
        colorDialog.Color = colorBack.BackColor;
        if (colorDialog.ShowDialog(this) == DialogResult.OK)
            colorBack.BackColor = colorDialog.Color;
    }

    private void ColorNear_DoubleClick(object sender, EventArgs e)
    {
        colorDialog.Color = colorNear.BackColor;
        if (colorDialog.ShowDialog(this) == DialogResult.OK)
            colorNear.BackColor = colorDialog.Color;
    }

    private void ColorFar_DoubleClick(object sender, EventArgs e)
    {
        colorDialog.Color = colorFar.BackColor;
        if (colorDialog.ShowDialog(this) == DialogResult.OK)
            colorFar.BackColor = colorDialog.Color;
    }

    private void OptionsForm_FormClosing(object sender, FormClosingEventArgs e)
    {
    }
}