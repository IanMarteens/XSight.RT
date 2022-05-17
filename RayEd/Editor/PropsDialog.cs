using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Rsc = RayEd.Properties.Resources;

namespace RayEd
{
    public partial class PropsDialog : Form
    {
        public static void Execute(string fileName, IntSight.Controls.CodeEditor editor)
        {
            using PropsDialog form = new PropsDialog();
            form.Init(fileName, editor);
            form.ShowDialog();
        }

        private void Init(string fileName, IntSight.Controls.CodeEditor editor)
        {
            txDiskSize.Text = string.Empty;
            txCreated.Text = string.Empty;
            txModified.Text = string.Empty;
            txLastAccess.Text = string.Empty;
            if (string.IsNullOrEmpty(fileName))
            {
                txFileName.Text = Rsc.UntitledScene;
                FileName = Rsc.SceneNotSaved;
            }
            else
            {
                txFileName.Text = Path.GetFileName(fileName);
                FileName = fileName;
                FileInfo info = new FileInfo(fileName);
                if (info.Exists)
                {
                    txDiskSize.Text =
                        GetSizeString("byte", "bytes", info.Length) + " (disk)";
                    txCreated.Text = info.CreationTime.ToLongDateString() +
                        ", " + info.CreationTime.ToLongTimeString();
                    txModified.Text = info.LastWriteTime.ToLongDateString() +
                        ", " + info.LastWriteTime.ToLongTimeString();
                    txLastAccess.Text = info.LastAccessTime.ToLongDateString() +
                        ", " + info.LastAccessTime.ToLongTimeString();
                }
                toolTip.SetToolTip(txPath, fileName);
            }
            txPath.Text = string.Empty;
            int documentSize = editor.GetDocumentSize();
            int undoSize = editor.GetUndoStackSize();
            if (documentSize <= 2)
            {
                txLines.Text = Rsc.SceneEmpty;
                txMemorySize.Text = Rsc.SceneEmpty;
                if (undoSize > 0)
                    toolTip.SetToolTip(txMemorySize, "Estimated undo/redo stack size: " +
                        GetSizeString("byte", "bytes", undoSize));
            }
            else
            {
                txLines.Text = GetSizeString("line", "lines", editor.LineCount);
                txMemorySize.Text =
                    GetSizeString("byte", "bytes", documentSize) + " (editor)";
                toolTip.SetToolTip(txMemorySize, "Estimated undo/redo stack size: " +
                    GetSizeString("byte", "bytes", undoSize));
            }
        }

        private static string GetSizeString(string singular, string plural, long size)
        {
            return size.ToString("#,0") + " " + (size == 1 ? singular : plural);
        }

        private string FileName;

        private PropsDialog()
        {
            InitializeComponent();
        }

        private void Path_Paint(object sender, PaintEventArgs e)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                StringFormat fmt = new StringFormat(StringFormat.GenericTypographic)
                {
                    Trimming = StringTrimming.EllipsisPath,
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near
                };
                e.Graphics.DrawString(FileName,
                    txPath.Font, Brushes.Black, txPath.ClientRectangle, fmt);
            }
        }
    }
}