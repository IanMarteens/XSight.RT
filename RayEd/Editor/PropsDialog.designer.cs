namespace RayEd
{
    partial class PropsDialog
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
            this.txFileName = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txDiskSize = new System.Windows.Forms.Label();
            this.txMemorySize = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txLines = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txPath = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txLastAccess = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txModified = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txCreated = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.OK = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txFileName
            // 
            this.txFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txFileName.AutoEllipsis = true;
            this.txFileName.Location = new System.Drawing.Point(52, 18);
            this.txFileName.Name = "txFileName";
            this.txFileName.Size = new System.Drawing.Size(300, 13);
            this.txFileName.TabIndex = 0;
            this.txFileName.Text = "label1";
            this.txFileName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::RayEd.Properties.Resources.properties;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.txDiskSize);
            this.groupBox1.Controls.Add(this.txMemorySize);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.txLines);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txPath);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 42);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(340, 98);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            // 
            // txDiskSize
            // 
            this.txDiskSize.Location = new System.Drawing.Point(87, 76);
            this.txDiskSize.Name = "txDiskSize";
            this.txDiskSize.Size = new System.Drawing.Size(222, 13);
            this.txDiskSize.TabIndex = 6;
            this.txDiskSize.Text = "label7";
            this.txDiskSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txMemorySize
            // 
            this.txMemorySize.Location = new System.Drawing.Point(87, 56);
            this.txMemorySize.Name = "txMemorySize";
            this.txMemorySize.Size = new System.Drawing.Size(222, 13);
            this.txMemorySize.TabIndex = 5;
            this.txMemorySize.Text = "label6";
            this.txMemorySize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 56);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(30, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Size:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txLines
            // 
            this.txLines.Location = new System.Drawing.Point(87, 36);
            this.txLines.Name = "txLines";
            this.txLines.Size = new System.Drawing.Size(222, 13);
            this.txLines.TabIndex = 3;
            this.txLines.Text = "label4";
            this.txLines.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 36);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Lines:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txPath
            // 
            this.txPath.AutoEllipsis = true;
            this.txPath.Location = new System.Drawing.Point(87, 16);
            this.txPath.Name = "txPath";
            this.txPath.Size = new System.Drawing.Size(235, 13);
            this.txPath.TabIndex = 1;
            this.txPath.Text = "label2";
            this.txPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txPath.Paint += new System.Windows.Forms.PaintEventHandler(this.Path_Paint);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Location:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.txLastAccess);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txModified);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.txCreated);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Location = new System.Drawing.Point(12, 146);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(340, 80);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            // 
            // txLastAccess
            // 
            this.txLastAccess.Location = new System.Drawing.Point(87, 56);
            this.txLastAccess.Name = "txLastAccess";
            this.txLastAccess.Size = new System.Drawing.Size(235, 13);
            this.txLastAccess.TabIndex = 11;
            this.txLastAccess.Text = "label6";
            this.txLastAccess.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Last access:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txModified
            // 
            this.txModified.Location = new System.Drawing.Point(87, 36);
            this.txModified.Name = "txModified";
            this.txModified.Size = new System.Drawing.Size(235, 13);
            this.txModified.TabIndex = 9;
            this.txModified.Text = "label4";
            this.txModified.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(18, 36);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(50, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "Modified:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txCreated
            // 
            this.txCreated.AutoEllipsis = true;
            this.txCreated.Location = new System.Drawing.Point(87, 16);
            this.txCreated.Name = "txCreated";
            this.txCreated.Size = new System.Drawing.Size(235, 13);
            this.txCreated.TabIndex = 7;
            this.txCreated.Text = "label2";
            this.txCreated.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(18, 16);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(47, 13);
            this.label9.TabIndex = 6;
            this.label9.Text = "Created:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // OK
            // 
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Location = new System.Drawing.Point(145, 235);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 4;
            this.OK.Text = "OK";
            // 
            // toolTip
            // 
            this.toolTip.IsBalloon = true;
            // 
            // PropsDialog
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.OK;
            this.ClientSize = new System.Drawing.Size(365, 270);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.txFileName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PropsDialog";
            this.Opacity = 0.85;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Properties";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label txFileName;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label txDiskSize;
        private System.Windows.Forms.Label txMemorySize;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label txLines;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label txPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label txLastAccess;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label txModified;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label txCreated;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.ToolTip toolTip;
    }
}