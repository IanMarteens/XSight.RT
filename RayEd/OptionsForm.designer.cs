namespace RayEd
{
    partial class OptionsForm
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
            this.colorFar = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.colorNear = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.colorBack = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.edDiffusion = new System.Windows.Forms.NumericUpDown();
            this.edBSpheres = new System.Windows.Forms.NumericUpDown();
            this.edUnions = new System.Windows.Forms.NumericUpDown();
            this.edLoops = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.edFontSize = new System.Windows.Forms.NumericUpDown();
            this.edLeftMargin = new System.Windows.Forms.NumericUpDown();
            this.edTabSize = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.bxSmartIndent = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cbFamilies = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tpRayTracer = new System.Windows.Forms.TabPage();
            this.tpEditor = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cbVisualStyle = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.helpProvider = new System.Windows.Forms.HelpProvider();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.edDiffusion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.edBSpheres)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.edUnions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.edLoops)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.edFontSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.edLeftMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.edTabSize)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tpRayTracer.SuspendLayout();
            this.tpEditor.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.colorFar);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.colorNear);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.colorBack);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(15, 152);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(272, 113);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sonar mode colors";
            // 
            // colorFar
            // 
            this.colorFar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.colorFar.Location = new System.Drawing.Point(127, 76);
            this.colorFar.Name = "colorFar";
            this.colorFar.Size = new System.Drawing.Size(87, 21);
            this.colorFar.TabIndex = 5;
            this.colorFar.DoubleClick += new System.EventHandler(this.ColorFar_DoubleClick);
            this.colorFar.MouseLeave += new System.EventHandler(this.ColorPanel_MouseLeave);
            this.colorFar.MouseEnter += new System.EventHandler(this.ColorPanel_MouseEnter);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(16, 76);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 21);
            this.label3.TabIndex = 4;
            this.label3.Text = "Far color";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // colorNear
            // 
            this.colorNear.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.colorNear.Location = new System.Drawing.Point(127, 49);
            this.colorNear.Name = "colorNear";
            this.colorNear.Size = new System.Drawing.Size(87, 21);
            this.colorNear.TabIndex = 3;
            this.colorNear.DoubleClick += new System.EventHandler(this.ColorNear_DoubleClick);
            this.colorNear.MouseLeave += new System.EventHandler(this.ColorPanel_MouseLeave);
            this.colorNear.MouseEnter += new System.EventHandler(this.ColorPanel_MouseEnter);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(16, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 21);
            this.label2.TabIndex = 2;
            this.label2.Text = "Near color";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // colorBack
            // 
            this.colorBack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.colorBack.Location = new System.Drawing.Point(127, 22);
            this.colorBack.Name = "colorBack";
            this.colorBack.Size = new System.Drawing.Size(87, 21);
            this.colorBack.TabIndex = 1;
            this.colorBack.DoubleClick += new System.EventHandler(this.ColorBack_DoubleClick);
            this.colorBack.MouseLeave += new System.EventHandler(this.ColorPanel_MouseLeave);
            this.colorBack.MouseEnter += new System.EventHandler(this.ColorPanel_MouseEnter);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(16, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "Background color";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // colorDialog
            // 
            this.colorDialog.FullOpen = true;
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Location = new System.Drawing.Point(157, 321);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "OK";
            this.OK.UseVisualStyleBackColor = true;
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(242, 321);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 2;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.edDiffusion);
            this.groupBox2.Controls.Add(this.edBSpheres);
            this.groupBox2.Controls.Add(this.edUnions);
            this.groupBox2.Controls.Add(this.edLoops);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Location = new System.Drawing.Point(15, 15);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(272, 131);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Scene optimizer thresholds";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(16, 97);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(96, 21);
            this.label12.TabIndex = 6;
            this.label12.Text = "Diffusion levels";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // edDiffusion
            // 
            this.edDiffusion.Location = new System.Drawing.Point(126, 98);
            this.edDiffusion.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.edDiffusion.Name = "edDiffusion";
            this.edDiffusion.Size = new System.Drawing.Size(87, 20);
            this.edDiffusion.TabIndex = 7;
            this.edDiffusion.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // edBSpheres
            // 
            this.edBSpheres.DecimalPlaces = 3;
            this.edBSpheres.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.edBSpheres.Location = new System.Drawing.Point(127, 72);
            this.edBSpheres.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.edBSpheres.Name = "edBSpheres";
            this.edBSpheres.Size = new System.Drawing.Size(87, 20);
            this.edBSpheres.TabIndex = 5;
            this.edBSpheres.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // edUnions
            // 
            this.edUnions.Location = new System.Drawing.Point(127, 46);
            this.edUnions.Name = "edUnions";
            this.edUnions.Size = new System.Drawing.Size(87, 20);
            this.edUnions.TabIndex = 3;
            this.edUnions.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // edLoops
            // 
            this.edLoops.Location = new System.Drawing.Point(127, 20);
            this.edLoops.Name = "edLoops";
            this.edLoops.Size = new System.Drawing.Size(87, 20);
            this.edLoops.TabIndex = 1;
            this.edLoops.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(16, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 21);
            this.label4.TabIndex = 4;
            this.label4.Text = "Bounding spheres";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(16, 46);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 21);
            this.label5.TabIndex = 2;
            this.label5.Text = "Union threshold";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(16, 20);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(96, 21);
            this.label6.TabIndex = 0;
            this.label6.Text = "Loop expansion";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.edFontSize);
            this.groupBox4.Controls.Add(this.edLeftMargin);
            this.groupBox4.Controls.Add(this.edTabSize);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.bxSmartIndent);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.cbFamilies);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Location = new System.Drawing.Point(15, 15);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(272, 154);
            this.groupBox4.TabIndex = 0;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Editor settings";
            // 
            // edFontSize
            // 
            this.edFontSize.DecimalPlaces = 1;
            this.edFontSize.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.edFontSize.Location = new System.Drawing.Point(127, 98);
            this.edFontSize.Maximum = new decimal(new int[] {
            96,
            0,
            0,
            0});
            this.edFontSize.Minimum = new decimal(new int[] {
            7,
            0,
            0,
            0});
            this.edFontSize.Name = "edFontSize";
            this.edFontSize.Size = new System.Drawing.Size(87, 20);
            this.edFontSize.TabIndex = 7;
            this.edFontSize.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            // 
            // edLeftMargin
            // 
            this.edLeftMargin.Location = new System.Drawing.Point(127, 46);
            this.edLeftMargin.Name = "edLeftMargin";
            this.edLeftMargin.Size = new System.Drawing.Size(87, 20);
            this.edLeftMargin.TabIndex = 3;
            this.edLeftMargin.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            // 
            // edTabSize
            // 
            this.edTabSize.Location = new System.Drawing.Point(127, 20);
            this.edTabSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.edTabSize.Name = "edTabSize";
            this.edTabSize.Size = new System.Drawing.Size(87, 20);
            this.edTabSize.TabIndex = 1;
            this.edTabSize.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(16, 98);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(96, 20);
            this.label10.TabIndex = 6;
            this.label10.Text = "Font size";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bxSmartIndent
            // 
            this.bxSmartIndent.AutoSize = true;
            this.bxSmartIndent.Location = new System.Drawing.Point(127, 127);
            this.bxSmartIndent.Name = "bxSmartIndent";
            this.bxSmartIndent.Size = new System.Drawing.Size(108, 17);
            this.bxSmartIndent.TabIndex = 8;
            this.bxSmartIndent.Text = "Smart indentation";
            this.bxSmartIndent.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(16, 72);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(96, 20);
            this.label9.TabIndex = 4;
            this.label9.Text = "Editor font";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbFamilies
            // 
            this.cbFamilies.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFamilies.FormattingEnabled = true;
            this.cbFamilies.Location = new System.Drawing.Point(127, 72);
            this.cbFamilies.Name = "cbFamilies";
            this.cbFamilies.Size = new System.Drawing.Size(134, 21);
            this.cbFamilies.TabIndex = 5;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(16, 46);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(96, 20);
            this.label8.TabIndex = 2;
            this.label8.Text = "Left margin";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(16, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(96, 20);
            this.label7.TabIndex = 0;
            this.label7.Text = "Tab size";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tpRayTracer);
            this.tabControl.Controls.Add(this.tpEditor);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(312, 302);
            this.tabControl.TabIndex = 0;
            // 
            // tpRayTracer
            // 
            this.tpRayTracer.Controls.Add(this.groupBox2);
            this.tpRayTracer.Controls.Add(this.groupBox1);
            this.tpRayTracer.Location = new System.Drawing.Point(4, 22);
            this.tpRayTracer.Name = "tpRayTracer";
            this.tpRayTracer.Padding = new System.Windows.Forms.Padding(3);
            this.tpRayTracer.Size = new System.Drawing.Size(304, 276);
            this.tpRayTracer.TabIndex = 0;
            this.tpRayTracer.Text = "Ray tracer";
            this.tpRayTracer.UseVisualStyleBackColor = true;
            // 
            // tpEditor
            // 
            this.tpEditor.Controls.Add(this.groupBox3);
            this.tpEditor.Controls.Add(this.groupBox4);
            this.tpEditor.Location = new System.Drawing.Point(4, 22);
            this.tpEditor.Name = "tpEditor";
            this.tpEditor.Padding = new System.Windows.Forms.Padding(3);
            this.tpEditor.Size = new System.Drawing.Size(304, 276);
            this.tpEditor.TabIndex = 1;
            this.tpEditor.Text = "Scene editor";
            this.tpEditor.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.cbVisualStyle);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Location = new System.Drawing.Point(15, 175);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(272, 54);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Menu and toolbars";
            // 
            // cbVisualStyle
            // 
            this.cbVisualStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbVisualStyle.FormattingEnabled = true;
            this.cbVisualStyle.Items.AddRange(new object[] {
            "Office Blue",
            "Visual Tan",
            "Silver Moon"});
            this.cbVisualStyle.Location = new System.Drawing.Point(127, 20);
            this.cbVisualStyle.Name = "cbVisualStyle";
            this.cbVisualStyle.Size = new System.Drawing.Size(134, 21);
            this.cbVisualStyle.TabIndex = 1;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(16, 20);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(96, 20);
            this.label11.TabIndex = 0;
            this.label11.Text = "Visual style";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // helpProvider
            // 
            this.helpProvider.HelpNamespace = "xsight.chm";
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(334, 356);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.helpProvider.SetHelpKeyword(this, "optsform.html");
            this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.Opacity = 0.85;
            this.helpProvider.SetShowHelp(this, true);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "XSight RT Options";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OptionsForm_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.edDiffusion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.edBSpheres)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.edUnions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.edLoops)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.edFontSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.edLeftMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.edTabSize)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tpRayTracer.ResumeLayout(false);
            this.tpEditor.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel colorBack;
        private System.Windows.Forms.Panel colorFar;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel colorNear;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown edBSpheres;
        private System.Windows.Forms.NumericUpDown edUnions;
        private System.Windows.Forms.NumericUpDown edLoops;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox bxSmartIndent;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cbFamilies;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tpRayTracer;
        private System.Windows.Forms.TabPage tpEditor;
        private System.Windows.Forms.NumericUpDown edLeftMargin;
        private System.Windows.Forms.NumericUpDown edTabSize;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox cbVisualStyle;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown edFontSize;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown edDiffusion;
        private System.Windows.Forms.HelpProvider helpProvider;
    }
}