namespace IntSight.Controls
{
    partial class PrintPreview
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintPreview));
            this.toolStripPanel1 = new System.Windows.Forms.ToolStripPanel();
            this.toolBar = new System.Windows.Forms.ToolStrip();
            this.bnPrint = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bnLast = new System.Windows.Forms.ToolStripButton();
            this.bnNext = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.bnPage = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.bnPrevious = new System.Windows.Forms.ToolStripButton();
            this.bnFirst = new System.Windows.Forms.ToolStripButton();
            this.btAutoZoom = new System.Windows.Forms.ToolStripButton();
            this.bnZoom100 = new System.Windows.Forms.ToolStripButton();
            this.printPreviewControl = new System.Windows.Forms.PrintPreviewControl();
            this.toolStripPanel1.SuspendLayout();
            this.toolBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripPanel1
            // 
            this.toolStripPanel1.Controls.Add(this.toolBar);
            this.toolStripPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolStripPanel1.Location = new System.Drawing.Point(0, 0);
            this.toolStripPanel1.Name = "toolStripPanel1";
            this.toolStripPanel1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripPanel1.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.toolStripPanel1.Size = new System.Drawing.Size(465, 25);
            // 
            // toolBar
            // 
            this.toolBar.Dock = System.Windows.Forms.DockStyle.None;
            this.toolBar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bnPrint,
            this.toolStripSeparator1,
            this.bnLast,
            this.bnNext,
            this.toolStripSeparator2,
            this.bnPage,
            this.toolStripSeparator3,
            this.bnPrevious,
            this.bnFirst,
            this.btAutoZoom,
            this.bnZoom100});
            this.toolBar.Location = new System.Drawing.Point(0, 0);
            this.toolBar.Name = "toolBar";
            this.toolBar.Padding = new System.Windows.Forms.Padding(4, 0, 1, 0);
            this.toolBar.Size = new System.Drawing.Size(465, 25);
            this.toolBar.Stretch = true;
            this.toolBar.TabIndex = 0;
            // 
            // bnPrint
            // 
            this.bnPrint.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnPrint.Image = global::IntSight.Controls.Properties.Resources.print;
            this.bnPrint.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnPrint.Name = "bnPrint";
            this.bnPrint.Size = new System.Drawing.Size(23, 22);
            this.bnPrint.Text = "Print";
            this.bnPrint.Click += new System.EventHandler(this.Print_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // bnLast
            // 
            this.bnLast.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bnLast.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnLast.Image = global::IntSight.Controls.Properties.Resources.last;
            this.bnLast.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnLast.Name = "bnLast";
            this.bnLast.Size = new System.Drawing.Size(23, 22);
            this.bnLast.Text = "Last page";
            this.bnLast.Click += new System.EventHandler(this.LastPage);
            // 
            // bnNext
            // 
            this.bnNext.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bnNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnNext.Image = global::IntSight.Controls.Properties.Resources.next;
            this.bnNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnNext.Name = "bnNext";
            this.bnNext.Size = new System.Drawing.Size(23, 22);
            this.bnNext.Text = "Next page";
            this.bnNext.Click += new System.EventHandler(this.NextPage);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // bnPage
            // 
            this.bnPage.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bnPage.AutoSize = false;
            this.bnPage.Name = "bnPage";
            this.bnPage.Size = new System.Drawing.Size(80, 22);
            this.bnPage.Text = "Page 1";
            this.bnPage.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // bnPrevious
            // 
            this.bnPrevious.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bnPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnPrevious.Enabled = false;
            this.bnPrevious.Image = global::IntSight.Controls.Properties.Resources.previous;
            this.bnPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnPrevious.Name = "bnPrevious";
            this.bnPrevious.Size = new System.Drawing.Size(23, 22);
            this.bnPrevious.Text = "Previous page";
            this.bnPrevious.Click += new System.EventHandler(this.PreviousPage);
            // 
            // bnFirst
            // 
            this.bnFirst.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bnFirst.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnFirst.Enabled = false;
            this.bnFirst.Image = global::IntSight.Controls.Properties.Resources.first;
            this.bnFirst.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnFirst.Name = "bnFirst";
            this.bnFirst.Size = new System.Drawing.Size(23, 22);
            this.bnFirst.Text = "First page";
            this.bnFirst.Click += new System.EventHandler(this.FirstPage);
            // 
            // btAutoZoom
            // 
            this.btAutoZoom.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btAutoZoom.Image = global::IntSight.Controls.Properties.Resources.fit;
            this.btAutoZoom.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btAutoZoom.Name = "btAutoZoom";
            this.btAutoZoom.Size = new System.Drawing.Size(23, 22);
            this.btAutoZoom.Text = "Zoom to fit";
            this.btAutoZoom.Click += new System.EventHandler(this.AutoZoom);
            // 
            // bnZoom100
            // 
            this.bnZoom100.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnZoom100.Image = global::IntSight.Controls.Properties.Resources.zoom100;
            this.bnZoom100.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnZoom100.Name = "bnZoom100";
            this.bnZoom100.Size = new System.Drawing.Size(23, 22);
            this.bnZoom100.Text = "Zoom to 100%";
            this.bnZoom100.Click += new System.EventHandler(this.Zoom100);
            // 
            // printPreviewControl
            // 
            this.printPreviewControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.printPreviewControl.Location = new System.Drawing.Point(0, 25);
            this.printPreviewControl.Name = "printPreviewControl";
            this.printPreviewControl.Size = new System.Drawing.Size(465, 241);
            this.printPreviewControl.TabIndex = 1;
            this.printPreviewControl.UseAntiAlias = true;
            this.printPreviewControl.StartPageChanged += new System.EventHandler(this.PrintPreviewControl_StartPageChanged);
            // 
            // PrintPreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(465, 266);
            this.Controls.Add(this.printPreviewControl);
            this.Controls.Add(this.toolStripPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintPreview";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Print Preview";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PrintPreview_FormClosing);
            this.toolStripPanel1.ResumeLayout(false);
            this.toolStripPanel1.PerformLayout();
            this.toolBar.ResumeLayout(false);
            this.toolBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStripPanel toolStripPanel1;
        private System.Windows.Forms.ToolStrip toolBar;
        private System.Windows.Forms.ToolStripButton bnPrint;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton bnFirst;
        private System.Windows.Forms.PrintPreviewControl printPreviewControl;
        private System.Windows.Forms.ToolStripButton bnLast;
        private System.Windows.Forms.ToolStripButton bnNext;
        private System.Windows.Forms.ToolStripButton bnPrevious;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripLabel bnPage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton btAutoZoom;
        private System.Windows.Forms.ToolStripButton bnZoom100;
    }
}