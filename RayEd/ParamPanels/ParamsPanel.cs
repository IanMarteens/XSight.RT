using IntSight.RayTracing.Engine;
using IntSight.RayTracing.Language;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Forms;

namespace RayEd
{
    public partial class ParamsPanel : UserControl
    {
        public ParamsPanel()
        {
            InitializeComponent();
            // Read the initial quality and cooling pause.
            ParamsSettings settings = new();
            cbQual.DataBindings.Add("SelectedIndex", settings, "Quality");
            bxDualMode.DataBindings.Add("Checked", settings, "Dual");
            bxRotateCamera.DataBindings.Add("Checked", settings, "RotateCamera");
            bxKeepHeight.DataBindings.Add("Checked", settings, "KeepHeight");
            edCooling.SelectedIndex = 0;
            ShowRotationAngle();
        }

        [Browsable(false)]
        public RenderProcessor RenderProcessor { get; set; }

        /// <summary>Number of samples taken for motion blur.</summary>
        [Browsable(false)]
        public int Samples => Convert.ToInt32(udSamples.Value);

        /// <summary>Size of the sampled area, in time units.</summary>
        [Browsable(false)]
        public double SamplingWidth => Convert.ToDouble(udWidth.Value);

        /// <summary>Gets the render quality.</summary>
        [Browsable(false)]
        public RenderMode RenderMode => cbQual.SelectedIndex switch
        {
            0 => RenderMode.Sonar,
            1 => RenderMode.Draft,
            2 => RenderMode.TexturedDraft,
            3 => RenderMode.Basic,
            4 => RenderMode.GoodEnough,
            _ => RenderMode.Normal,
        };

        [Browsable(false)]
        public bool DualMode => bxDualMode.Checked;

        [Browsable(false)]
        public bool RotateCamera => bxRotateCamera.Checked;

        [Browsable(false)]
        public bool KeepHeight => bxKeepHeight.Checked;

        [Browsable(false)]
        public double Clock => Convert.ToDouble(udClock.Value);

        [Browsable(false)]
        public bool MotionBlur => bxMotionBlur.Checked;

        private bool clockChanging;

        private void TrackBarClock_ValueChanged(object sender, EventArgs e)
        {
            if (!clockChanging)
            {
                clockChanging = true;
                try
                {
                    udClock.Value = Convert.ToDecimal(tbClock.Value / 100.0);
                    ShowRotationAngle();
                }
                finally
                {
                    clockChanging = false;
                }
            }
        }

        private void Clock_ValueChanged(object sender, EventArgs e)
        {
            if (!clockChanging)
            {
                clockChanging = true;
                try
                {
                    tbClock.Value = Convert.ToInt32(Math.Round(udClock.Value * 100));
                    ShowRotationAngle();
                }
                finally
                {
                    clockChanging = false;
                }
            }
        }

        private void ShowRotationAngle() =>
            toolTip.SetToolTip(bxRotateCamera, Convert2Deg((double)udClock.Value * 360.0));

        private static string Convert2Deg(double value)
        {
            int degrees = (int)value;
            int minutes = (int)Math.Round((value - degrees) * 60);
            return minutes == 0 
                ? string.Format("{0}º", degrees) : string.Format("{0}º {1}'", degrees, minutes);
        }

        private void Cooling_SelectedItemChanged(object sender, EventArgs e)
        {
            if (RenderProcessor != null)
                switch (edCooling.SelectedIndex)
                {
                    case 0: RenderProcessor.CoolingPause = 0; break;
                    case 1: RenderProcessor.CoolingPause = 100; break;
                    case 2: RenderProcessor.CoolingPause = 200; break;
                    case 3: RenderProcessor.CoolingPause = 500; break;
                    case 4: RenderProcessor.CoolingPause = 1000; break;
                }
        }

        private sealed class ParamsSettings : ApplicationSettingsBase
        {
            public ParamsSettings() => PropertyChanged += delegate { Save(); };

            [UserScopedSetting(), DefaultSettingValue("5")]
            public int Quality
            {
                get => (int)this[nameof(Quality)];
                set => this[nameof(Quality)] = value;
            }

            [UserScopedSetting(), DefaultSettingValue("True")]
            public bool Dual
            {
                get => (bool)this[nameof(Dual)];
                set => this[nameof(Dual)] = value;
            }

            [UserScopedSetting(), DefaultSettingValue("False")]
            public bool RotateCamera
            {
                get => (bool)this[nameof(RotateCamera)];
                set => this[nameof(RotateCamera)] = value;
            }

            [UserScopedSetting(), DefaultSettingValue("False")]
            public bool KeepHeight
            {
                get => (bool)this[nameof(KeepHeight)];
                set => this[nameof(KeepHeight)] = value;
            }
        }
    }
}
