using IntSight.Controls.CodeModel;

namespace IntSight.Controls;

public partial class CodeEditor
{
    /// <summary>Saves an HTML representation of the editor's content.</summary>
    /// <param name="fileName">A file name.</param>
    public void WriteHtml(string fileName)
    {
        using TextWriter writer = File.CreateText(fileName);
        writer.WriteLine("<html>");
        writer.WriteLine("<head>");
        writer.WriteLine("<style>");
        writer.WriteLine(string.Format(
            "PRE B {{ color: \"#{0:X6}\"; font-weight: normal; }}",
            KeywordColor.ToArgb() & 0xFFFFFF));
        writer.WriteLine(string.Format(
            "PRE I {{ color: \"#{0:X6}\"; font-style: normal; }}",
            CommentColor.ToArgb() & 0xFFFFFF));
        writer.WriteLine(string.Format(
            "PRE SPAN.STR {{ color: \"#{0:X6}\"; }}",
            StringColor.ToArgb() & 0xFFFFFF));
        writer.WriteLine("</style>");
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
        WriteHtml(writer);
        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    /// <summary>Saves an HTML representation of the editor's content.</summary>
    /// <param name="fileName">The text writer to receive the HTML text.</param>
    /// <remarks>Saved text does not contain CSS definitions.</remarks>
    public void WriteHtml(TextWriter writer)
    {
        writer.Write("<pre>");
        foreach (Lexeme lexeme in this.Tokens())
            switch (lexeme.Kind)
            {
                case Lexeme.Token.NewLine:
                    writer.WriteLine();
                    break;
                case Lexeme.Token.Keyword:
                    writer.Write("<b>");
                    writer.Write(lexeme.Text);
                    writer.Write("</b>");
                    break;
                case Lexeme.Token.String:
                    writer.Write("<span class=\"str\">");
                    writer.Write(HtmlEncode(lexeme.Text));
                    writer.Write("</span>");
                    break;
                case Lexeme.Token.PartialComment:
                case Lexeme.Token.Comment:
                    writer.Write("<i>");
                    writer.Write(HtmlEncode(lexeme.Text));
                    writer.Write("</i>");
                    break;
                default:
                    writer.Write(HtmlEncode(lexeme.Text));
                    break;
            }
        writer.WriteLine("</pre>");
    }

    private static string HtmlEncode(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}