using IntSight.Controls.CodeModel;
using Rsc = IntSight.Controls.Properties.Resources;

namespace IntSight.Controls;

public partial class CodeEditor
{
    #region ICodeView members.

    private struct BraceInfo
    {
        public string LeftStr, RightStr;
        public CodeEditor.Position LeftPos, RightPos;
        public bool Valid;
    }

    private BraceInfo braces;

    void ICodeView.LineCountChanged() =>
        SetScrollInfo(model.LineCount + linesInPage - 2, linesInPage, topLine, true);

    /// <summary>
    /// Invalidates lines in a given range, scrolls the client area when the current line
    /// moves outside the current view, and updates the caret position.
    /// </summary>
    /// <param name="from">First line to redraw.</param>
    /// <param name="to">Last line to redraw.</param>
    void ICodeView.Redraw(int from, int to)
    {
        bool mustScroll = false;
        int currLine = model.Current.line, currCol = model.Current.column;
        int oldTop = topLine, oldLeft = leftColumn;
        HideCaret(Handle);
        if (currLine < topLine)
        {
            topLine = currLine;
            view.LineCountChanged();
            mustScroll = true;
        }
        else if (currLine - topLine >= linesInPage - 1)
        {
            topLine = currLine - linesInPage + 2;
            view.LineCountChanged();
            mustScroll = true;
        }
        if (currCol < leftColumn)
        {
            leftColumn = currCol;
            HorizontalScrollChanged();
            mustScroll = true;
        }
        else if (currCol - leftColumn >= columnsInPage - 1)
        {
            leftColumn = currCol - columnsInPage + 2;
            HorizontalScrollChanged();
            mustScroll = true;
        }
        if (mustScroll)
            if (Math.Abs(topLine - oldTop) <= 8 && leftColumn == oldLeft)
            {
                mustScroll = false;
                ScrollWindow(0, (oldTop - topLine) * lineHeight);
            }
            else
                Invalidate();
        if (!mustScroll && from <= to)
        {
            int y0 = (from - topLine) * lineHeight;
            if (to == int.MaxValue)
                to = Height - y0;
            else
                to = (to - from + 1) * lineHeight;
            Invalidate(new Rectangle(0, y0, Width, to));
        }
        SetCaretPos();
    }

    void ICodeView.Redraw()
    {
        HideCaret(Handle);
        Invalidate();
        SetCaretPos();
    }

    CodeEditor ICodeView.Control => this;

    private void InvalidateLine(int lineNo)
    {
        if (lineNo >= topLine &&
            lineNo - topLine < linesInPage - 1)
        {
            int y0 = (lineNo - topLine) * lineHeight;
            Invalidate(new Rectangle(0, y0, Width, y0 + lineHeight));
        }
    }

    /// <summary>Notifies the code editor about matching parentheses/brackets.</summary>
    /// <param name="leftStr">String representation of the opening character.</param>
    /// <param name="rightStr">String representation of the closing character.</param>
    /// <param name="leftPos">Position of the opening character.</param>
    /// <param name="rightPos">Position of the closing character.</param>
    void ICodeView.MatchParentheses(string leftStr, string rightStr,
        CodeEditor.Position leftPos, CodeEditor.Position rightPos)
    {
        if (!braces.Valid || braces.LeftStr != leftStr ||
            braces.LeftPos.NotEquals(leftPos) || braces.RightPos.NotEquals(rightPos))
        {
            if (braces.Valid)
            {
                // Invalidate lines from the old matching pair.
                InvalidateLine(braces.LeftPos.line);
                if (braces.RightPos.line != braces.LeftPos.line)
                    InvalidateLine(braces.RightPos.line);
            }
            // Remember information about the new matching pair.
            braces.LeftStr = leftStr; braces.RightStr = rightStr;
            braces.LeftPos = leftPos; braces.RightPos = rightPos;
            braces.Valid = true;
            // Invalidate involved lines.
            InvalidateLine(leftPos.line);
            if (rightPos.line != leftPos.line)
                InvalidateLine(rightPos.line);
        }
    }

    /// <summary>Notifies the viewport there are no parentheses to hilite.</summary>
    void ICodeView.MatchNoParentheses()
    {
        if (braces.Valid)
        {
            InvalidateLine(braces.LeftPos.line);
            if (braces.RightPos.line != braces.LeftPos.line)
                InvalidateLine(braces.RightPos.line);
            braces.Valid = false;
        }
    }

    #endregion

    #region Painting.

    private void CalculateMeasures()
    {
        const string probe = "Aynnnnnnnnnnnnnn";
        using (Graphics g = CreateGraphics())
        {
            Size size = TextRenderer.MeasureText(
                g, probe, this.Font, new Size(), TextFormatFlags.NoPadding);
            lineHeight = size.Height;
            charWidth = size.Width / probe.Length;
        }
        linesInPage = Height / lineHeight;
        columnsInPage = (Width - margin - 1) / charWidth;
        view.LineCountChanged();
        HorizontalScrollChanged();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        CalculateMeasures();
        base.OnFontChanged(e);
    }

    protected override void OnResize(EventArgs e)
    {
        CalculateMeasures();
        if (lastSize.Height < Height)
        {
            Invalidate(new Rectangle(
                0, lastSize.Height, Width, Height - lastSize.Height));
            if (lastSize.Width < Width)
                Invalidate(new Rectangle(
                    lastSize.Width, 0, Width - lastSize.Width, lastSize.Height));
        }
        else
            Invalidate(new Rectangle(
                lastSize.Width, 0, Width - lastSize.Width, Height));
        lastSize = new Size(Width, linesInPage * lineHeight);
        base.OnResize(e);
    }

    private void DrawLine(Graphics g, int lineNumber, Bitmap bmp)
    {
        const TextFormatFlags flags =
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
            TextFormatFlags.Left | TextFormatFlags.NoPrefix;

        int y = (lineNumber - topLine) * lineHeight;
        foreach (Lexeme lex in model.Tokens(lineNumber, leftColumn, columnsInPage))
        {
            Color foreColor, backColor;
            switch (lex.Kind)
            {
                case Lexeme.Token.NewLine:
                    return;
                case Lexeme.Token.String:
                    foreColor = stringColor;
                    backColor = BackColor;
                    break;
                case Lexeme.Token.Keyword:
                    foreColor = keywordColor;
                    backColor = BackColor;
                    break;
                case Lexeme.Token.Comment:
                case Lexeme.Token.PartialComment:
                    foreColor = commentColor;
                    backColor = BackColor;
                    break;
                case Lexeme.Token.Selection:
                    foreColor = SystemColors.HighlightText;
                    backColor = SystemColors.Highlight;
                    break;
                case Lexeme.Token.Error:
                    foreColor = Color.Red;
                    backColor = Color.Gainsboro;
                    break;
                default:
                    foreColor = ForeColor;
                    backColor = BackColor;
                    break;
            }
            int column = lex.Column - leftColumn;
            string text = lex.Text;
            if (column < 0)
            {
                text = text.Length + column <= 0 ? string.Empty : text.Remove(0, -column);
                column = 0;
            }
            TextRenderer.DrawText(g, text, Font,
                new Point(column * charWidth + margin + 2, y),
                foreColor, backColor, flags);
        }
        // Draw a bookmark, if any.
        if (margin > 7 && model.IsBookmark(lineNumber))
            g.DrawImageUnscaled(bmp,
                Math.Max((margin - bmp.Width) / 2, 0),
                (lineHeight - bmp.Height) / 2 + y + 1);
        // Mark brackets/parentheses pairs, if any.
        if (braces.Valid)
        {
            if (lineNumber == braces.LeftPos.line &&
                !model.InsideSelection(braces.LeftPos))
            {
                int column = braces.LeftPos.column - leftColumn;
                if (column >= 0)
                    TextRenderer.DrawText(g, braces.LeftStr, Font,
                        new Point(charWidth * column + margin + 2, y),
                        ForeColor, bracketColor, flags);
            }
            if (lineNumber == braces.RightPos.line &&
                !model.InsideSelection(braces.RightPos))
            {
                int column = braces.RightPos.column - leftColumn;
                if (column >= 0)
                    TextRenderer.DrawText(g, braces.RightStr, Font,
                        new Point(charWidth * column + margin + 2, y),
                        ForeColor, bracketColor, flags);
            }
        }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using SolidBrush solid = new(BackColor);
        if (margin > 0)
        {
            Rectangle r = new(0, 0, margin, Height);
            if (pevent.ClipRectangle.IntersectsWith(r))
                using (SolidBrush brush = new(marginColor))
                    pevent.Graphics.FillRectangle(brush, r);
            pevent.Graphics.FillRectangle(solid, margin, 0, Width - margin, Height);
        }
        else
            pevent.Graphics.FillRectangle(solid, pevent.ClipRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        int y0 = e.ClipRectangle.Top;
        int y1 = (y0 + e.ClipRectangle.Height + lineHeight - 1) / lineHeight + topLine;
        if (y1 >= model.LineCount)
            y1 = model.LineCount - 1;
        if (y0 > 0)
            y0 = y0 / lineHeight + topLine;
        else
            y0 = topLine;
        Graphics g = e.Graphics;
        Bitmap bmp =
            margin < 12 ? Rsc.bkmark08 :
            margin < 15 ? Rsc.bkmark12 :
            Rsc.bkmark14;
        while (y0 <= y1)
            DrawLine(g, y0++, bmp);
        // This base method call fires the Paint event.
        // If you don't need any owner-draw functionality, drop this call.
        // base.OnPaint(e);
    }

    #endregion Painting.
}
