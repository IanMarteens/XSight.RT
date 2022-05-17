using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IntSight.Controls
{
    public partial class PrintPreview : Form
    {
        private int maxPage;
        private int pageCount;

        public PrintPreview()
        {
            InitializeComponent();
            printPreviewControl.Zoom = 1.0;
            maxPage = -1;
            printPreviewControl.MouseWheel += PrintPreviewControl_MouseWheel;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void PrintPreviewControl_MouseWheel(object sender, MouseEventArgs e)
        {
            int scrollLines = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            IntPtr wParam;
            if (scrollLines < 0)
            {
                wParam = (IntPtr)1;
                scrollLines = -scrollLines;
            }
            else
                wParam = IntPtr.Zero;
            HandleRef href = new HandleRef(printPreviewControl, printPreviewControl.Handle);
            for (int i = 0; i++ < scrollLines; )
                SendMessage(href, 0x0115, wParam, IntPtr.Zero);
        }

        public static void Execute(PrintDocument document, IWin32Window mainWindow)
        {
            using PrintPreview form = new PrintPreview
            {
                Location = new Point(0, 0),
                Size = Screen.FromHandle(mainWindow.Handle).WorkingArea.Size
            };
            form.printPreviewControl.Document = document;
            form.PerformLayout();
            document.PrintPage += form.WatchPrintPage;
            document.EndPrint += form.WatchEndPrint;
            try
            {
                form.ShowDialog(mainWindow);
            }
            finally
            {
                document.EndPrint -= form.WatchEndPrint;
                document.PrintPage -= form.WatchPrintPage;
            }
        }

        private void WatchPrintPage(object sender, PrintPageEventArgs e)
        {
            pageCount++;
        }

        private void WatchEndPrint(object sender, PrintEventArgs e)
        {
            maxPage = pageCount - 1;
            UpdateButtons();
            PrintPreviewControl_StartPageChanged(null, EventArgs.Empty);
        }

        private void PrintPreview_FormClosing(object sender, FormClosingEventArgs e)
        {
            printPreviewControl.InvalidatePreview();
        }

        private void Print_Click(object sender, EventArgs e)
        {
            printPreviewControl.Document.Print();
        }

        private void UpdateButtons()
        {
            bnFirst.Enabled = bnPrevious.Enabled = printPreviewControl.StartPage > 0;
            if (maxPage >= 0)
                bnNext.Enabled = bnLast.Enabled = printPreviewControl.StartPage < maxPage;
        }

        private void NextPage(object sender, EventArgs e)
        {
            int pageNumber = printPreviewControl.StartPage + 1;
            printPreviewControl.StartPage = pageNumber;
            if (maxPage < 0)
                if (printPreviewControl.StartPage < pageNumber)
                    maxPage = printPreviewControl.StartPage;
            UpdateButtons();
        }

        private void LastPage(object sender, EventArgs e)
        {
            if (maxPage >= 0)
                printPreviewControl.StartPage = maxPage;
            else
            {
                printPreviewControl.StartPage = int.MaxValue;
                maxPage = printPreviewControl.StartPage;
            }
            UpdateButtons();
        }

        private void FirstPage(object sender, EventArgs e)
        {
            printPreviewControl.StartPage = 0;
            UpdateButtons();
        }

        private void PreviousPage(object sender, EventArgs e)
        {
            printPreviewControl.StartPage--;
            UpdateButtons();
        }

        private void PrintPreviewControl_StartPageChanged(object sender, EventArgs e)
        {
            bnPage.Text = maxPage < 0
                ? string.Format("Page {0}",
                    printPreviewControl.StartPage + 1)
                : string.Format("Page {0}/{1}",
                    printPreviewControl.StartPage + 1, maxPage + 1);
        }

        private void AutoZoom(object sender, EventArgs e)
        {
            printPreviewControl.AutoZoom = true;
        }

        private void Zoom100(object sender, EventArgs e)
        {
            printPreviewControl.Zoom = 1.0;
        }
    }
}