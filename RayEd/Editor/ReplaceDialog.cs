using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using IntSight.Controls;

namespace RayEd
{
    public partial class ReplaceDialog : FindBase
    {
        private static ReplaceDialog instance;

        public static void Show(CodeEditor editor, EventHandler<FindCompleteEventArgs> replaceComplete)
        {
            if (instance == null)
            {
                FindDialog.Shutdown();
                instance = new(editor, replaceComplete);
                instance.Show(editor.FindForm());
            }
        }

        public static void Shutdown()
        {
            instance?.Close();
        }

        protected ReplaceDialog()
        {
            InitializeComponent();
        }

        private ReplaceDialog(CodeEditor editor, EventHandler<FindCompleteEventArgs> findComplete)
            : base(editor, findComplete)
        {
            InitializeComponent();
            RestoreValues();
        }

        private void ReplaceForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveValues();
            instance = null;
        }

        protected override void SaveValues()
        {
            base.SaveValues();
            var settings = Properties.FindSettings.Default;
            settings.ReplaceText = replaceCombo.Text;
            var findStrings = new string[replaceCombo.Items.Count];
            replaceCombo.Items.CopyTo(findStrings, 0);
            settings.ReplaceItems.Clear();
            settings.ReplaceItems.AddRange(findStrings);
            settings.Save();
        }

        protected override void RestoreValues()
        {
            base.RestoreValues();
            var settings = Properties.FindSettings.Default;
            replaceCombo.Text = settings.ReplaceText;
            replaceCombo.Items.Clear();
            if (settings.ReplaceItems == null)
                settings.ReplaceItems = new StringCollection();
            else
                foreach (string value in settings.ReplaceItems)
                    replaceCombo.Items.Add(value);
        }

        private void ReplaceForm_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt)) == Keys.Control &&
                e.KeyCode == Keys.F)
            {
                FindDialog.Show(editor, findComplete);
                e.Handled = true;
            }
        }

        private void Replace_Click(object sender, EventArgs e)
        {
            if (string.Compare(textCombo.Text, editor.SelectedText, !matchCase.Checked) == 0)
            {
                editor.SelectedText = replaceCombo.Text;
                OnFindComplete(FindCompleteEventArgs.Replaced);
                editor.FindText(textCombo.Text,
                    editor.Current, wholeWords.Checked, matchCase.Checked);
            }
            else
            {
                bool result = editor.FindText(textCombo.Text,
                    editor.Current, wholeWords.Checked, matchCase.Checked);
                OnFindComplete(result ?
                    FindCompleteEventArgs.Found : FindCompleteEventArgs.NotFound);
                if (!result)
                    Beep();
            }
        }

        private void ReplaceAll_Click(object sender, EventArgs e)
        {
            CodeEditor.Position start = CodeEditor.Position.Zero;
            int count = 0;
            while (editor.FindText(
                textCombo.Text, start, wholeWords.Checked, matchCase.Checked))
            {
                start = editor.Current;
                editor.SelectedText = replaceCombo.Text;
                start.Column += replaceCombo.Text.Length - textCombo.Text.Length;
                count++;
            }
            if (count == 0)
            {
                OnFindComplete(FindCompleteEventArgs.NotFound);
                Beep();
            }
            else
                OnFindComplete(new FindCompleteEventArgs(count));
        }
    }
}