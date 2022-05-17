namespace RayEd
{
    partial class ReplaceDialog
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
            this.label2 = new System.Windows.Forms.Label();
            this.replaceCombo = new System.Windows.Forms.ComboBox();
            this.bnReplace = new System.Windows.Forms.Button();
            this.bnReplaceAll = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // wholeWords
            // 
            this.wholeWords.Location = new System.Drawing.Point(80, 92);
            this.wholeWords.TabIndex = 5;
            // 
            // matchCase
            // 
            this.matchCase.Location = new System.Drawing.Point(80, 69);
            this.matchCase.TabIndex = 4;
            // 
            // bnFind
            // 
            this.bnFind.TabIndex = 6;
            // 
            // bnClose
            // 
            this.bnClose.Location = new System.Drawing.Point(360, 98);
            this.bnClose.TabIndex = 9;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Replace with";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // replaceCombo
            // 
            this.replaceCombo.FormattingEnabled = true;
            this.replaceCombo.Location = new System.Drawing.Point(80, 42);
            this.replaceCombo.Name = "replaceCombo";
            this.replaceCombo.Size = new System.Drawing.Size(268, 21);
            this.replaceCombo.TabIndex = 3;
            // 
            // bnReplace
            // 
            this.bnReplace.Location = new System.Drawing.Point(360, 42);
            this.bnReplace.Name = "bnReplace";
            this.bnReplace.Size = new System.Drawing.Size(75, 23);
            this.bnReplace.TabIndex = 7;
            this.bnReplace.Text = "&Replace";
            this.bnReplace.Click += new System.EventHandler(this.Replace_Click);
            // 
            // bnReplaceAll
            // 
            this.bnReplaceAll.Location = new System.Drawing.Point(360, 70);
            this.bnReplaceAll.Name = "bnReplaceAll";
            this.bnReplaceAll.Size = new System.Drawing.Size(75, 23);
            this.bnReplaceAll.TabIndex = 8;
            this.bnReplaceAll.Text = "Replace &all";
            this.bnReplaceAll.Click += new System.EventHandler(this.ReplaceAll_Click);
            // 
            // ReplaceDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(454, 136);
            this.Controls.Add(this.bnReplaceAll);
            this.Controls.Add(this.replaceCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.bnReplace);
            this.Name = "ReplaceDialog";
            this.Text = "Replace";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ReplaceForm_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ReplaceForm_KeyDown);
            this.Controls.SetChildIndex(this.bnReplace, 0);
            this.Controls.SetChildIndex(this.bnClose, 0);
            this.Controls.SetChildIndex(this.bnFind, 0);
            this.Controls.SetChildIndex(this.label2, 0);
            this.Controls.SetChildIndex(this.replaceCombo, 0);
            this.Controls.SetChildIndex(this.bnReplaceAll, 0);
            this.Controls.SetChildIndex(this.matchCase, 0);
            this.Controls.SetChildIndex(this.wholeWords, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.textCombo, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox replaceCombo;
        private System.Windows.Forms.Button bnReplace;
        private System.Windows.Forms.Button bnReplaceAll;
    }
}