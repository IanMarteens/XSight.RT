namespace RayEd
{
    partial class BenchForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.bxBackground = new System.Windows.Forms.CheckBox();
            this.bxMultithreading = new System.Windows.Forms.CheckBox();
            this.cbBenchmarks = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bnRun = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox
            // 
            this.groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox.Controls.Add(this.bxBackground);
            this.groupBox.Controls.Add(this.bxMultithreading);
            this.groupBox.Controls.Add(this.cbBenchmarks);
            this.groupBox.Controls.Add(this.label1);
            this.groupBox.Location = new System.Drawing.Point(12, 8);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(251, 101);
            this.groupBox.TabIndex = 0;
            this.groupBox.TabStop = false;
            // 
            // bxBackground
            // 
            this.bxBackground.AutoSize = true;
            this.bxBackground.Location = new System.Drawing.Point(86, 69);
            this.bxBackground.Name = "bxBackground";
            this.bxBackground.Size = new System.Drawing.Size(117, 17);
            this.bxBackground.TabIndex = 3;
            this.bxBackground.Text = "Run in background";
            this.bxBackground.UseVisualStyleBackColor = true;
            this.bxBackground.CheckedChanged += new System.EventHandler(this.Background_CheckedChanged);
            // 
            // bxMultithreading
            // 
            this.bxMultithreading.AutoSize = true;
            this.bxMultithreading.Location = new System.Drawing.Point(86, 46);
            this.bxMultithreading.Name = "bxMultithreading";
            this.bxMultithreading.Size = new System.Drawing.Size(113, 17);
            this.bxMultithreading.TabIndex = 2;
            this.bxMultithreading.Text = "Use multithreading";
            this.bxMultithreading.UseVisualStyleBackColor = true;
            this.bxMultithreading.CheckedChanged += new System.EventHandler(this.Multithreading_CheckedChanged);
            // 
            // cbBenchmarks
            // 
            this.cbBenchmarks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBenchmarks.FormattingEnabled = true;
            this.cbBenchmarks.Location = new System.Drawing.Point(86, 19);
            this.cbBenchmarks.Name = "cbBenchmarks";
            this.cbBenchmarks.Size = new System.Drawing.Size(159, 21);
            this.cbBenchmarks.TabIndex = 1;
            this.cbBenchmarks.SelectedIndexChanged += new System.EventHandler(this.Benchmarks_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Benchmark";
            // 
            // bnRun
            // 
            this.bnRun.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.bnRun.Location = new System.Drawing.Point(100, 119);
            this.bnRun.Name = "bnRun";
            this.bnRun.Size = new System.Drawing.Size(75, 23);
            this.bnRun.TabIndex = 1;
            this.bnRun.Text = "Run";
            this.bnRun.UseVisualStyleBackColor = true;
            this.bnRun.Click += new System.EventHandler(this.Run_ClickAsync);
            // 
            // BenchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(275, 150);
            this.Controls.Add(this.bnRun);
            this.Controls.Add(this.groupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BenchForm";
            this.Opacity = 0.85;
            this.ShowInTaskbar = false;
            this.Text = "Run benchmarks";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.BenchForm_FormClosed);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BenchForm_FormClosing);
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.ComboBox cbBenchmarks;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bnRun;
        private System.Windows.Forms.CheckBox bxMultithreading;
        private System.Windows.Forms.CheckBox bxBackground;
        private System.Windows.Forms.ToolTip toolTip;
    }
}