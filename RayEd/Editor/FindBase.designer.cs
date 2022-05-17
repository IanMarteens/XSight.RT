namespace RayEd
{
    partial class FindBase
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
            this.wholeWords = new System.Windows.Forms.CheckBox();
            this.matchCase = new System.Windows.Forms.CheckBox();
            this.textCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bnFind = new System.Windows.Forms.Button();
            this.bnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // wholeWords
            // 
            this.wholeWords.AutoSize = true;
            this.wholeWords.Location = new System.Drawing.Point(80, 64);
            this.wholeWords.Name = "wholeWords";
            this.wholeWords.Size = new System.Drawing.Size(88, 17);
            this.wholeWords.TabIndex = 3;
            this.wholeWords.Text = "Whole words";
            // 
            // matchCase
            // 
            this.matchCase.AutoSize = true;
            this.matchCase.Location = new System.Drawing.Point(80, 41);
            this.matchCase.Name = "matchCase";
            this.matchCase.Size = new System.Drawing.Size(82, 17);
            this.matchCase.TabIndex = 2;
            this.matchCase.Text = "Match case";
            // 
            // textCombo
            // 
            this.textCombo.FormattingEnabled = true;
            this.textCombo.Location = new System.Drawing.Point(80, 14);
            this.textCombo.Name = "textCombo";
            this.textCombo.Size = new System.Drawing.Size(268, 21);
            this.textCombo.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Find what";
            // 
            // bnFind
            // 
            this.bnFind.Location = new System.Drawing.Point(360, 14);
            this.bnFind.Name = "bnFind";
            this.bnFind.Size = new System.Drawing.Size(75, 23);
            this.bnFind.TabIndex = 4;
            this.bnFind.Text = "Find next";
            this.bnFind.Click += new System.EventHandler(this.Find_Click);
            // 
            // bnClose
            // 
            this.bnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bnClose.Location = new System.Drawing.Point(360, 42);
            this.bnClose.Name = "bnClose";
            this.bnClose.Size = new System.Drawing.Size(75, 23);
            this.bnClose.TabIndex = 5;
            this.bnClose.Text = "Close";
            this.bnClose.Click += new System.EventHandler(this.Close_Click);
            // 
            // FindBase
            // 
            this.AcceptButton = this.bnFind;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bnClose;
            this.ClientSize = new System.Drawing.Size(454, 142);
            this.Controls.Add(this.bnClose);
            this.Controls.Add(this.bnFind);
            this.Controls.Add(this.wholeWords);
            this.Controls.Add(this.matchCase);
            this.Controls.Add(this.textCombo);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.KeyPreview = true;
            this.Name = "FindBase";
            this.Opacity = 0.85;
            this.ShowInTaskbar = false;
            this.Text = "FindBase";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        protected System.Windows.Forms.CheckBox wholeWords;
        protected System.Windows.Forms.CheckBox matchCase;
        protected System.Windows.Forms.ComboBox textCombo;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.Button bnFind;
        protected System.Windows.Forms.Button bnClose;



    }
}