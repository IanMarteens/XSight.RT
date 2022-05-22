using IntSight.RayTracing.Engine;
using System.Configuration;
using System.Threading.Tasks;
using Rsc = RayEd.Properties.Resources;

namespace RayEd;

public partial class BenchForm : Form
{
    private static BenchForm instance;
    private readonly List<Benchmark.BenchmarkId> availableBenchmarks;
    private readonly BenchmarkSettings settings;

    public BenchForm()
    {
        InitializeComponent();
        settings = new BenchmarkSettings();
        bxMultithreading.Checked = settings.UseMultithreading;
        bxBackground.Checked = settings.RunBackground;
        availableBenchmarks = Benchmark.GetBenchmarks();
        cbBenchmarks.DataSource = availableBenchmarks;
        cbBenchmarks.DisplayMember = "Description";
        cbBenchmarks.ValueMember = "Id";
    }

    public static void ShowForm()
    {
        instance ??= new BenchForm();
        instance.Show();
        instance.BringToFront();
    }

    private static string FormatTime(int time) =>
        time <= 0 ? Rsc.ZeroSeconds :
            time < 60000 ? Rsc.FmtStrSeconds.InvFormat(time / 1000.0) :
            Rsc.FmtStrMinutes.InvFormat(time / 60000, (time % 60000) / 1000.0);

    private async void Run_ClickAsync(object sender, EventArgs e)
    {
        int benchmarkId = (int)cbBenchmarks.SelectedValue;
        bnRun.Enabled = false;
        groupBox.Text = Rsc.RenderRendering;
        if (bxBackground.Checked)
        {
            int time = await Task.Run(() => Benchmark.Run(benchmarkId, bxMultithreading.Checked));
            if (!IsDisposed)
            {
                groupBox.Text = FormatTime(time);
                bnRun.Enabled = true;
            }
        }
        else
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                int time = Benchmark.Run(benchmarkId, bxMultithreading.Checked);
                groupBox.Text = FormatTime(time);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                bnRun.Enabled = true;
            }
        }
    }

    private void BenchForm_FormClosed(object sender, FormClosedEventArgs e) =>
        instance = null;

    private void BenchForm_FormClosing(object sender, FormClosingEventArgs e) =>
        e.Cancel = !bnRun.Enabled;

    private void Benchmarks_SelectedIndexChanged(object sender, EventArgs e) =>
        toolTip.SetToolTip(cbBenchmarks,
            ((Benchmark.BenchmarkId)cbBenchmarks.SelectedItem).Remarks);

    private sealed class BenchmarkSettings : ApplicationSettingsBase
    {
        [UserScopedSetting, DefaultSettingValue("true")]
        public bool UseMultithreading
        {
            get => (bool)this[nameof(UseMultithreading)];
            set => this[nameof(UseMultithreading)] = value;
        }

        [UserScopedSetting, DefaultSettingValue("true")]
        public bool RunBackground
        {
            get => (bool)this[nameof(RunBackground)];
            set => this[nameof(RunBackground)] = value;
        }
    }

    private void Multithreading_CheckedChanged(object sender, EventArgs e)
    {
        settings.UseMultithreading = bxMultithreading.Checked;
        settings.Save();
    }

    private void Background_CheckedChanged(object sender, EventArgs e)
    {
        settings.RunBackground = bxBackground.Checked;
        settings.Save();
    }
}
