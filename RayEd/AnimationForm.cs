using IntSight.RayTracing.Language;
using System.IO;

namespace RayEd;

public partial class AnimationForm : Form
{
    public sealed class Parameters
    {
        public TimeShape Shape;
        public int FrameRate, TotalFrames;
        public int FirstFrame, LastFrame;
        public string Directory, FileName;

        public Parameters()
        {
            Shape = TimeShape.Linear;
            FrameRate = TotalFrames = 15;
            LastFrame = TotalFrames - 1;
            Directory = Application.StartupPath;
            FileName = string.Empty;
        }
    }

    private AnimationForm() => InitializeComponent();

    public static bool Execute(Parameters result)
    {
        using AnimationForm form = new();
        form.FrameRate.Value = result.FrameRate;
        form.TotalFrames.Value = result.TotalFrames;
        form.FirstFrame.Value = result.FirstFrame;
        form.LastFrame.Value = result.LastFrame;
        form.OutputDir.Text = result.Directory ?? string.Empty;
        switch (result.Shape)
        {
            case TimeShape.Linear: form.Linear.Checked = true; break;
            case TimeShape.Parabolic: form.Parabolic.Checked = true; break;
            case TimeShape.SquareRoot: form.SquareRoot.Checked = true; break;
            case TimeShape.Sine: form.Sine.Checked = true; break;
            case TimeShape.Ess: form.Ess.Checked = true; break;
        }
        if (form.ShowDialog(MainForm.Instance) == DialogResult.OK)
        {
            result.FrameRate = (int)form.FrameRate.Value;
            result.TotalFrames = (int)form.TotalFrames.Value;
            result.Directory = form.OutputDir.Text;
            if (form.Interval.Checked)
            {
                result.FirstFrame = Math.Max(0, Math.Min(result.TotalFrames - 1,
                    (int)form.FirstFrame.Value));
                result.LastFrame = Math.Max(0, Math.Min(result.TotalFrames - 1,
                    (int)form.LastFrame.Value));
            }
            else
            {
                result.FirstFrame = 0;
                result.LastFrame = result.TotalFrames - 1;
            }
            if (form.Linear.Checked)
                result.Shape = TimeShape.Linear;
            else if (form.Parabolic.Checked)
                result.Shape = TimeShape.Parabolic;
            else if (form.SquareRoot.Checked)
                result.Shape = TimeShape.SquareRoot;
            else if (form.Sine.Checked)
                result.Shape = TimeShape.Sine;
            else if (form.Ess.Checked)
                result.Shape = TimeShape.Ess;
            return true;
        }
        else
            return false;
    }

    private void Interval_CheckedChanged(object sender, EventArgs e) =>
        groupBox4.Enabled = Interval.Checked;

    private void BrowseDir_Click(object sender, EventArgs e)
    {
        folderBrowser.SelectedPath = Directory.Exists(OutputDir.Text)
            ? OutputDir.Text : string.Empty;
        if (folderBrowser.ShowDialog(this) == DialogResult.OK)
            OutputDir.Text = folderBrowser.SelectedPath;
    }
}