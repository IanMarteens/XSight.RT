namespace RayEd
{
    partial class NoiseForm
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
        /// Required colorMap for Designer support - do not modify
        /// the contents of this colorMap with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbMethod = new System.Windows.Forms.ComboBox();
            this.color02 = new System.Windows.Forms.Panel();
            this.color01 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.cbTurbulence = new System.Windows.Forms.ComboBox();
            this.cbColorMap = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.edSampleHeight = new System.Windows.Forms.MaskedTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.edSampleWidth = new System.Windows.Forms.MaskedTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.edBmpHeight = new System.Windows.Forms.MaskedTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.edBmpWidth = new System.Windows.Forms.MaskedTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bnApply = new System.Windows.Forms.Button();
            this.bnClose = new System.Windows.Forms.Button();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.txTime = new System.Windows.Forms.Label();
            this.helpProvider = new System.Windows.Forms.HelpProvider();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox
            // 
            this.groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox.Controls.Add(this.label7);
            this.groupBox.Controls.Add(this.cbMethod);
            this.groupBox.Controls.Add(this.color02);
            this.groupBox.Controls.Add(this.color01);
            this.groupBox.Controls.Add(this.label6);
            this.groupBox.Controls.Add(this.cbTurbulence);
            this.groupBox.Controls.Add(this.cbColorMap);
            this.groupBox.Controls.Add(this.label5);
            this.groupBox.Controls.Add(this.edSampleHeight);
            this.groupBox.Controls.Add(this.label4);
            this.groupBox.Controls.Add(this.edSampleWidth);
            this.groupBox.Controls.Add(this.label3);
            this.groupBox.Controls.Add(this.edBmpHeight);
            this.groupBox.Controls.Add(this.label2);
            this.groupBox.Controls.Add(this.edBmpWidth);
            this.groupBox.Controls.Add(this.label1);
            this.groupBox.Location = new System.Drawing.Point(12, 8);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(251, 244);
            this.groupBox.TabIndex = 0;
            this.groupBox.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(22, 129);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "Method";
            // 
            // cbMethod
            // 
            this.cbMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMethod.FormattingEnabled = true;
            this.cbMethod.Items.AddRange(new object[] {
            "Perlin noise",
            "Crackle",
            "Bubbles"});
            this.cbMethod.Location = new System.Drawing.Point(103, 126);
            this.cbMethod.Name = "cbMethod";
            this.cbMethod.Size = new System.Drawing.Size(121, 21);
            this.cbMethod.TabIndex = 9;
            // 
            // color02
            // 
            this.color02.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.color02.Location = new System.Drawing.Point(168, 207);
            this.color02.Name = "color02";
            this.color02.Size = new System.Drawing.Size(56, 21);
            this.color02.TabIndex = 15;
            this.toolTip.SetToolTip(this.color02, "Double click to select color");
            this.color02.DoubleClick += new System.EventHandler(this.ColorPanel_DoubleClick);
            this.color02.MouseLeave += new System.EventHandler(this.ColorPanel_MouseLeave);
            this.color02.MouseEnter += new System.EventHandler(this.ColorPanel_MouseEnter);
            // 
            // color01
            // 
            this.color01.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.color01.Location = new System.Drawing.Point(103, 207);
            this.color01.Name = "color01";
            this.color01.Size = new System.Drawing.Size(56, 21);
            this.color01.TabIndex = 14;
            this.toolTip.SetToolTip(this.color01, "Double click to select color");
            this.color01.DoubleClick += new System.EventHandler(this.ColorPanel_DoubleClick);
            this.color01.MouseLeave += new System.EventHandler(this.ColorPanel_MouseLeave);
            this.color01.MouseEnter += new System.EventHandler(this.ColorPanel_MouseEnter);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(22, 156);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(61, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Turbulence";
            // 
            // cbTurbulence
            // 
            this.cbTurbulence.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTurbulence.FormattingEnabled = true;
            this.cbTurbulence.Items.AddRange(new object[] {
            "- None -",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8"});
            this.cbTurbulence.Location = new System.Drawing.Point(103, 153);
            this.cbTurbulence.Name = "cbTurbulence";
            this.cbTurbulence.Size = new System.Drawing.Size(121, 21);
            this.cbTurbulence.TabIndex = 11;
            // 
            // cbColorMap
            // 
            this.cbColorMap.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbColorMap.FormattingEnabled = true;
            this.cbColorMap.Items.AddRange(new object[] {
            "Shades of gray",
            "Full color",
            "Interpolation"});
            this.cbColorMap.Location = new System.Drawing.Point(103, 180);
            this.cbColorMap.Name = "cbColorMap";
            this.cbColorMap.Size = new System.Drawing.Size(121, 21);
            this.cbColorMap.TabIndex = 13;
            this.cbColorMap.SelectedIndexChanged += new System.EventHandler(this.ColorMap_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(23, 183);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Color map";
            // 
            // edSampleHeight
            // 
            this.edSampleHeight.AllowPromptAsInput = false;
            this.edSampleHeight.CutCopyMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            this.edSampleHeight.Location = new System.Drawing.Point(103, 100);
            this.edSampleHeight.Mask = "9990";
            this.edSampleHeight.Name = "edSampleHeight";
            this.edSampleHeight.PromptChar = ' ';
            this.edSampleHeight.Size = new System.Drawing.Size(100, 20);
            this.edSampleHeight.TabIndex = 7;
            this.edSampleHeight.Text = "3";
            this.edSampleHeight.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 103);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(74, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Sample height";
            // 
            // edSampleWidth
            // 
            this.edSampleWidth.AllowPromptAsInput = false;
            this.edSampleWidth.CutCopyMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            this.edSampleWidth.Location = new System.Drawing.Point(103, 74);
            this.edSampleWidth.Mask = "9990";
            this.edSampleWidth.Name = "edSampleWidth";
            this.edSampleWidth.PromptChar = ' ';
            this.edSampleWidth.Size = new System.Drawing.Size(100, 20);
            this.edSampleWidth.TabIndex = 5;
            this.edSampleWidth.Text = "4";
            this.edSampleWidth.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Sample width";
            // 
            // edBmpHeight
            // 
            this.edBmpHeight.AllowPromptAsInput = false;
            this.edBmpHeight.CutCopyMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            this.edBmpHeight.Location = new System.Drawing.Point(103, 48);
            this.edBmpHeight.Mask = "9990";
            this.edBmpHeight.Name = "edBmpHeight";
            this.edBmpHeight.PromptChar = ' ';
            this.edBmpHeight.Size = new System.Drawing.Size(100, 20);
            this.edBmpHeight.TabIndex = 3;
            this.edBmpHeight.Text = "150";
            this.edBmpHeight.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Bitmap height";
            // 
            // edBmpWidth
            // 
            this.edBmpWidth.AllowPromptAsInput = false;
            this.edBmpWidth.CutCopyMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            this.edBmpWidth.Location = new System.Drawing.Point(103, 22);
            this.edBmpWidth.Mask = "9990";
            this.edBmpWidth.Name = "edBmpWidth";
            this.edBmpWidth.PromptChar = ' ';
            this.edBmpWidth.Size = new System.Drawing.Size(100, 20);
            this.edBmpWidth.TabIndex = 1;
            this.edBmpWidth.Text = "200";
            this.edBmpWidth.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Bitmap width";
            // 
            // bnApply
            // 
            this.bnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bnApply.Location = new System.Drawing.Point(107, 264);
            this.bnApply.Name = "bnApply";
            this.bnApply.Size = new System.Drawing.Size(75, 23);
            this.bnApply.TabIndex = 1;
            this.bnApply.Text = "Apply";
            this.bnApply.Click += new System.EventHandler(this.Apply_Click);
            // 
            // bnClose
            // 
            this.bnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bnClose.Location = new System.Drawing.Point(188, 264);
            this.bnClose.Name = "bnClose";
            this.bnClose.Size = new System.Drawing.Size(75, 23);
            this.bnClose.TabIndex = 2;
            this.bnClose.Text = "&Close";
            this.bnClose.Click += new System.EventHandler(this.Close_Click);
            // 
            // colorDialog
            // 
            this.colorDialog.FullOpen = true;
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 200;
            this.toolTip.ReshowDelay = 40;
            // 
            // txTime
            // 
            this.txTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txTime.AutoSize = true;
            this.txTime.Location = new System.Drawing.Point(15, 268);
            this.txTime.Name = "txTime";
            this.txTime.Size = new System.Drawing.Size(38, 13);
            this.txTime.TabIndex = 3;
            this.txTime.Text = "0 secs";
            // 
            // helpProvider
            // 
            this.helpProvider.HelpNamespace = "xsight.chm";
            // 
            // NoiseForm
            // 
            this.AcceptButton = this.bnApply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bnClose;
            this.ClientSize = new System.Drawing.Size(275, 295);
            this.Controls.Add(this.txTime);
            this.Controls.Add(this.bnClose);
            this.Controls.Add(this.bnApply);
            this.Controls.Add(this.groupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.helpProvider.SetHelpKeyword(this, "noiseform.html");
            this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NoiseForm";
            this.Opacity = 0.85;
            this.helpProvider.SetShowHelp(this, true);
            this.ShowInTaskbar = false;
            this.Text = "Noise Generator";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NoiseForm_FormClosed);
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.ComboBox cbColorMap;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.MaskedTextBox edSampleHeight;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.MaskedTextBox edSampleWidth;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.MaskedTextBox edBmpHeight;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MaskedTextBox edBmpWidth;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bnApply;
        private System.Windows.Forms.Button bnClose;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbTurbulence;
        private System.Windows.Forms.Panel color02;
        private System.Windows.Forms.Panel color01;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbMethod;
        private System.Windows.Forms.Label txTime;
        private System.Windows.Forms.HelpProvider helpProvider;
    }
}