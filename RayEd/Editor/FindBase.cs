using IntSight.Controls;
using System;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace RayEd
{
    public partial class FindBase : Form
    {
        protected EventHandler<FindCompleteEventArgs> findComplete;
        protected CodeEditor editor;

        protected FindBase()
        {
            InitializeComponent();
        }

        public FindBase(CodeEditor editor, EventHandler<FindCompleteEventArgs> findComplete)
        {
            InitializeComponent();
            this.editor = editor;
            string text = editor.SelectedText;
            if (!string.IsNullOrEmpty(text))
                textCombo.Text = text;
            this.findComplete = findComplete;
        }

        protected virtual void OnFindComplete(FindCompleteEventArgs e)
        {
            findComplete?.Invoke(this, e);
        }

        protected virtual void SaveValues()
        {
            var settings = Properties.FindSettings.Default;
            settings.FindText = textCombo.Text;
            settings.MatchCase = matchCase.Checked;
            settings.WholeWords = wholeWords.Checked;
            settings.LeftPosition = Left;
            settings.TopPosition = Top;
            var findStrings = new string[textCombo.Items.Count];
            textCombo.Items.CopyTo(findStrings, 0);
            settings.FindItems.Clear();
            settings.FindItems.AddRange(findStrings);
            settings.Save();
        }

        protected virtual void RestoreValues()
        {
            var settings = Properties.FindSettings.Default;
            textCombo.Text = settings.FindText;
            matchCase.Checked = settings.MatchCase;
            wholeWords.Checked = settings.WholeWords;
            int leftPos = settings.LeftPosition;
            int topPos = settings.TopPosition;
            if (leftPos >= 0 && topPos >= 0)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new(leftPos, topPos);
            }
            textCombo.Items.Clear();
            if (settings.FindItems == null)
                settings.FindItems = new StringCollection();
            else
                foreach (string value in settings.FindItems)
                    textCombo.Items.Add(value);
        }

        public static void Beep()
        {
            if (OperatingSystem.IsWindows())
            {
                using var sp = new System.Media.SoundPlayer(RayEd.Properties.Resources.notFound);
                sp.Play();
            }
        }

        private void Find_Click(object sender, EventArgs e)
        {
            string findString = textCombo.Text;
            textCombo.SelectAll();
            int pos = textCombo.Items.IndexOf(findString);
            if (pos != -1)
                textCombo.Items.RemoveAt(pos);
            if (textCombo.Items.Count == 8)
                textCombo.Items.RemoveAt(7);
            textCombo.Items.Insert(0, findString);
            bool result = editor.FindText(
                findString, editor.Current, wholeWords.Checked, matchCase.Checked);
            if (!result && editor.Current > CodeEditor.Position.Zero)
                result = editor.FindText(findString, CodeEditor.Position.Zero,
                    wholeWords.Checked, matchCase.Checked);
            OnFindComplete(result ?
                FindCompleteEventArgs.Found : FindCompleteEventArgs.NotFound);
            if (!result)
                Beep();
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    public enum FindResult { NotFound, Found, Replaced, ReplacedAll }

    public sealed class FindCompleteEventArgs : EventArgs
    {
        public static readonly FindCompleteEventArgs Found =
            new(FindResult.Found);
        public static readonly FindCompleteEventArgs NotFound =
            new(FindResult.NotFound);
        public static readonly FindCompleteEventArgs Replaced =
            new(FindResult.Replaced);

        public readonly FindResult Reason;
        public readonly int Count;

        public FindCompleteEventArgs(FindResult reason)
        {
            Reason = reason;
            if (reason != FindResult.NotFound)
                Count = 1;
        }

        public FindCompleteEventArgs(int count)
        {
            Reason = FindResult.ReplacedAll;
            Count = count;
        }
    }
}
