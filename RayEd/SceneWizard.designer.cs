namespace RayEd
{
    partial class SceneWizard
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
            this.bnCancel = new System.Windows.Forms.Button();
            this.bnOk = new System.Windows.Forms.Button();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.cbLight = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.bxNamedParameters = new System.Windows.Forms.CheckBox();
            this.cbAmbient = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbBackground = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbCamera = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbSampler = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.helpProvider = new System.Windows.Forms.HelpProvider();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // bnCancel
            // 
            this.bnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bnCancel.Location = new System.Drawing.Point(310, 206);
            this.bnCancel.Name = "bnCancel";
            this.bnCancel.Size = new System.Drawing.Size(75, 23);
            this.bnCancel.TabIndex = 5;
            this.bnCancel.Text = "Cancel";
            // 
            // bnOk
            // 
            this.bnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bnOk.Location = new System.Drawing.Point(218, 206);
            this.bnOk.Name = "bnOk";
            this.bnOk.Size = new System.Drawing.Size(75, 23);
            this.bnOk.TabIndex = 4;
            this.bnOk.Text = "OK";
            // 
            // groupBox
            // 
            this.groupBox.Controls.Add(this.cbLight);
            this.groupBox.Controls.Add(this.label5);
            this.groupBox.Controls.Add(this.bxNamedParameters);
            this.groupBox.Controls.Add(this.cbAmbient);
            this.groupBox.Controls.Add(this.label4);
            this.groupBox.Controls.Add(this.cbBackground);
            this.groupBox.Controls.Add(this.label3);
            this.groupBox.Controls.Add(this.cbCamera);
            this.groupBox.Controls.Add(this.label2);
            this.groupBox.Controls.Add(this.cbSampler);
            this.groupBox.Controls.Add(this.label1);
            this.groupBox.Location = new System.Drawing.Point(12, 12);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(371, 185);
            this.groupBox.TabIndex = 3;
            this.groupBox.TabStop = false;
            // 
            // cbLight
            // 
            this.cbLight.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLight.FormattingEnabled = true;
            this.cbLight.Items.AddRange(new object[] {
            "Unassigned"});
            this.cbLight.Location = new System.Drawing.Point(137, 130);
            this.cbLight.Name = "cbLight";
            this.cbLight.Size = new System.Drawing.Size(217, 21);
            this.cbLight.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(22, 133);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(103, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Select a light source";
            // 
            // bxNamedParameters
            // 
            this.bxNamedParameters.AutoSize = true;
            this.bxNamedParameters.Location = new System.Drawing.Point(137, 157);
            this.bxNamedParameters.Name = "bxNamedParameters";
            this.bxNamedParameters.Size = new System.Drawing.Size(135, 17);
            this.bxNamedParameters.TabIndex = 10;
            this.bxNamedParameters.Text = "Use named parameters";
            // 
            // cbAmbient
            // 
            this.cbAmbient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAmbient.FormattingEnabled = true;
            this.cbAmbient.Items.AddRange(new object[] {
            "Unassigned"});
            this.cbAmbient.Location = new System.Drawing.Point(137, 101);
            this.cbAmbient.Name = "cbAmbient";
            this.cbAmbient.Size = new System.Drawing.Size(217, 21);
            this.cbAmbient.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Select an ambient";
            // 
            // cbBackground
            // 
            this.cbBackground.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBackground.FormattingEnabled = true;
            this.cbBackground.Items.AddRange(new object[] {
            "Unassigned"});
            this.cbBackground.Location = new System.Drawing.Point(137, 73);
            this.cbBackground.Margin = new System.Windows.Forms.Padding(1, 3, 3, 3);
            this.cbBackground.Name = "cbBackground";
            this.cbBackground.Size = new System.Drawing.Size(217, 21);
            this.cbBackground.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 76);
            this.label3.Margin = new System.Windows.Forms.Padding(3, 3, 1, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(106, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Select a background";
            // 
            // cbCamera
            // 
            this.cbCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCamera.FormattingEnabled = true;
            this.cbCamera.Items.AddRange(new object[] {
            "Unassigned"});
            this.cbCamera.Location = new System.Drawing.Point(137, 45);
            this.cbCamera.Name = "cbCamera";
            this.cbCamera.Size = new System.Drawing.Size(217, 21);
            this.cbCamera.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Select a camera";
            // 
            // cbSampler
            // 
            this.cbSampler.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSampler.FormattingEnabled = true;
            this.cbSampler.Items.AddRange(new object[] {
            "Unassigned"});
            this.cbSampler.Location = new System.Drawing.Point(137, 19);
            this.cbSampler.Name = "cbSampler";
            this.cbSampler.Size = new System.Drawing.Size(217, 21);
            this.cbSampler.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select a sampler";
            // 
            // helpProvider
            // 
            this.helpProvider.HelpNamespace = "xsight.chm";
            // 
            // SceneWizard
            // 
            this.AcceptButton = this.bnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bnCancel;
            this.ClientSize = new System.Drawing.Size(403, 237);
            this.Controls.Add(this.bnCancel);
            this.Controls.Add(this.bnOk);
            this.Controls.Add(this.groupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.helpProvider.SetHelpKeyword(this, "scwizform.html");
            this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SceneWizard";
            this.Opacity = 0.85;
            this.helpProvider.SetShowHelp(this, true);
            this.ShowInTaskbar = false;
            this.Text = "Scene Wizard";
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bnCancel;
        private System.Windows.Forms.Button bnOk;
        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.CheckBox bxNamedParameters;
        private System.Windows.Forms.ComboBox cbAmbient;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbBackground;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbCamera;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbSampler;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbLight;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.HelpProvider helpProvider;
    }
}