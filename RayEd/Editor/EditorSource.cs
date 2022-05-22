using IntSight.Controls;
using IntSight.Parser;
using System.Diagnostics.SymbolStore;

namespace RayEd;

public sealed class CodeEditorDocument : IDocument
{
    private ISymbolDocumentWriter symbolWriter;
    private readonly CodeEditor editor;

    public CodeEditorDocument(CodeEditor editor)
    {
        this.Url = string.Empty;
        this.editor = editor;
    }

    public override string ToString() => Url;

    #region IDocument Members

    public string Url { get; set; }

    string IDocument.Name => Url;

    ISymbolDocumentWriter IDocument.SymbolWriter
    {
        get => symbolWriter;
        set => symbolWriter = value;
    }

    public ISource Open() => new CodeEditorSource(this, editor);

    #endregion

    #region IComparable<IDocument> Members

    int IComparable<IDocument>.CompareTo(IDocument other) =>
        other == null ? -1 : string.Compare(Url, other.Url, StringComparison.InvariantCulture);

    #endregion
}

public sealed class CodeEditorSource : ISource
{
    private readonly CodeEditor editor;
    private string buffer;
    private readonly int lineCount;
    private int line;
    private int column;
    private int tokenPos;
    private int length;
    private readonly IDocument document;

    public CodeEditorSource(IDocument document, CodeEditor editor)
    {
        this.document = document;
        this.editor = editor;
        lineCount = editor.LineCount;
        line = 1;
        column = 0;
        tokenPos = 0;
        buffer = lineCount > 0 ? editor[0] : string.Empty;
        length = buffer.Length;
    }

    #region ISource members.

    void IDisposable.Dispose() { }

    ushort ISource.FirstChar
    {
        get
        {
        state0:
            if (column >= length)
            {
                if (line >= lineCount)
                {
                    tokenPos = column;
                    return 0;
                }
                buffer = editor[line++];
                length = buffer.Length;
                column = 0;
                goto state0;
            }
            switch (buffer[column])
            {
                case '\u0009':
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0020':
                case '\u00A0':
                    column++;
                    goto state0;
                case '{':
                    column++;
                    goto state1;
                case '/':
                    if (this[1] != '/')
                        return '/';
                    column += 2;
                    goto state2;
                default:
                    tokenPos = column;
                    return buffer[column];
            }

        state1:
            if (column >= length)
            {
                if (line >= lineCount)
                {
                    tokenPos = column;
                    return 0;
                }
                buffer = editor[line++];
                length = buffer.Length;
                column = 0;
                goto state1;
            }
            if (buffer[column] == '}')
            {
                column++;
                goto state0;
            }
            column++;
            goto state1;

        state2:
            if (column >= length)
            {
                if (line >= lineCount)
                {
                    tokenPos = column;
                    return 0;
                }
                buffer = editor[line++];
                length = buffer.Length;
                column = 0;
                goto state0;
            }
            column++;
            goto state2;
        }
    }

    IDocument ISource.Document => document;

    SourceRange ISource.GetRange(int length) =>
        new SourceRange(document,
            line, (short)(tokenPos + 1), line, (short)(tokenPos + length + 1));

    public ushort this[int position]
    {
        get
        {
            // ASSERT: position > 0
            position += column;
            return position >= length ? '\u000A' : buffer[position];
        }
    }

    string ISource.Read(int size)
    {
        // No valid token contains \u000A or \u000D.
        // There's no need to count line feeds here.
        string result = buffer.Substring(column, size);
        column += size;
        return result;
    }

    string ISource.Skip(int size)
    {
        column += size;
        return string.Empty;
    }

    #endregion
}
