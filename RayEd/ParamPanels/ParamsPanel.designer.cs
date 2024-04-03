namespace RayEd
{
    partial class ParamsPanel
    {
        /// <summary> 
        /// Variable del diseñador requerida.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben eliminar; false en caso contrario, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary> 
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new ToolTip(components);
            flowLayout = new FlowLayoutPanel();
            renderPanel = new TaskPanel();
            bxKeepHeight = new CheckBox();
            cbQual = new ComboBox();
            tbClock = new Slider();
            txQuality = new Label();
            bxDualMode = new CheckBox();
            udClock = new NumericUpDown();
            bxRotateCamera = new CheckBox();
            txClock = new Label();
            blurPanel = new TaskPanel();
            bxMotionBlur = new CheckBox();
            udSamples = new NumericUpDown();
            txSamples = new Label();
            txWidth = new Label();
            udWidth = new NumericUpDown();
            coolingPanel = new TaskPanel();
            edCooling = new DomainUpDown();
            txCooling = new Label();
            flowLayout.SuspendLayout();
            renderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)udClock).BeginInit();
            blurPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)udSamples).BeginInit();
            ((System.ComponentModel.ISupportInitialize)udWidth).BeginInit();
            coolingPanel.SuspendLayout();
            SuspendLayout();
            // 
            // toolTip
            // 
            toolTip.ToolTipTitle = "Rotation angle";
            // 
            // flowLayout
            // 
            flowLayout.AutoSize = true;
            flowLayout.Controls.Add(renderPanel);
            flowLayout.Controls.Add(blurPanel);
            flowLayout.Controls.Add(coolingPanel);
            flowLayout.Dock = DockStyle.Fill;
            flowLayout.Location = new Point(0, 0);
            flowLayout.Margin = new Padding(0);
            flowLayout.MaximumSize = new Size(172, 900);
            flowLayout.MinimumSize = new Size(172, 184);
            flowLayout.Name = "flowLayout";
            flowLayout.Size = new Size(172, 367);
            flowLayout.TabIndex = 0;
            // 
            // renderPanel
            // 
            renderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            renderPanel.Controls.Add(bxKeepHeight);
            renderPanel.Controls.Add(cbQual);
            renderPanel.Controls.Add(tbClock);
            renderPanel.Controls.Add(txQuality);
            renderPanel.Controls.Add(bxDualMode);
            renderPanel.Controls.Add(udClock);
            renderPanel.Controls.Add(bxRotateCamera);
            renderPanel.Controls.Add(txClock);
            renderPanel.Location = new Point(0, 0);
            renderPanel.Margin = new Padding(0, 0, 0, 8);
            renderPanel.Name = "renderPanel";
            renderPanel.Size = new Size(172, 182);
            renderPanel.TabIndex = 0;
            renderPanel.Text = "Render";
            // 
            // bxKeepHeight
            // 
            bxKeepHeight.AutoSize = true;
            bxKeepHeight.BackColor = Color.Transparent;
            bxKeepHeight.Font = new Font("Tahoma", 8F);
            bxKeepHeight.Location = new Point(62, 102);
            bxKeepHeight.Name = "bxKeepHeight";
            bxKeepHeight.Size = new Size(83, 17);
            bxKeepHeight.TabIndex = 4;
            bxKeepHeight.Text = "Keep height";
            bxKeepHeight.UseVisualStyleBackColor = false;
            // 
            // cbQual
            // 
            cbQual.DropDownStyle = ComboBoxStyle.DropDownList;
            cbQual.Font = new Font("Tahoma", 8F);
            cbQual.FormattingEnabled = true;
            cbQual.Items.AddRange(new object[] { "Sonar", "Draft", "Textured draft", "Basic", "Good enough", "Optimal" });
            cbQual.Location = new Point(62, 30);
            cbQual.Name = "cbQual";
            cbQual.Size = new Size(100, 21);
            cbQual.TabIndex = 1;
            cbQual.TabStop = false;
            // 
            // tbClock
            // 
            tbClock.BackColor = Color.Transparent;
            tbClock.Location = new Point(8, 155);
            tbClock.Name = "tbClock";
            tbClock.Size = new Size(156, 22);
            tbClock.TabIndex = 7;
            tbClock.ValueChanged += TrackBarClock_ValueChanged;
            // 
            // txQuality
            // 
            txQuality.BackColor = Color.Transparent;
            txQuality.Font = new Font("Tahoma", 8F);
            txQuality.Location = new Point(6, 29);
            txQuality.Name = "txQuality";
            txQuality.Size = new Size(55, 21);
            txQuality.TabIndex = 0;
            txQuality.Text = "Quality";
            txQuality.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bxDualMode
            // 
            bxDualMode.AutoSize = true;
            bxDualMode.BackColor = Color.Transparent;
            bxDualMode.Font = new Font("Tahoma", 8F);
            bxDualMode.Location = new Point(62, 54);
            bxDualMode.Name = "bxDualMode";
            bxDualMode.Size = new Size(94, 17);
            bxDualMode.TabIndex = 2;
            bxDualMode.Text = "Multithreading";
            bxDualMode.UseVisualStyleBackColor = false;
            // 
            // udClock
            // 
            udClock.DecimalPlaces = 3;
            udClock.Font = new Font("Tahoma", 8F);
            udClock.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            udClock.Location = new Point(62, 126);
            udClock.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            udClock.Name = "udClock";
            udClock.Size = new Size(100, 20);
            udClock.TabIndex = 6;
            udClock.TabStop = false;
            udClock.ValueChanged += Clock_ValueChanged;
            // 
            // bxRotateCamera
            // 
            bxRotateCamera.AutoSize = true;
            bxRotateCamera.BackColor = Color.Transparent;
            bxRotateCamera.Font = new Font("Tahoma", 8F);
            bxRotateCamera.Location = new Point(62, 78);
            bxRotateCamera.Name = "bxRotateCamera";
            bxRotateCamera.Size = new Size(97, 17);
            bxRotateCamera.TabIndex = 3;
            bxRotateCamera.Text = "Rotate camera";
            bxRotateCamera.UseVisualStyleBackColor = false;
            // 
            // txClock
            // 
            txClock.BackColor = Color.Transparent;
            txClock.Font = new Font("Tahoma", 8F);
            txClock.Location = new Point(6, 126);
            txClock.Name = "txClock";
            txClock.Size = new Size(51, 20);
            txClock.TabIndex = 5;
            txClock.Text = "Clock";
            txClock.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // blurPanel
            // 
            blurPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            blurPanel.Controls.Add(bxMotionBlur);
            blurPanel.Controls.Add(udSamples);
            blurPanel.Controls.Add(txSamples);
            blurPanel.Controls.Add(txWidth);
            blurPanel.Controls.Add(udWidth);
            blurPanel.Location = new Point(0, 190);
            blurPanel.Margin = new Padding(0, 0, 0, 8);
            blurPanel.Name = "blurPanel";
            blurPanel.Size = new Size(172, 108);
            blurPanel.TabIndex = 1;
            blurPanel.Text = "Motion Blur";
            // 
            // bxMotionBlur
            // 
            bxMotionBlur.AutoSize = true;
            bxMotionBlur.BackColor = Color.Transparent;
            bxMotionBlur.Font = new Font("Tahoma", 8F);
            bxMotionBlur.Location = new Point(62, 30);
            bxMotionBlur.Name = "bxMotionBlur";
            bxMotionBlur.Size = new Size(100, 17);
            bxMotionBlur.TabIndex = 0;
            bxMotionBlur.Text = "Use motion blur";
            bxMotionBlur.UseVisualStyleBackColor = false;
            // 
            // udSamples
            // 
            udSamples.Font = new Font("Tahoma", 8F);
            udSamples.Location = new Point(62, 54);
            udSamples.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            udSamples.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            udSamples.Name = "udSamples";
            udSamples.Size = new Size(100, 20);
            udSamples.TabIndex = 2;
            udSamples.TabStop = false;
            udSamples.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // txSamples
            // 
            txSamples.BackColor = Color.Transparent;
            txSamples.Font = new Font("Tahoma", 8F);
            txSamples.Location = new Point(6, 54);
            txSamples.Name = "txSamples";
            txSamples.Size = new Size(55, 21);
            txSamples.TabIndex = 1;
            txSamples.Text = "Samples";
            txSamples.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txWidth
            // 
            txWidth.BackColor = Color.Transparent;
            txWidth.Font = new Font("Tahoma", 8F);
            txWidth.Location = new Point(6, 78);
            txWidth.Name = "txWidth";
            txWidth.Size = new Size(51, 21);
            txWidth.TabIndex = 3;
            txWidth.Text = "Width";
            txWidth.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // udWidth
            // 
            udWidth.DecimalPlaces = 3;
            udWidth.Font = new Font("Tahoma", 8F);
            udWidth.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            udWidth.Location = new Point(62, 78);
            udWidth.Maximum = new decimal(new int[] { 500, 0, 0, 196608 });
            udWidth.Minimum = new decimal(new int[] { 2, 0, 0, 196608 });
            udWidth.Name = "udWidth";
            udWidth.Size = new Size(100, 20);
            udWidth.TabIndex = 4;
            udWidth.TabStop = false;
            udWidth.Value = new decimal(new int[] { 2, 0, 0, 196608 });
            // 
            // coolingPanel
            // 
            coolingPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            coolingPanel.Controls.Add(edCooling);
            coolingPanel.Controls.Add(txCooling);
            coolingPanel.Location = new Point(0, 306);
            coolingPanel.Margin = new Padding(0, 0, 0, 4);
            coolingPanel.Name = "coolingPanel";
            coolingPanel.Size = new Size(172, 57);
            coolingPanel.TabIndex = 2;
            coolingPanel.Text = "Cooling";
            // 
            // edCooling
            // 
            edCooling.BackColor = SystemColors.Window;
            edCooling.Font = new Font("Tahoma", 8F);
            edCooling.Items.Add("None");
            edCooling.Items.Add("100 msecs");
            edCooling.Items.Add("200 msecs");
            edCooling.Items.Add("500 msecs");
            edCooling.Items.Add("1000 msecs");
            edCooling.Location = new Point(62, 30);
            edCooling.Name = "edCooling";
            edCooling.ReadOnly = true;
            edCooling.Size = new Size(100, 20);
            edCooling.TabIndex = 1;
            edCooling.TabStop = false;
            edCooling.Text = "None";
            edCooling.TextAlign = HorizontalAlignment.Right;
            edCooling.Wrap = true;
            edCooling.SelectedItemChanged += Cooling_SelectedItemChanged;
            // 
            // txCooling
            // 
            txCooling.BackColor = Color.Transparent;
            txCooling.Font = new Font("Tahoma", 8F);
            txCooling.Location = new Point(6, 30);
            txCooling.Name = "txCooling";
            txCooling.Size = new Size(51, 21);
            txCooling.TabIndex = 0;
            txCooling.Text = "Pause";
            txCooling.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ParamsPanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            Controls.Add(flowLayout);
            Font = new Font("Tahoma", 8.25F);
            Name = "ParamsPanel";
            Size = new Size(173, 367);
            flowLayout.ResumeLayout(false);
            renderPanel.ResumeLayout(false);
            renderPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)udClock).EndInit();
            blurPanel.ResumeLayout(false);
            blurPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)udSamples).EndInit();
            ((System.ComponentModel.ISupportInitialize)udWidth).EndInit();
            coolingPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.NumericUpDown udWidth;
        private System.Windows.Forms.Label txWidth;
        private System.Windows.Forms.Label txClock;
        private System.Windows.Forms.ComboBox cbQual;
        private System.Windows.Forms.NumericUpDown udSamples;
        private System.Windows.Forms.Label txQuality;
        private System.Windows.Forms.Label txSamples;
        private System.Windows.Forms.CheckBox bxMotionBlur;
        private System.Windows.Forms.Label txCooling;
        private System.Windows.Forms.DomainUpDown edCooling;
        private System.Windows.Forms.NumericUpDown udClock;
        private System.Windows.Forms.CheckBox bxDualMode;
        private System.Windows.Forms.CheckBox bxRotateCamera;
        private System.Windows.Forms.ToolTip toolTip;
        private TaskPanel coolingPanel;
        private TaskPanel blurPanel;
        private TaskPanel renderPanel;
        private System.Windows.Forms.FlowLayoutPanel flowLayout;
        private Slider tbClock;
        private System.Windows.Forms.CheckBox bxKeepHeight;
    }
}
