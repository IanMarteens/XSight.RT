namespace RayEd
{
    partial class AnimationForm
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

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Ess = new System.Windows.Forms.RadioButton();
            this.Sine = new System.Windows.Forms.RadioButton();
            this.Parabolic = new System.Windows.Forms.RadioButton();
            this.SquareRoot = new System.Windows.Forms.RadioButton();
            this.Linear = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.TotalFrames = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.FrameRate = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.OutputDir = new System.Windows.Forms.TextBox();
            this.BrowseDir = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.Ok = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.LastFrame = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.FirstFrame = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.Interval = new System.Windows.Forms.CheckBox();
            this.folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            this.helpProvider = new System.Windows.Forms.HelpProvider();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TotalFrames)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FrameRate)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LastFrame)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FirstFrame)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Ess);
            this.groupBox1.Controls.Add(this.Sine);
            this.groupBox1.Controls.Add(this.Parabolic);
            this.groupBox1.Controls.Add(this.SquareRoot);
            this.groupBox1.Controls.Add(this.Linear);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(150, 141);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Time shape";
            // 
            // Ess
            // 
            this.Ess.AutoSize = true;
            this.Ess.Location = new System.Drawing.Point(20, 111);
            this.Ess.Name = "Ess";
            this.Ess.Size = new System.Drawing.Size(42, 17);
            this.Ess.TabIndex = 7;
            this.Ess.TabStop = true;
            this.Ess.Text = "\"S\"";
            this.Ess.UseVisualStyleBackColor = true;
            // 
            // Sine
            // 
            this.Sine.AutoSize = true;
            this.Sine.Location = new System.Drawing.Point(20, 88);
            this.Sine.Name = "Sine";
            this.Sine.Size = new System.Drawing.Size(100, 17);
            this.Sine.TabIndex = 3;
            this.Sine.Text = "Sine (half cycle)";
            this.Sine.UseVisualStyleBackColor = true;
            // 
            // Parabolic
            // 
            this.Parabolic.AutoSize = true;
            this.Parabolic.Location = new System.Drawing.Point(20, 42);
            this.Parabolic.Name = "Parabolic";
            this.Parabolic.Size = new System.Drawing.Size(69, 17);
            this.Parabolic.TabIndex = 1;
            this.Parabolic.Text = "Parabolic";
            this.Parabolic.UseVisualStyleBackColor = true;
            // 
            // SquareRoot
            // 
            this.SquareRoot.AutoSize = true;
            this.SquareRoot.Location = new System.Drawing.Point(20, 65);
            this.SquareRoot.Name = "SquareRoot";
            this.SquareRoot.Size = new System.Drawing.Size(80, 17);
            this.SquareRoot.TabIndex = 2;
            this.SquareRoot.Text = "Square root";
            this.SquareRoot.UseVisualStyleBackColor = true;
            // 
            // Linear
            // 
            this.Linear.AutoSize = true;
            this.Linear.Checked = true;
            this.Linear.Location = new System.Drawing.Point(20, 19);
            this.Linear.Name = "Linear";
            this.Linear.Size = new System.Drawing.Size(54, 17);
            this.Linear.TabIndex = 0;
            this.Linear.TabStop = true;
            this.Linear.Text = "Linear";
            this.Linear.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.TotalFrames);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.FrameRate);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(177, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(203, 82);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Frame";
            // 
            // TotalFrames
            // 
            this.TotalFrames.Location = new System.Drawing.Point(92, 45);
            this.TotalFrames.Maximum = new decimal(new int[] {
            50000,
            0,
            0,
            0});
            this.TotalFrames.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.TotalFrames.Name = "TotalFrames";
            this.TotalFrames.Size = new System.Drawing.Size(99, 20);
            this.TotalFrames.TabIndex = 3;
            this.TotalFrames.ThousandsSeparator = true;
            this.TotalFrames.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Total frames";
            // 
            // FrameRate
            // 
            this.FrameRate.Location = new System.Drawing.Point(92, 19);
            this.FrameRate.Maximum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this.FrameRate.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.FrameRate.Name = "FrameRate";
            this.FrameRate.Size = new System.Drawing.Size(100, 20);
            this.FrameRate.TabIndex = 1;
            this.FrameRate.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Frame rate";
            // 
            // OutputDir
            // 
            this.OutputDir.Location = new System.Drawing.Point(20, 19);
            this.OutputDir.Name = "OutputDir";
            this.OutputDir.Size = new System.Drawing.Size(296, 20);
            this.OutputDir.TabIndex = 0;
            // 
            // BrowseDir
            // 
            this.BrowseDir.Location = new System.Drawing.Point(322, 17);
            this.BrowseDir.Name = "BrowseDir";
            this.BrowseDir.Size = new System.Drawing.Size(36, 22);
            this.BrowseDir.TabIndex = 1;
            this.BrowseDir.Text = "...";
            this.BrowseDir.UseVisualStyleBackColor = true;
            this.BrowseDir.Click += new System.EventHandler(this.BrowseDir_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.OutputDir);
            this.groupBox3.Controls.Add(this.BrowseDir);
            this.groupBox3.Location = new System.Drawing.Point(11, 159);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(369, 50);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Output directory";
            // 
            // Ok
            // 
            this.Ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Ok.Location = new System.Drawing.Point(216, 218);
            this.Ok.Name = "Ok";
            this.Ok.Size = new System.Drawing.Size(75, 23);
            this.Ok.TabIndex = 5;
            this.Ok.Text = "OK";
            this.Ok.UseVisualStyleBackColor = true;
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(305, 218);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 6;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.LastFrame);
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Controls.Add(this.FirstFrame);
            this.groupBox4.Controls.Add(this.label3);
            this.groupBox4.Enabled = false;
            this.groupBox4.Location = new System.Drawing.Point(177, 100);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(203, 53);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            // 
            // LastFrame
            // 
            this.LastFrame.Location = new System.Drawing.Point(135, 20);
            this.LastFrame.Name = "LastFrame";
            this.LastFrame.Size = new System.Drawing.Size(57, 20);
            this.LastFrame.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(109, 22);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "To";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FirstFrame
            // 
            this.FirstFrame.Location = new System.Drawing.Point(51, 20);
            this.FirstFrame.Name = "FirstFrame";
            this.FirstFrame.Size = new System.Drawing.Size(52, 20);
            this.FirstFrame.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "From";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Interval
            // 
            this.Interval.AutoSize = true;
            this.Interval.Location = new System.Drawing.Point(192, 100);
            this.Interval.Name = "Interval";
            this.Interval.Size = new System.Drawing.Size(61, 17);
            this.Interval.TabIndex = 2;
            this.Interval.Text = "Interval";
            this.Interval.UseVisualStyleBackColor = true;
            this.Interval.CheckedChanged += new System.EventHandler(this.Interval_CheckedChanged);
            // 
            // folderBrowser
            // 
            this.folderBrowser.Description = "Select the output path";
            // 
            // helpProvider
            // 
            this.helpProvider.HelpNamespace = "xsight.chm";
            // 
            // AnimationForm
            // 
            this.AcceptButton = this.Ok;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(392, 253);
            this.Controls.Add(this.Interval);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.Ok);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.helpProvider.SetHelpKeyword(this, "animform.html");
            this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AnimationForm";
            this.helpProvider.SetShowHelp(this, true);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Animation";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TotalFrames)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FrameRate)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LastFrame)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FirstFrame)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown TotalFrames;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown FrameRate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton Sine;
        private System.Windows.Forms.RadioButton Parabolic;
        private System.Windows.Forms.RadioButton SquareRoot;
        private System.Windows.Forms.RadioButton Linear;
        private System.Windows.Forms.TextBox OutputDir;
        private System.Windows.Forms.Button BrowseDir;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button Ok;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox Interval;
        private System.Windows.Forms.NumericUpDown LastFrame;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown FirstFrame;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FolderBrowserDialog folderBrowser;
        private System.Windows.Forms.RadioButton Ess;
        private System.Windows.Forms.HelpProvider helpProvider;
    }
}