using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using IntSight.RayTracing.Engine;

namespace RayEd
{
    public partial class NoiseForm : Form
    {
        private static NoiseForm instance;
        private readonly NoiseSettings settings;

        private NoiseForm()
        {
            InitializeComponent();
            Owner = MainForm.Instance;
            settings = new NoiseSettings();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            edBmpWidth.Text = settings.BitmapWidth.ToString();
            edBmpHeight.Text = settings.BitmapHeight.ToString();
            edSampleWidth.Text = settings.SampleWidth.ToString();
            edSampleHeight.Text = settings.SampleHeight.ToString();
            cbMethod.SelectedIndex = settings.Method;
            cbTurbulence.SelectedIndex = settings.Turbulence;
            color01.BackColor = settings.Color1;
            color02.BackColor = settings.Color2;
            txTime.Text = string.Empty;
        }

        public static void ShowForm()
        {
            instance ??= new NoiseForm();
            instance.Show();
            instance.cbColorMap.SelectedIndex = instance.settings.ColorMap;
            instance.BringToFront();
        }

        private void Close_Click(object sender, EventArgs e) => Close();

        private void NoiseForm_FormClosed(object sender, FormClosedEventArgs e) => instance = null;

        private void Apply_Click(object sender, EventArgs e)
        {
            int bwidth, bheight, swidth, sheight;
            try
            {
                bwidth = int.Parse(edBmpWidth.Text, NumberStyles.Integer);
                bheight = int.Parse(edBmpHeight.Text, NumberStyles.Integer);
                swidth = int.Parse(edSampleWidth.Text, NumberStyles.Integer);
                sheight = int.Parse(edSampleHeight.Text, NumberStyles.Integer);
            }
            catch
            {
                return;
            }
            NoiseGen.Color1 = color01.BackColor;
            NoiseGen.Color2 = color02.BackColor;
            Cursor saveCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                BmpForm.ShowForm(NoiseGen.Test(
                    bwidth, bheight, swidth, sheight,
                    (short)cbTurbulence.SelectedIndex,
                    cbMethod.SelectedIndex,
                    cbColorMap.SelectedIndex), "");
                watch.Stop();
                txTime.Text = string.Format(
                    Properties.Resources.NoiseGenerationTime,
                    watch.ElapsedMilliseconds / 1000.0);
            }
            finally
            {
                Cursor.Current = saveCursor;
            }
            settings.BitmapWidth = bwidth;
            settings.BitmapHeight = bheight;
            settings.SampleWidth = swidth;
            settings.SampleHeight = sheight;
            settings.Method = cbMethod.SelectedIndex;
            settings.Turbulence = cbTurbulence.SelectedIndex;
            settings.ColorMap = cbColorMap.SelectedIndex;
            settings.Save();
        }

        private void ColorMap_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbColorMap.SelectedIndex == 2)
            {
                if (!color01.Visible)
                {
                    Height += 27;
                    color01.Show();
                    color02.Show();
                }
            }
            else
            {
                if (color01.Visible)
                {
                    color02.Hide();
                    color01.Hide();
                    Height -= 27;
                }
            }
        }

        private void ColorPanel_MouseEnter(object sender, EventArgs e) =>
            ((Panel)sender).BorderStyle = BorderStyle.Fixed3D;

        private void ColorPanel_MouseLeave(object sender, EventArgs e) =>
            ((Panel)sender).BorderStyle = BorderStyle.FixedSingle;

        private void ColorPanel_DoubleClick(object sender, EventArgs e)
        {
            Panel colorPanel = (Panel)sender;
            colorDialog.Color = colorPanel.BackColor;
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                colorPanel.BackColor = colorDialog.Color;
            }
        }

        private static class NoiseGen
        {
            private static HsvPixel hsv1, hsv2;

            public static Pixel Color1 { get; set; } = new Pixel(1.00F, 1.00F, 0.00F);
            public static Pixel Color2 { get; set; } = new Pixel(1.00F, 0.00F, 0.00F);

            public static PixelMap Test(
                int width, int height, int sampleWidth, int sampleHeight, short turbulence,
                int method, int colorMap)
            {
                PixelMap map = new PixelMap(width, height);
                double w = (double)sampleWidth / width, h = (double)sampleHeight / height;
                Func<double, Pixel> v2p;
                if (method == 2)
                {
                    // Bubbles
                    switch (colorMap)
                    {
                        case 0:
                            v2p = value => new Pixel(value);
                            break;
                        case 1:
                            v2p = value => Pixel.FromHue(360.0 * value);
                            break;
                        default:
                            v2p = value => new Pixel(hsv1.Interpolate(value, hsv2));
                            hsv1 = new HsvPixel(Color1);
                            hsv2 = new HsvPixel(Color2);
                            break;
                    }
                    CrackleNoise gen = new CrackleNoise();
                    if (turbulence == 0)
                        for (int row = 0; row < height; row++)
                            for (int col = 0; col < width; col++)
                                map[row, col] = v2p(gen.Bubbles(col * w, row * h, 0.0));
                    else
                        for (int row = 0; row < height; row++)
                            for (int col = 0; col < width; col++)
                                map[row, col] = v2p(gen.Bubbles(col * w, row * h, 0.0, turbulence));
                }
                else if (method == 1)
                {
                    switch (colorMap)
                    {
                        case 0:
                            v2p = value => new Pixel(value);
                            break;
                        case 1:
                            v2p = value => Pixel.FromHue(360.0 * value);
                            break;
                        default:
                            v2p = value => new Pixel(hsv1.Interpolate(value, hsv2));
                            hsv1 = new HsvPixel(Color1);
                            hsv2 = new HsvPixel(Color2);
                            break;
                    }
                    CrackleNoise gen = new CrackleNoise(new Vector(-1, 1, 0));
                    if (turbulence == 0)
                        for (int row = 0; row < height; row++)
                            for (int col = 0; col < width; col++)
                                map[row, col] = v2p(gen[col * w, row * h, 0.0]);
                    else
                        for (int row = 0; row < height; row++)
                            for (int col = 0; col < width; col++)
                                map[row, col] = v2p(gen[col * w, row * h, 0.0, turbulence]);
                }
                else
                {
                    switch (colorMap)
                    {
                        case 0:
                            v2p = value => new Pixel((1.0 + value) / 2);
                            break;
                        case 1:
                            v2p = value => Pixel.FromHue(180.0 * (1.0 + value));
                            break;
                        default:
                            v2p = value => new Pixel(hsv1.Interpolate(value, hsv2));
                            hsv1 = new HsvPixel(Color1);
                            hsv2 = new HsvPixel(Color2);
                            break;
                    }
                    SolidNoise gen = new SolidNoise();
                    if (turbulence == 0)
                        for (int row = 0; row < height; row++)
                            for (int col = 0; col < width; col++)
                                map[row, col] = v2p(gen[col * w, row * h, 0.0]);
                    else
                        for (int row = 0; row < height; row++)
                            for (int col = 0; col < width; col++)
                                map[row, col] = v2p(gen[col * w, row * h, 0.0, turbulence]);
                }
                return map;
            }
        }

        private sealed class NoiseSettings : ApplicationSettingsBase
        {
            [UserScopedSetting, DefaultSettingValue("640")]
            public int BitmapWidth
            {
                get => (int)this[nameof(BitmapWidth)];
                set => this[nameof(BitmapWidth)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("480")]
            public int BitmapHeight
            {
                get => (int)this[nameof(BitmapHeight)];
                set => this[nameof(BitmapHeight)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("4")]
            public int SampleWidth
            {
                get => (int)this[nameof(SampleWidth)];
                set => this[nameof(SampleWidth)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("3")]
            public int SampleHeight
            {
                get => (int)this[nameof(SampleHeight)];
                set => this[nameof(SampleHeight)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("0")]
            public int Method
            {
                get => (int)this[nameof(Method)];
                set => this[nameof(Method)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("0")]
            public int Turbulence
            {
                get => (int)this[nameof(Turbulence)];
                set => this[nameof(Turbulence)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("0")]
            public int ColorMap
            {
                get => (int)this[nameof(ColorMap)];
                set => this[nameof(ColorMap)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("LightSteelBlue")]
            public Color Color1
            {
                get => (Color)this[nameof(Color1)];
                set => this[nameof(Color1)] = value;
            }

            [UserScopedSetting, DefaultSettingValue("Navy")]
            public Color Color2
            {
                get => (Color)this[nameof(Color2)];
                set => this[nameof(Color2)] = value;
            }
        }
    }
}