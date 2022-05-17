using System;
using System.Windows.Forms;
using Rsc = IntSight.Controls.Properties.Resources;

namespace IntSight.Controls
{
    internal partial class GotoLineDialog : Form
    {
        private int maxLines;

        public static bool Execute(IntSight.Controls.CodeEditor editor)
        {
            using GotoLineDialog form = new GotoLineDialog
            {
                maxLines = editor.LineCount
            };
            form.label.Text = string.Format(form.label.Text, form.maxLines);
            form.textBox.Text = editor.CurrentLine.ToString();
            if (form.ShowDialog(editor.FindForm()) == DialogResult.OK)
            {
                editor.CurrentLine = int.Parse(form.textBox.Text);
                return true;
            }
            return false;
        }

        private GotoLineDialog()
        {
            InitializeComponent();
        }

        private void ShowError(string message) =>
            toolTip.Show(message, textBox, textBox.Width, textBox.Height, 3000);

        private void GotoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (!int.TryParse(textBox.Text, out int value))
                {
                    ShowError(Rsc.GotoLineLineNotInteger);
                    e.Cancel = true;
                }
                else if (value < 1)
                {
                    ShowError(Rsc.GotoLineLineLesserThanOne);
                    e.Cancel = true;
                }
                else if (value > maxLines)
                {
                    ShowError(string.Format(Rsc.GotoLineLineTooHigh, maxLines));
                    e.Cancel = true;
                }
                if (e.Cancel)
                    textBox.Focus();
            }
        }

        private void TextBox_Click(object sender, EventArgs e)
        {
            toolTip.Hide(textBox);
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            toolTip.Hide(textBox);
            if (e.KeyChar >= 32)
                if (e.KeyChar < '0' || e.KeyChar > '9')
                {
                    ShowError(Rsc.GotoLineEnterValidLine);
                    e.Handled = true;
                }
        }

        private void GotoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            toolTip.Hide(textBox);
        }
    }
}