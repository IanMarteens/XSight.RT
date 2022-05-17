using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IntSight.Controls
{
    public partial class CodeEditor
    {
        #region Caret.

        [DllImport("user32.dll")]
        private static extern bool CreateCaret(IntPtr wnd, IntPtr bmp, int width, int height);
        [DllImport("user32.dll")]
        private static extern bool DestroyCaret();
        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowCaret(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool SetCaretPos(int x, int y);

        #endregion

        #region Mouse.

        public string GetMouseText()
        {
            using (model.WrapOperation())
                return model.GetIdentifier(GetPosition(PointToClient(MousePosition)));
        }

        public string GetMouseText(bool toEol)
        {
            using (model.WrapOperation())
            {
                Position p = GetPosition(PointToClient(MousePosition));
                if (toEol)
                    return model.GetLineSufix(p);
                else
                    return model.GetIdentifier(p);
            }
        }

        protected Position GetPosition(int x, int y) => new(
            topLine + y / lineHeight, leftColumn + (x - margin - 2) / charWidth);

        protected Position GetPosition(Point p) => new(
            topLine + p.Y / lineHeight, leftColumn + (p.X - margin - 2) / charWidth);

        protected Position GetTextPosition(Point p)
        {
            Position pos = new(
                topLine + p.Y / lineHeight, leftColumn + (p.X - margin - 2) / charWidth);
            int v = model.LineCount;
            if (v == 0)
                pos.line = 0;
            else if (pos.line >= v)
                pos.line = v - 1;
            v = model[pos.line].Length;
            if (pos.column > v)
                pos.column = v;
            return pos;
        }

        private void SetCaretPos()
        {
            SetCaretPos(
                margin + (model.Current.column - leftColumn) * charWidth + 2,
                (model.Current.line - topLine) * lineHeight);
            ShowCaret(Handle);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            using (model.WrapOperation())
            {
                if (e.Button != MouseButtons.Right)
                {
                    model.SelectWord(GetPosition(e.X, e.Y));
                    doubleClicked = true;
                }
                base.OnMouseDoubleClick(e);
            }
        }

        /// <summary>Called when a mouse button is pressed.</summary>
        /// <param name="e">Mouse event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            using (model.WrapOperation())
            {
                Position p = GetPosition(e.X, e.Y);
                marginSelected = false;
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        if (e.X < margin)
                        {
                            model.Select(p.line);
                            marginSelected = true;
                        }
                        else if (!model.InsideSelection(p))
                        {
                            this.Focus();
                            model.MoveTo(p, (ModifierKeys & Keys.Shift) != 0);
                            Capture = true;
                        }
                        else
                        {
                            dragMoved = false;
                            DoDragDrop(this, DragDropEffects.Copy | DragDropEffects.Move);
                        }
                        break;
                    case MouseButtons.Right:
                        if (!model.InsideSelection(p))
                        {
                            this.Focus();
                            model.MoveTo(p, false);
                        }
                        break;
                }
                base.OnMouseDown(e);
            }
        }

        /// <summary>Called when the mouse position changes.</summary>
        /// <param name="e">Mouse event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            using (model.WrapOperation())
            {
                // If there's a capture, then we are extending a selection with the mouse.
                if (Capture && e.Button == MouseButtons.Left)
                {
                    Position p = GetPosition(e.X, e.Y);
                    if (p.NotEquals(model.Current))
                        model.MoveTo(p, true);
                }
                base.OnMouseMove(e);
            }
        }

        /// <summary>Called when a mouse button is released.</summary>
        /// <param name="e">Mouse event data.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            using (model.WrapOperation())
            {
                base.OnMouseUp(e);
                if (!marginSelected && Capture && e.Button == MouseButtons.Left)
                {
                    Capture = false;
                    if (doubleClicked)
                        doubleClicked = false;
                    else
                        model.MoveTo(GetPosition(e.X, e.Y), true);
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            using (model.WrapOperation())
            {
                int scrollLines = -e.Delta * SystemInformation.MouseWheelScrollLines / 120;
                if (scrollLines < 0)
                    scrollLines = Math.Max(scrollLines, -topLine);
                else if (scrollLines > 0)
                    scrollLines = Math.Min(scrollLines, model.LineCount - topLine - 1);
                if (scrollLines != 0 && model.LineCount > 0)
                    model.ScrollTo(ref topLine, topLine + scrollLines, linesInPage);
                base.OnMouseWheel(e);
            }
        }

        #endregion

        #region Drag and drop.

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            using (model.WrapOperation())
            {
                if (drgevent.Data.GetDataPresent(typeof(CodeEditor)))
                {
                    Point p = PointToClient(new Point(drgevent.X, drgevent.Y));
                    if (p.X < margin)
                        p.X = margin;
                    Position pos = GetTextPosition(p);
                    HideCaret(Handle);
                    SetCaretPos(
                        margin + (pos.column - leftColumn) * charWidth + 1,
                        (pos.line - topLine) * lineHeight);
                    ShowCaret(Handle);
                    drgevent.Effect = (drgevent.KeyState & 8) == 8 ?
                        DragDropEffects.Copy : DragDropEffects.Move;
                }
                else
                    drgevent.Effect = DragDropEffects.None;
                base.OnDragOver(drgevent);
            }
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            using (model.WrapOperation())
            {
                if (drgevent.Data.GetDataPresent(typeof(CodeEditor)))
                {
                    if (drgevent.Data.GetData(typeof(CodeEditor)) is CodeEditor ed)
                        if (ed != this)
                        {
                            if (ed.model.HasSelection)
                                using (model.WrapOperation())
                                {
                                    model.MoveTo(GetTextPosition(PointToClient(
                                        new Point(drgevent.X, drgevent.Y))), false);
                                    this.model.SelectedText = ed.model.SelectedText;
                                }
                        }
                        else if (model.HasSelection)
                            model.DragSelection(
                                GetTextPosition(PointToClient(
                                new Point(drgevent.X, drgevent.Y))),
                                drgevent.Effect == DragDropEffects.Copy);
                }
                base.OnDragDrop(drgevent);
            }
        }

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
        {
            using (model.WrapOperation())
                if (qcdevent.EscapePressed)
                {
                    HideCaret(Handle);
                    SetCaretPos();
                }
                else
                    // If a drag operation is started, but the mouse never moves outside
                    // the initial security threshold, the OnDragDrop method is not called.
                    // However, OnQueryContinueDrag is called with qcdevent.Action == Drop.
                    switch (qcdevent.Action)
                    {
                        case DragAction.Continue:
                            dragMoved = true;
                            break;
                        case DragAction.Drop:
                            if (!dragMoved)
                                model.DragSelection(
                                    GetTextPosition(PointToClient(MousePosition)), false);
                            break;
                    }
        }

        #endregion
    }
}
