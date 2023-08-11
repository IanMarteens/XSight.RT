using Rsc = IntSight.Parser.Properties.Resources;

namespace IntSight.Parser;

/// <summary>Represents an interval inside a single source code file.</summary>
/// <remarks>This is an immutable data structure.</remarks>
public readonly struct SourceRange : IComparable<SourceRange>, IEquatable<SourceRange>
{
    private class NullDocument : IDocument
    {
        #region IDocument Members

        ISource IDocument.Open() => null;
        string IDocument.Url => string.Empty;
        string IDocument.Name => string.Empty;

        ISymbolDocumentWriter IDocument.SymbolWriter
        {
            get => null;
            set { }
        }

        #endregion

        #region IComparable<IDocument> Members

        int IComparable<IDocument>.CompareTo(IDocument other) => this == other ? 0 : -1;

        #endregion
    }

    public static readonly SourceRange Default = new(new NullDocument(), int.MaxValue);

    public readonly int FromLine, ToLine;
    public readonly short FromColumn, ToColumn;
    public readonly IDocument Document;

    public SourceRange(IDocument document,
        int fromLine, short fromColumn, int toLine, short toColumn)
    {
        Document = document;
        FromLine = fromLine;
        FromColumn = fromColumn;
        ToLine = toLine;
        ToColumn = toColumn;
    }

    public SourceRange(IDocument document)
        : this(document, int.MaxValue) { }
    public SourceRange(SourceRange from, SourceRange to)
        : this(from.Document, from.FromLine, from.FromColumn, to.ToLine, to.ToColumn) { }
    public SourceRange(IDocument document, int line, short column)
        : this(document, line, column, line, column) { }
    public SourceRange(IDocument document, int line) : this(document, line, 0) { }

    public SourceRange FromStart() =>
        new(Document, FromLine, FromColumn, FromLine, FromColumn);

    /// <summary>Creates a new range keeping the document reference.</summary>
    /// <param name="fromLine">First line number.</param>
    /// <param name="fromColumn">First column number.</param>
    /// <param name="toLine">Last line number.</param>
    /// <param name="toColumn">Last column number.</param>
    /// <returns>The new created range.</returns>
    public SourceRange NewRange(int fromLine, short fromColumn, int toLine, short toColumn) =>
        new(Document, fromLine, fromColumn, toLine, toColumn);

    public bool Contains(int line, int column) =>
        (line > FromLine || line == FromLine && column >= FromColumn) &&
            (line < ToLine || line == ToLine && column <= ToColumn);

    public bool Contains(ref SourceRange other) =>
        (other.FromLine > FromLine ||
            other.FromLine == FromLine && other.FromColumn >= FromColumn) &&
            (other.ToLine < ToLine ||
            other.ToLine == ToLine && other.ToColumn <= ToColumn);

    public bool IsDefault =>
        FromLine == int.MaxValue && ToLine == int.MaxValue &&
                FromColumn == 0 && ToColumn == 0;

    public override bool Equals(object obj) =>
        obj is SourceRange range && Equals(range);

    /// <summary>Hash, mix & remix parts of the range.</summary>
    /// <returns>Who cares? It will never be called...</returns>
    public override int GetHashCode() =>
        ((uint)FromLine | ((uint)FromColumn << 16) |
            (ulong)((uint)ToLine | ((uint)ToColumn << 16)) << 32).GetHashCode();

    public override string ToString()
    {
        string docName = Document?.Name ?? string.Empty;
        return FromLine == int.MaxValue || FromLine <= 0
            ? docName
            : FromColumn == 0
            ? string.Format(Rsc.FormatLine, docName, FromLine)
            : FromLine == ToLine && ToColumn - FromColumn > 1
            ? string.Format(Rsc.FormatLineRange,
                docName, FromLine, FromColumn, ToColumn)
            : string.Format(Rsc.FormatLineColumn, docName, FromLine, FromColumn);
    }

    public string ToString(string formatString)
    {
        string docName = Document?.Name ?? string.Empty;
        return string.Format(formatString, docName, FromLine, FromColumn, ToLine, ToColumn);
    }

    #region IComparable<SourceRange> members.

    public int CompareTo(SourceRange other)
    {
        int result = Document == null ?
            (other.Document == null ? 0 : +1) :
            Document.CompareTo(other.Document);
        if (result == 0)
        {
            result = FromLine.CompareTo(other.FromLine);
            if (result == 0)
            {
                result = FromColumn.CompareTo(other.FromColumn);
                if (result == 0)
                {
                    result = other.ToLine.CompareTo(ToLine);
                    if (result == 0)
                        result = other.ToColumn.CompareTo(ToColumn);
                }
            }
        }
        return result;
    }

    #endregion

    #region IEquatable<SourceRange> members.

    public bool Equals(SourceRange other) =>
        Document == other.Document &&
            FromLine == other.FromLine && FromColumn == other.FromColumn &&
            ToLine == other.ToLine && ToColumn == other.ToColumn;

    #endregion

    /// <summary>Union of source ranges.</summary>
    /// <param name="r1">First source range.</param>
    /// <param name="r2">Second source range.</param>
    /// <returns>The combined source range.</returns>
    public static SourceRange operator +(SourceRange r1, SourceRange r2)
    {
        int fl; short fc;
        if (r1.FromLine < r2.FromLine ||
            r1.FromLine == r2.FromLine && r1.FromColumn <= r2.FromColumn)
            (fl, fc) = (r1.FromLine, r1.FromColumn);
        else
            (fl, fc) = (r2.FromLine, r2.FromColumn);
        return r1.ToLine > r2.ToLine ||
            r1.ToLine == r2.ToLine && r1.ToColumn >= r2.ToColumn
            ? new(r1.Document, fl, fc, r1.ToLine, r1.ToColumn)
            : new(r1.Document, fl, fc, r2.ToLine, r2.ToColumn);
    }

    public static bool operator ==(SourceRange left, SourceRange right) =>
        left.Equals(right);

    public static bool operator !=(SourceRange left, SourceRange right) =>
        !left.Equals(right);
}
