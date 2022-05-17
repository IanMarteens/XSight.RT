namespace RayEd
{
    partial class SceneTree
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SceneTree));
            this.treeView = new System.Windows.Forms.TreeView();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.bnOpen = new System.Windows.Forms.ToolStripButton();
            this.bnSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.miExpandAll = new System.Windows.Forms.ToolStripMenuItem();
            this.miCollapseAll = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.miExpandUnions = new System.Windows.Forms.ToolStripMenuItem();
            this.bnClose = new System.Windows.Forms.ToolStripButton();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.saveDlg = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolStrip.SuspendLayout();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView
            // 
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.treeView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.treeView.FullRowSelect = true;
            this.treeView.HideSelection = false;
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageList;
            this.treeView.Location = new System.Drawing.Point(0, 0);
            this.treeView.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.ShowNodeToolTips = true;
            this.treeView.Size = new System.Drawing.Size(323, 415);
            this.treeView.TabIndex = 0;
            this.treeView.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.TreeView_DrawNode);
            this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView_AfterSelect);
            // 
            // imageList
            // 
            this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imageList.Images.SetKeyName(0, "ObjectsMgBg.bmp");
            this.imageList.Images.SetKeyName(1, "bounds.bmp");
            this.imageList.Images.SetKeyName(2, "unbound.bmp");
            this.imageList.Images.SetKeyName(3, "NumbersMg.bmp");
            this.imageList.Images.SetKeyName(4, "Venn.bmp");
            this.imageList.Images.SetKeyName(5, "Synchro.bmp");
            this.imageList.Images.SetKeyName(6, "box.png");
            this.imageList.Images.SetKeyName(7, "sphere.png");
            this.imageList.Images.SetKeyName(8, "cylinder.png");
            this.imageList.Images.SetKeyName(9, "cylinderx.png");
            this.imageList.Images.SetKeyName(10, "Compress.bmp");
            this.imageList.Images.SetKeyName(11, "Tree.bmp");
            this.imageList.Images.SetKeyName(12, "torus.png");
            this.imageList.Images.SetKeyName(13, "cone.png");
            this.imageList.Images.SetKeyName(14, "blob.png");
            this.imageList.Images.SetKeyName(15, "StackMg.bmp");
            this.imageList.Images.SetKeyName(16, "StackXMg.bmp");
            this.imageList.Images.SetKeyName(17, "StackYMg.bmp");
            this.imageList.Images.SetKeyName(18, "StackZMg.bmp");
            this.imageList.Images.SetKeyName(19, "quart.png");
            this.imageList.Images.SetKeyName(20, "ball.png");
            this.imageList.Images.SetKeyName(21, "cap.png");
            this.imageList.Images.SetKeyName(22, "pipe.png");
            this.imageList.Images.SetKeyName(23, "ellip.png");
            this.imageList.Images.SetKeyName(24, "hyper.png");
            // 
            // toolStrip
            // 
            this.toolStrip.AutoSize = false;
            this.toolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip.GripMargin = new System.Windows.Forms.Padding(0);
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(14, 14);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bnOpen,
            this.bnSave,
            this.toolStripSeparator2,
            this.toolStripDropDownButton1,
            this.bnClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(0);
            this.toolStrip.Size = new System.Drawing.Size(323, 25);
            this.toolStrip.Stretch = true;
            this.toolStrip.TabIndex = 1;
            // 
            // bnOpen
            // 
            this.bnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnOpen.Image = ((System.Drawing.Image)(resources.GetObject("bnOpen.Image")));
            this.bnOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnOpen.Name = "bnOpen";
            this.bnOpen.Size = new System.Drawing.Size(29, 22);
            this.bnOpen.Text = "Open XML...";
            this.bnOpen.Click += new System.EventHandler(this.Open_Click);
            // 
            // bnSave
            // 
            this.bnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnSave.Image = ((System.Drawing.Image)(resources.GetObject("bnSave.Image")));
            this.bnSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnSave.Name = "bnSave";
            this.bnSave.Size = new System.Drawing.Size(29, 22);
            this.bnSave.Text = "Save XML...";
            this.bnSave.Click += new System.EventHandler(this.SaveXml_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miExpandAll,
            this.miCollapseAll,
            this.toolStripSeparator3,
            this.miExpandUnions});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(28, 22);
            this.toolStripDropDownButton1.Text = "Tree nodes";
            // 
            // miExpandAll
            // 
            this.miExpandAll.Name = "miExpandAll";
            this.miExpandAll.Size = new System.Drawing.Size(250, 26);
            this.miExpandAll.Text = "Expand all";
            this.miExpandAll.Click += new System.EventHandler(this.ExpandAll_Click);
            // 
            // miCollapseAll
            // 
            this.miCollapseAll.Name = "miCollapseAll";
            this.miCollapseAll.Size = new System.Drawing.Size(250, 26);
            this.miCollapseAll.Text = "Collapse all";
            this.miCollapseAll.Click += new System.EventHandler(this.CollapseAll_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(247, 6);
            // 
            // miExpandUnions
            // 
            this.miExpandUnions.Name = "miExpandUnions";
            this.miExpandUnions.Size = new System.Drawing.Size(250, 26);
            this.miExpandUnions.Text = "Expand top level unions";
            this.miExpandUnions.Click += new System.EventHandler(this.ExpandUnions_Click);
            // 
            // bnClose
            // 
            this.bnClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bnClose.AutoSize = false;
            this.bnClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bnClose.Image = ((System.Drawing.Image)(resources.GetObject("bnClose.Image")));
            this.bnClose.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.bnClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bnClose.Margin = new System.Windows.Forms.Padding(0);
            this.bnClose.Name = "bnClose";
            this.bnClose.Size = new System.Drawing.Size(20, 19);
            this.bnClose.Text = "Close";
            this.bnClose.ToolTipText = "Close";
            this.bnClose.Click += new System.EventHandler(this.Close_Click);
            // 
            // toolStripContainer1
            // 
            // 
            // 
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip);
            this.toolStripContainer1.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.BottomToolStripPanel.Name = "";
            this.toolStripContainer1.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripContainer1.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.toolStripContainer1.BottomToolStripPanel.Size = new System.Drawing.Size(323, 34);
            // 
            // 
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.treeView);
            this.toolStripContainer1.ContentPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(323, 415);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            // 
            // 
            // 
            this.toolStripContainer1.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.LeftToolStripPanel.Name = "";
            this.toolStripContainer1.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripContainer1.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            // 
            // 
            // 
            this.toolStripContainer1.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.RightToolStripPanel.Name = "";
            this.toolStripContainer1.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripContainer1.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.toolStripContainer1.Size = new System.Drawing.Size(323, 415);
            this.toolStripContainer1.TabIndex = 2;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // 
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip);
            this.toolStripContainer1.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.TopToolStripPanel.Name = "";
            this.toolStripContainer1.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripContainer1.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.toolStripContainer1.TopToolStripPanel.Size = new System.Drawing.Size(323, 38);
            // 
            // statusStrip
            // 
            this.statusStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 0);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(323, 22);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 0;
            // 
            // statusLabel
            // 
            this.statusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.statusLabel.Size = new System.Drawing.Size(308, 16);
            this.statusLabel.Spring = true;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // saveDlg
            // 
            this.saveDlg.DefaultExt = "xml";
            this.saveDlg.Filter = "XML files|*.xml|All files|*.*";
            this.saveDlg.Title = "Save XML";
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "xml";
            this.openFileDialog.Filter = "XML files|*.xml|All files|*.*";
            this.openFileDialog.Title = "Open XML file";
            // 
            // SceneTree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStripContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SceneTree";
            this.Size = new System.Drawing.Size(323, 415);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ToolStripButton bnClose;
        private System.Windows.Forms.SaveFileDialog saveDlg;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem miExpandAll;
        private System.Windows.Forms.ToolStripMenuItem miCollapseAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem miExpandUnions;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ToolStripButton bnOpen;
        private System.Windows.Forms.ToolStripButton bnSave;
    }
}
