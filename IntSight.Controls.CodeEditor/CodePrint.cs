using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using IntSight.Controls.CodeModel;

namespace IntSight.Controls
{
    /// <summary>Prints the document managed by a code editor.</summary>
    public class EditorDocument : PrintDocument
    {
        private IEnumerator<Lexeme> tokenizer;
        private readonly StringFormat stringFormat;
        private Font italicFont, boldFont;
        private int pageNumber, lineNumber;
        private float lineHeight, xPos, yPos;

        public EditorDocument()
        {
            stringFormat = new StringFormat(StringFormat.GenericTypographic);
            stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            ResetFont();
            LineNumbers = true;
        }

        [Browsable(true)]
        [Description("The code editor providing the content to be printed.")]
        public CodeEditor Editor { get; set; }

        [Browsable(true)]
        [Description("Show line numbers along the left margin.")]
        [DefaultValue(true)]
        public bool LineNumbers { get; set; }

        [Browsable(true)]
        [Description("Base font used to print text.")]
        public Font Font { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private bool ShouldSerializeFont() =>
            string.Compare(Font.FontFamily.Name, "Courier New",
                StringComparison.InvariantCultureIgnoreCase) != 0 ||
                Font.SizeInPoints != 9.0F ||
                Font.Style != 0;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private void ResetFont()
        {
            Font?.Dispose();
            Font = new Font("Courier New", 9.0F);
        }

        protected override void OnBeginPrint(PrintEventArgs e)
        {
            if (Editor != null)
            {
                italicFont = new Font(Font, FontStyle.Italic);
                boldFont = new Font(Font, FontStyle.Bold);
                pageNumber = 0;
                lineNumber = 0;
                tokenizer = Editor.Tokens().GetEnumerator();
            }
            base.OnBeginPrint(e);
        }

        protected override void OnEndPrint(PrintEventArgs e)
        {
            base.OnEndPrint(e);
            if (Editor != null)
            {
                boldFont.Dispose();
                italicFont.Dispose();
                tokenizer.Dispose();
                tokenizer = null;
                italicFont = boldFont = null;
            }
        }

        protected override void OnPrintPage(PrintPageEventArgs e)
        {
            if (Editor != null)
            {
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                float leftMargin = e.MarginBounds.Left;
                float topMargin = e.MarginBounds.Top;

                // Calculate the number of lines per page.
                lineHeight = Font.GetHeight(e.Graphics);
                float linesPerPage = e.MarginBounds.Height / lineHeight;
                // Print a header.
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.Trimming = StringTrimming.EllipsisPath;
                e.Graphics.DrawString(
                    DocumentName, Font, Brushes.Black,
                    new RectangleF(
                        leftMargin, topMargin, e.MarginBounds.Width - 20, lineHeight),
                    stringFormat);
                stringFormat.Trimming = StringTrimming.None;
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString((++pageNumber).ToString(),
                    Font, Brushes.Black,
                    new RectangleF(
                        leftMargin, topMargin, e.MarginBounds.Width, lineHeight),
                    stringFormat);
                e.Graphics.DrawLine(Pens.Black,
                    leftMargin, topMargin + lineHeight + 2,
                    e.MarginBounds.Right, topMargin + lineHeight + 2);
                linesPerPage -= 2;
                // Print each line of the file.
                stringFormat.Trimming = StringTrimming.EllipsisCharacter;
                stringFormat.Alignment = StringAlignment.Near;
                int lineNo = 0;
                xPos = leftMargin;
                yPos = topMargin + 2 * lineHeight;
                bool atFirstCharacter = true;
                while (true)
                {
                    if (!tokenizer.MoveNext())
                    {
                        base.OnPrintPage(e);
                        e.HasMorePages = false;
                        return;
                    }
                    Lexeme lexeme = tokenizer.Current;
                    if (atFirstCharacter && LineNumbers)
                    {
                        PrintLineNumber(e);
                        atFirstCharacter = false;
                    }
                    switch (lexeme.Kind)
                    {
                        case Lexeme.Token.Text:
                        case Lexeme.Token.Error:
                            PrintText(e, lexeme.Text, Font, Brushes.Black);
                            break;
                        case Lexeme.Token.Keyword:
                            PrintText(e, lexeme.Text, boldFont, Brushes.Navy);
                            break;
                        case Lexeme.Token.String:
                            PrintText(e, lexeme.Text, Font, Brushes.Maroon);
                            break;
                        case Lexeme.Token.PartialComment:
                        case Lexeme.Token.Comment:
                            PrintText(e, lexeme.Text, italicFont, Brushes.Green);
                            break;
                        case Lexeme.Token.NewLine:
                            lineNo++;
                            lineNumber++;
                            atFirstCharacter = true;
                            xPos = leftMargin;
                            yPos += lineHeight;
                            if (lineNo >= linesPerPage)
                            {
                                base.OnPrintPage(e);
                                e.HasMorePages = true;
                                return;
                            }
                            break;
                    }
                }
            }
            base.OnPrintPage(e);
        }

        private void PrintText(PrintPageEventArgs e, string text, Font font, Brush brush)
        {
            if (xPos < e.MarginBounds.Right)
            {
                RectangleF rect = new RectangleF(
                    xPos, yPos, e.MarginBounds.Right - xPos, lineHeight);
                e.Graphics.DrawString(text, font, brush, rect, stringFormat);
                xPos += e.Graphics.MeasureString(text, font,
                    new PointF(0, 0), stringFormat).Width;
            }
        }

        private void PrintLineNumber(PrintPageEventArgs e)
        {
            string lineText = (lineNumber + 1).ToString() + " ";
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.Trimming = StringTrimming.None;
            e.Graphics.DrawString(lineText, Font, Brushes.DimGray,
                new RectangleF(0, yPos, e.MarginBounds.Left, lineHeight), stringFormat);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.Trimming = StringTrimming.EllipsisCharacter;
        }
    }
}
