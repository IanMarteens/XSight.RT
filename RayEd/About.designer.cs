namespace RayEd
{
    partial class About
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            this.txClose = new System.Windows.Forms.LinkLabel();
            this.txVersion = new System.Windows.Forms.Label();
            this.txFramework = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txClose
            // 
            this.txClose.ActiveLinkColor = System.Drawing.Color.MediumBlue;
            this.txClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txClose.AutoSize = true;
            this.txClose.BackColor = System.Drawing.Color.Transparent;
            this.txClose.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.txClose.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.txClose.LinkColor = System.Drawing.Color.MediumBlue;
            this.txClose.Location = new System.Drawing.Point(393, 159);
            this.txClose.Name = "txClose";
            this.txClose.Size = new System.Drawing.Size(56, 21);
            this.txClose.TabIndex = 0;
            this.txClose.TabStop = true;
            this.txClose.Text = "Close";
            this.txClose.VisitedLinkColor = System.Drawing.Color.Gold;
            this.txClose.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Close_LinkClicked);
            // 
            // txVersion
            // 
            this.txVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txVersion.BackColor = System.Drawing.Color.Transparent;
            this.txVersion.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.txVersion.ForeColor = System.Drawing.Color.White;
            this.txVersion.Location = new System.Drawing.Point(277, 86);
            this.txVersion.Name = "txVersion";
            this.txVersion.Size = new System.Drawing.Size(172, 23);
            this.txVersion.TabIndex = 1;
            this.txVersion.Text = "Version 1.0.0";
            this.txVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txFramework
            // 
            this.txFramework.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txFramework.BackColor = System.Drawing.Color.Transparent;
            this.txFramework.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.txFramework.ForeColor = System.Drawing.Color.White;
            this.txFramework.Location = new System.Drawing.Point(277, 109);
            this.txFramework.Name = "txFramework";
            this.txFramework.Size = new System.Drawing.Size(172, 23);
            this.txFramework.TabIndex = 2;
            this.txFramework.Text = "Version 1.0.0";
            this.txFramework.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // About
            // 
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(461, 189);
            this.Controls.Add(this.txFramework);
            this.Controls.Add(this.txVersion);
            this.Controls.Add(this.txClose);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "About";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About...";
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.About_KeyPress);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel txClose;
        private System.Windows.Forms.Label txVersion;
        private System.Windows.Forms.Label txFramework;
    }
}