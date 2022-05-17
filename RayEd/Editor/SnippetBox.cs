using IntSight.Controls.CodeModel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RayEd
{
    public partial class SnippetBox : Form
    {
        private string searchStr = string.Empty;
        private readonly ICodeSnippet[] snippets;
        private readonly ICodeSnippetCallback callback;
        private readonly bool isSurround;

        public static void Select(ICodeSnippet[] snippets,
            Control editor, int x, int y, bool isSurround, ICodeSnippetCallback callback)
        {
            if (snippets != null && snippets.Length > 0)
                using (var sb = new SnippetBox(snippets, editor, x, y, isSurround, callback))
                    sb.Show();
        }

        private SnippetBox() => InitializeComponent();

        private SnippetBox(ICodeSnippet[] snippets, Control editor, int x, int y,
            bool isSurround, ICodeSnippetCallback callback)
        {
            InitializeComponent();
            if (editor != null)
                Location = editor.PointToScreen(new Point(x, y));
            this.snippets = snippets;
            foreach (ICodeSnippet snippet in snippets)
                listBox.Items.Add(snippet.Name);
            listBox.SelectedIndex = 0;
            if (listBox.Height > listBox.ItemHeight * snippets.Length)
                listBox.Height = listBox.ItemHeight * (snippets.Length + 1);
            ClientSize = listBox.Size;
            this.isSurround = isSurround;
            this.callback = callback;
        }

        private void ListBox_DoubleClick(object sender, EventArgs e)
        {
            timer.Enabled = false;
            if (listBox.SelectedIndex != ListBox.NoMatches)
                callback.Expand(snippets[listBox.SelectedIndex], isSurround);
            Close();
        }

        private void SnippetBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (listBox.SelectedIndex != ListBox.NoMatches)
                        callback.Expand(snippets[listBox.SelectedIndex], isSurround);
                    break;
                case Keys.Escape:
                    break;
                default:
                    return;
            }
            timer.Enabled = false;
            Close();
            e.Handled = true;
        }

        private void SnippetBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= ' ')
                searchStr += e.KeyChar.ToString();
            else if (e.KeyChar == 8)
            {
                if (searchStr.Length > 0)
                    searchStr = searchStr.Remove(searchStr.Length - 1);
            }
            else
                return;
            if (searchStr.Length == 0)
                listBox.SelectedIndex = 0;
            else
            {
                int idx = listBox.FindString(searchStr, -1);
                if (idx != ListBox.NoMatches)
                    listBox.SelectedIndex = idx;
            }
            e.Handled = true;
            timer.Enabled = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Enabled = false;
            searchStr = string.Empty;
        }

        private void SnippetBox_Deactivate(object sender, EventArgs e) => Close();
    }
}