using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IntSight.Controls
{
    public partial class CodeEditor
    {
        #region The scroll handling subsystem.

        protected const ushort SB_LINEUP = 0;
        protected const ushort SB_LINEDOWN = 1;
        protected const ushort SB_PAGEUP = 2;
        protected const ushort SB_PAGEDOWN = 3;
        protected const ushort SB_THUMBPOSITION = 4;
        protected const ushort SB_THUMBTRACK = 5;
        protected const ushort SB_TOP = 6;
        protected const ushort SB_BOTTOM = 7;

        /// <summary>Handles a message from the vertical scrollbar.</summary>
        /// <param name="m">Message information.</param>
        private void WmVScroll(ref Message m)
        {
            using (model.WrapOperation())
                switch ((ushort)m.WParam)
                {
                    case SB_LINEUP:
                        model.ScrollTo(ref topLine, topLine - 1, linesInPage);
                        break;
                    case SB_LINEDOWN:
                        model.ScrollTo(ref topLine, topLine + 1, linesInPage);
                        break;
                    case SB_PAGEUP:
                        model.MovePageUp(ref topLine, linesInPage, false);
                        break;
                    case SB_PAGEDOWN:
                        model.MovePageDown(ref topLine, linesInPage, false);
                        break;
                    case SB_THUMBPOSITION:
                        // The user has released the scrollbar's thumb.
                        model.ScrollTo(
                            ref topLine, (int)(((uint)m.WParam) >> 16), linesInPage);
                        // Hide the tooltip message window.
                        if (toolTip != null)
                            toolTip.Hide(this);
                        break;
                    case SB_THUMBTRACK:
                        // The user holds and moves the scrollbar's thumb.
                        model.ScrollTo(
                            ref topLine, (int)(((uint)m.WParam) >> 16), linesInPage);
                        // Show a tooltip message window containing the current line number.
                        if (toolTip != null)
                        {
                            Point pt = PointToClient(MousePosition);
                            pt.X = Width - 100;
                            toolTip.Show(string.Format(
                                Properties.Resources.ScrollBarLineFormat,
                                Current.line + 1), this, pt);
                        }
                        break;
                    case SB_TOP:
                        model.MoveTo(Position.Zero, false);
                        break;
                    case SB_BOTTOM:
                        model.MoveTo(Position.Zero.ChangeLine(int.MaxValue), false);
                        break;
                }
        }

        private void ChangeLeftColumn(int newValue)
        {
            if (newValue < 0)
                newValue = 0;
            else
            {
                int max = model.RangeWidth(topLine, topLine + linesInPage - 1) - 1;
                if (newValue > max)
                    newValue = max;
            }
            if (leftColumn != newValue)
            {
                leftColumn = newValue;
                HorizontalScrollChanged();
                view.Redraw();
            }
        }

        /// <summary>Handles a message from the horizontal scrollbar.</summary>
        /// <param name="m">Information about the Windows message.</param>
        private void WmHScroll(ref Message m)
        {
            using (model.WrapOperation())
                switch ((ushort)m.WParam)
                {
                    case SB_LINEUP:
                        ChangeLeftColumn(leftColumn - 1);
                        break;
                    case SB_LINEDOWN:
                        ChangeLeftColumn(leftColumn + 1);
                        break;
                    case SB_TOP:
                        ChangeLeftColumn(0);
                        break;
                    case SB_BOTTOM:
                        ChangeLeftColumn(int.MaxValue);
                        break;
                    case SB_PAGEUP:
                        ChangeLeftColumn(leftColumn - 16);
                        break;
                    case SB_PAGEDOWN:
                        ChangeLeftColumn(leftColumn + 16);
                        break;
                    case SB_THUMBPOSITION:
                        ChangeLeftColumn((int)(((uint)m.WParam) >> 16));
                        break;
                }
        }

        private void HorizontalScrollChanged()
        {
            if (model.LineCount > 0)
                SetScrollInfo(1024, columnsInPage, leftColumn, false);
            else
                SetScrollInfo(0, 0, 0, false);
        }

        #endregion

        #region Window management.

        /// <summary>Parameters needed for creating a handle for the control.</summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_HSCROLL = 0x100000;
                const int WS_VSCROLL = 0x200000;

                CreateParams result = base.CreateParams;
                result.Style |= WS_VSCROLL | WS_HSCROLL;
                return result;
            }
        }

        /// <summary>Processes Windows messages.</summary>
        /// <param name="m">Information about the Windows message.</param>
        protected override void WndProc(ref Message m)
        {
            const int WM_SETCURSOR = 0x0020;
            const int WM_HSCROLL = 0x0114;
            const int WM_VSCROLL = 0x0115;

            switch (m.Msg)
            {
                case WM_SETCURSOR:
                    if (!changingCursor)
                    {
                        changingCursor = true;
                        Point p = PointToClient(MousePosition);
                        base.Cursor =
                            p.X < margin ? inverseArrow :
                            model.InsideSelection(GetPosition(p)) ? Cursors.Arrow :
                            Cursors.IBeam;
                        changingCursor = false;
                    }
                    base.WndProc(ref m);
                    break;
                case WM_HSCROLL:
                    if (m.LParam != IntPtr.Zero)
                        base.WndProc(ref m);
                    else
                        WmHScroll(ref m);
                    break;
                case WM_VSCROLL:
                    if (m.LParam != IntPtr.Zero)
                        base.WndProc(ref m);
                    else
                        WmVScroll(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public int cbSize;
            public int fMask;
            public int nMin;
            public int nMax;
            public int nPage;
            public int nPos;
            public int nTrackPos;
        }

        private int SetScrollInfo(int max, int page, int pos, bool vertical)
        {
            const int SB_HORZ = 0;
            const int SB_VERT = 1;
            const int SIF_RANGE = 0x1;
            const int SIF_PAGE = 0x2;
            const int SIF_POS = 0x4;

            SCROLLINFO info = new SCROLLINFO
            {
                cbSize = 28,
                fMask = SIF_POS | SIF_RANGE | SIF_PAGE,
                nMin = 0,
                nMax = max,
                nPage = page,
                nPos = pos
            };
            return SetScrollInfo(this.Handle, vertical ? SB_VERT : SB_HORZ, ref info, true);
        }

        private int ScrollWindow(int dx, int dy)
        {
            const uint SW_INVALIDATE = 0x0002;
            const uint SW_ERASE = 0x0004;

            return ScrollWindowEx(Handle, dx, dy,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                SW_INVALIDATE | SW_ERASE);
        }

        [DllImport("user32.dll")]
        private static extern int ScrollWindowEx(IntPtr wnd, int dx, int dy,
            IntPtr scroll, IntPtr clip, IntPtr region, IntPtr update, uint flags);
        [DllImport("user32.dll")]
        private static extern int SetScrollInfo(IntPtr hwnd, int fnBar,
            [In] ref SCROLLINFO lpsi, bool fRedraw);

        #endregion
    }
}
