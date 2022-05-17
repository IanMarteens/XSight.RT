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
            this.components = new System.ComponentModel.Container();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.flowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.renderPanel = new RayEd.TaskPanel();
            this.cbQual = new System.Windows.Forms.ComboBox();
            this.tbClock = new RayEd.Slider();
            this.txQuality = new System.Windows.Forms.Label();
            this.bxDualMode = new System.Windows.Forms.CheckBox();
            this.udClock = new System.Windows.Forms.NumericUpDown();
            this.bxRotateCamera = new System.Windows.Forms.CheckBox();
            this.txClock = new System.Windows.Forms.Label();
            this.blurPanel = new RayEd.TaskPanel();
            this.bxMotionBlur = new System.Windows.Forms.CheckBox();
            this.udSamples = new System.Windows.Forms.NumericUpDown();
            this.txSamples = new System.Windows.Forms.Label();
            this.txWidth = new System.Windows.Forms.Label();
            this.udWidth = new System.Windows.Forms.NumericUpDown();
            this.coolingPanel = new RayEd.TaskPanel();
            this.edCooling = new System.Windows.Forms.DomainUpDown();
            this.txCooling = new System.Windows.Forms.Label();
            this.bxKeepHeight = new System.Windows.Forms.CheckBox();
            this.flowLayout.SuspendLayout();
            this.renderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udClock)).BeginInit();
            this.blurPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udSamples)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udWidth)).BeginInit();
            this.coolingPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolTip
            // 
            this.toolTip.ToolTipTitle = "Rotation angle";
            // 
            // flowLayout
            // 
            this.flowLayout.AutoSize = true;
            this.flowLayout.Controls.Add(this.renderPanel);
            this.flowLayout.Controls.Add(this.blurPanel);
            this.flowLayout.Controls.Add(this.coolingPanel);
            this.flowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayout.Location = new System.Drawing.Point(0, 0);
            this.flowLayout.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayout.MaximumSize = new System.Drawing.Size(172, 900);
            this.flowLayout.MinimumSize = new System.Drawing.Size(172, 184);
            this.flowLayout.Name = "flowLayout";
            this.flowLayout.Size = new System.Drawing.Size(172, 367);
            this.flowLayout.TabIndex = 0;
            // 
            // renderPanel
            // 
            this.renderPanel.Controls.Add(this.bxKeepHeight);
            this.renderPanel.Controls.Add(this.cbQual);
            this.renderPanel.Controls.Add(this.tbClock);
            this.renderPanel.Controls.Add(this.txQuality);
            this.renderPanel.Controls.Add(this.bxDualMode);
            this.renderPanel.Controls.Add(this.udClock);
            this.renderPanel.Controls.Add(this.bxRotateCamera);
            this.renderPanel.Controls.Add(this.txClock);
            this.renderPanel.Location = new System.Drawing.Point(0, 0);
            this.renderPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.renderPanel.Name = "renderPanel";
            this.renderPanel.Size = new System.Drawing.Size(172, 182);
            this.renderPanel.TabIndex = 0;
            this.renderPanel.Text = "Render";
            // 
            // cbQual
            // 
            this.cbQual.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbQual.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbQual.FormattingEnabled = true;
            this.cbQual.Items.AddRange(new object[] {
            "Sonar",
            "Draft",
            "Textured draft",
            "Basic",
            "Good enough",
            "Optimal"});
            this.cbQual.Location = new System.Drawing.Point(62, 30);
            this.cbQual.Name = "cbQual";
            this.cbQual.Size = new System.Drawing.Size(100, 24);
            this.cbQual.TabIndex = 1;
            this.cbQual.TabStop = false;
            // 
            // tbClock
            // 
            this.tbClock.BackColor = System.Drawing.Color.Transparent;
            this.tbClock.Location = new System.Drawing.Point(8, 155);
            this.tbClock.Name = "tbClock";
            this.tbClock.Size = new System.Drawing.Size(156, 22);
            this.tbClock.TabIndex = 7;
            this.tbClock.ValueChanged += new System.EventHandler(this.TrackBarClock_ValueChanged);
            // 
            // txQuality
            // 
            this.txQuality.BackColor = System.Drawing.Color.Transparent;
            this.txQuality.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txQuality.Location = new System.Drawing.Point(6, 29);
            this.txQuality.Name = "txQuality";
            this.txQuality.Size = new System.Drawing.Size(55, 21);
            this.txQuality.TabIndex = 0;
            this.txQuality.Text = "Quality";
            this.txQuality.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bxDualMode
            // 
            this.bxDualMode.AutoSize = true;
            this.bxDualMode.BackColor = System.Drawing.Color.Transparent;
            this.bxDualMode.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bxDualMode.Location = new System.Drawing.Point(62, 54);
            this.bxDualMode.Name = "bxDualMode";
            this.bxDualMode.Size = new System.Drawing.Size(115, 21);
            this.bxDualMode.TabIndex = 2;
            this.bxDualMode.Text = "Multithreading";
            this.bxDualMode.UseVisualStyleBackColor = false;
            // 
            // udClock
            // 
            this.udClock.DecimalPlaces = 3;
            this.udClock.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.udClock.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.udClock.Location = new System.Drawing.Point(62, 126);
            this.udClock.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udClock.Name = "udClock";
            this.udClock.Size = new System.Drawing.Size(100, 24);
            this.udClock.TabIndex = 6;
            this.udClock.TabStop = false;
            this.udClock.ValueChanged += new System.EventHandler(this.Clock_ValueChanged);
            // 
            // bxRotateCamera
            // 
            this.bxRotateCamera.AutoSize = true;
            this.bxRotateCamera.BackColor = System.Drawing.Color.Transparent;
            this.bxRotateCamera.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bxRotateCamera.Location = new System.Drawing.Point(62, 78);
            this.bxRotateCamera.Name = "bxRotateCamera";
            this.bxRotateCamera.Size = new System.Drawing.Size(120, 21);
            this.bxRotateCamera.TabIndex = 3;
            this.bxRotateCamera.Text = "Rotate camera";
            this.bxRotateCamera.UseVisualStyleBackColor = false;
            // 
            // txClock
            // 
            this.txClock.BackColor = System.Drawing.Color.Transparent;
            this.txClock.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txClock.Location = new System.Drawing.Point(6, 126);
            this.txClock.Name = "txClock";
            this.txClock.Size = new System.Drawing.Size(51, 20);
            this.txClock.TabIndex = 5;
            this.txClock.Text = "Clock";
            this.txClock.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // blurPanel
            // 
            this.blurPanel.Controls.Add(this.bxMotionBlur);
            this.blurPanel.Controls.Add(this.udSamples);
            this.blurPanel.Controls.Add(this.txSamples);
            this.blurPanel.Controls.Add(this.txWidth);
            this.blurPanel.Controls.Add(this.udWidth);
            this.blurPanel.Location = new System.Drawing.Point(0, 190);
            this.blurPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.blurPanel.Name = "blurPanel";
            this.blurPanel.Size = new System.Drawing.Size(172, 108);
            this.blurPanel.TabIndex = 1;
            this.blurPanel.Text = "Motion Blur";
            // 
            // bxMotionBlur
            // 
            this.bxMotionBlur.AutoSize = true;
            this.bxMotionBlur.BackColor = System.Drawing.Color.Transparent;
            this.bxMotionBlur.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bxMotionBlur.Location = new System.Drawing.Point(62, 30);
            this.bxMotionBlur.Name = "bxMotionBlur";
            this.bxMotionBlur.Size = new System.Drawing.Size(126, 21);
            this.bxMotionBlur.TabIndex = 0;
            this.bxMotionBlur.Text = "Use motion blur";
            this.bxMotionBlur.UseVisualStyleBackColor = false;
            // 
            // udSamples
            // 
            this.udSamples.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.udSamples.Location = new System.Drawing.Point(62, 54);
            this.udSamples.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.udSamples.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.udSamples.Name = "udSamples";
            this.udSamples.Size = new System.Drawing.Size(100, 24);
            this.udSamples.TabIndex = 2;
            this.udSamples.TabStop = false;
            this.udSamples.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // txSamples
            // 
            this.txSamples.BackColor = System.Drawing.Color.Transparent;
            this.txSamples.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txSamples.Location = new System.Drawing.Point(6, 54);
            this.txSamples.Name = "txSamples";
            this.txSamples.Size = new System.Drawing.Size(55, 21);
            this.txSamples.TabIndex = 1;
            this.txSamples.Text = "Samples";
            this.txSamples.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txWidth
            // 
            this.txWidth.BackColor = System.Drawing.Color.Transparent;
            this.txWidth.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txWidth.Location = new System.Drawing.Point(6, 78);
            this.txWidth.Name = "txWidth";
            this.txWidth.Size = new System.Drawing.Size(51, 21);
            this.txWidth.TabIndex = 3;
            this.txWidth.Text = "Width";
            this.txWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // udWidth
            // 
            this.udWidth.DecimalPlaces = 3;
            this.udWidth.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.udWidth.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.udWidth.Location = new System.Drawing.Point(62, 78);
            this.udWidth.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            196608});
            this.udWidth.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            196608});
            this.udWidth.Name = "udWidth";
            this.udWidth.Size = new System.Drawing.Size(100, 24);
            this.udWidth.TabIndex = 4;
            this.udWidth.TabStop = false;
            this.udWidth.Value = new decimal(new int[] {
            2,
            0,
            0,
            196608});
            // 
            // coolingPanel
            // 
            this.coolingPanel.Controls.Add(this.edCooling);
            this.coolingPanel.Controls.Add(this.txCooling);
            this.coolingPanel.Location = new System.Drawing.Point(0, 306);
            this.coolingPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.coolingPanel.Name = "coolingPanel";
            this.coolingPanel.Size = new System.Drawing.Size(172, 57);
            this.coolingPanel.TabIndex = 2;
            this.coolingPanel.Text = "Cooling";
            // 
            // edCooling
            // 
            this.edCooling.BackColor = System.Drawing.SystemColors.Window;
            this.edCooling.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.edCooling.Items.Add("None");
            this.edCooling.Items.Add("100 msecs");
            this.edCooling.Items.Add("200 msecs");
            this.edCooling.Items.Add("500 msecs");
            this.edCooling.Items.Add("1000 msecs");
            this.edCooling.Location = new System.Drawing.Point(62, 30);
            this.edCooling.Name = "edCooling";
            this.edCooling.ReadOnly = true;
            this.edCooling.Size = new System.Drawing.Size(100, 24);
            this.edCooling.TabIndex = 1;
            this.edCooling.TabStop = false;
            this.edCooling.Text = "None";
            this.edCooling.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.edCooling.Wrap = true;
            this.edCooling.SelectedItemChanged += new System.EventHandler(this.Cooling_SelectedItemChanged);
            // 
            // txCooling
            // 
            this.txCooling.BackColor = System.Drawing.Color.Transparent;
            this.txCooling.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txCooling.Location = new System.Drawing.Point(6, 30);
            this.txCooling.Name = "txCooling";
            this.txCooling.Size = new System.Drawing.Size(51, 21);
            this.txCooling.TabIndex = 0;
            this.txCooling.Text = "Pause";
            this.txCooling.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bxKeepHeight
            // 
            this.bxKeepHeight.AutoSize = true;
            this.bxKeepHeight.BackColor = System.Drawing.Color.Transparent;
            this.bxKeepHeight.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bxKeepHeight.Location = new System.Drawing.Point(62, 102);
            this.bxKeepHeight.Name = "bxKeepHeight";
            this.bxKeepHeight.Size = new System.Drawing.Size(102, 21);
            this.bxKeepHeight.TabIndex = 4;
            this.bxKeepHeight.Text = "Keep height";
            this.bxKeepHeight.UseVisualStyleBackColor = false;
            // 
            // ParamsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.flowLayout);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Name = "ParamsPanel";
            this.Size = new System.Drawing.Size(172, 367);
            this.flowLayout.ResumeLayout(false);
            this.renderPanel.ResumeLayout(false);
            this.renderPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udClock)).EndInit();
            this.blurPanel.ResumeLayout(false);
            this.blurPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udSamples)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udWidth)).EndInit();
            this.coolingPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
