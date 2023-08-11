using IntSight.Controls.CodeModel;

namespace IntSight.Controls;

public partial class CodeEditor
{
    /// <summary>
    /// The data structure that represents the document being edited by a code editor.
    /// </summary>
    private partial class CodeModel : ICodeModel
    {
        private const string CR_LF = "\r\n";
        /// <summary>Characters in whitespace.</summary>
        private readonly static char[] trimChars = new char[] { ' ', '\t', '\n', '\r' };
        private readonly static string[] lineSeparators = new string[] { CR_LF };

        /// <summary>
        /// Represents the relationship between lines and multiline comments.
        /// </summary>
        private enum LineState : byte
        {
            /// <summary>This line has nothing to do with multiline comments.</summary>
            Clean,
            /// <summary>This line contains an opening brace.</summary>
            Open,
            /// <summary>This line is contained inside a multiline comment.</summary>
            Inside,
            /// <summary>This line contains a closing brace.</summary>
            Close,
            /// <summary>This line fully contains a multiline comment.</summary>
            Contains,
            /// <summary>This line contains an unpaired closing brace.</summary>
            Error
        }

        /// <summary>
        /// Represents both a line and its state, according to its relationship
        /// relative to multiline comments.
        /// </summary>
        private struct Header
        {
            public static readonly Header Empty = new(LineState.Clean);

            public string Line;
            public LineState State;

            public Header(string line, LineState state) =>
                (this.Line, this.State) = (line, state);

            /// <summary>
            /// Initializes a clean line, w/ no multiline comments in its neighborhood.
            /// </summary>
            /// <param name="line">Line text.</param>
            public Header(string line) =>
                (this.Line, this.State) = (line, LineState.Clean);

            public Header(LineState state) : this(string.Empty, state) { }

            public readonly string TrimmedLine => Line.TrimEnd(trimChars);

            public readonly int Length => Line.Length;

            public readonly int Indent
            {
                get
                {
                    for (int i = 0; i < Line.Length; i++)
                        if (!char.IsWhiteSpace(Line, i))
                            return i;
                    return 0;
                }
            }

            public readonly bool IsFirstChar(int position)
            {
                // All previous characters must be blanks.
                for (int i = 0; i < position; i++)
                    if (!char.IsWhiteSpace(Line, i))
                        return false;
                // It's ok when we are at the end of the line,
                // or when the current character is not a white space.
                if (position == Line.Length || !char.IsWhiteSpace(Line, position))
                    return true;
                // Otherwise, all trailing characters must be blanks to succeed.
                for (int i = position + 1; i < Line.Length; i++)
                    if (!char.IsWhiteSpace(Line, i))
                        return false;
                return true;
            }
        }

        private readonly ICodeView view;
        private ICodeScanner scanner;
        private readonly ICodeScanner defaultScanner;
        private Header[] lines;
        private int lineCount, capacity, tabLength;
        private Position current, from;
        private Position old0, old1;
        private readonly Stack<UndoAction> undoList;
        private readonly Stack<UndoAction> redoList;
        private bool modified, smartIndentation, mergeUndoCommands;
        private Position braceLeft, braceRight;
        private readonly List<int> bookmarks;
        // State change watcher.
        private readonly OperationWrapper wrapper;
        private readonly SelectionChangedEventArgs watchEventArgs;
        private int watchLevel;
        private Position watchOriginalFrom, watchOriginalTo;
        private long watchTimestamp;
        /// <summary>Master timestamp, incremented with each modification.</summary>
        private long timestamp;

        /// <summary>Creates an internal model for a code editor.</summary>
        /// <param name="owner">The editor control.</param>
        public CodeModel(ICodeView owner)
        {
            this.wrapper = new OperationWrapper(this);
            this.watchEventArgs = new SelectionChangedEventArgs(
                Position.Zero, Position.Zero);
            this.defaultScanner = new DefaultScanner();
            this.scanner = this.defaultScanner;
            this.view = owner;
            this.lines = new Header[256];
            this.capacity = 256;
            this.lines[0] = Header.Empty;
            this.lineCount = 1;
            this.undoList = new Stack<UndoAction>();
            this.redoList = new Stack<UndoAction>();
            this.tabLength = 4;
            this.smartIndentation = true;
            this.mergeUndoCommands = true;
            this.bookmarks = new List<int>();
        }

        #region ICodeModel document management.

        /// <summary>Initializes an empty document.</summary>
        void ICodeModel.Reset()
        {
            lines = new Header[256];
            capacity = 256;
            lines[0] = Header.Empty;
            lineCount = 1;
            current = new Position();
            from = new Position();
            undoList.Clear();
            redoList.Clear();
            bookmarks.Clear();
            view.LineCountChanged();
            modified = true;
            Modified = false;
        }

        /// <summary>Loads contents from a text reader.</summary>
        /// <param name="reader">Text reader with source code.</param>
        void ICodeModel.Load(TextReader reader)
        {
            lineCount = 0;
            string tabString = new(' ', tabLength);
            while (true)
            {
                string s = reader.ReadLine();
                if (s == null) break;
                if (lineCount >= lines.Length - 4)
                {
                    Array.Resize<Header>(ref lines, capacity + 256);
                    capacity = lines.Length;
                }
                lines[lineCount++] = new Header(
                    s.TrimEnd(trimChars).Replace("\t", tabString));
            }
            if (lineCount == 0)
            {
                lines[0] = Header.Empty;
                lineCount = 1;
            }
            // After loading, all lines are grouped
            // as if the cursor moved to the last line.
            current.line = lineCount - 1;
            Current = Position.Zero;
            from = new Position();
            undoList.Clear();
            redoList.Clear();
            bookmarks.Clear();
            RecalculateLineStates(0, lineCount - 1);
            view.LineCountChanged();
            modified = true;
            Modified = false;
        }

        /// <summary>Save contents to a text writer.</summary>
        /// <param name="writer">Output text writer.</param>
        void ICodeModel.Save(TextWriter writer)
        {
            for (int i = 0; i <= current.line; i++)
                writer.WriteLine(lines[i].TrimmedLine);
            for (int i = capacity - lineCount + current.line + 1; i < capacity; i++)
                writer.WriteLine(lines[i].TrimmedLine);
            Modified = false;
        }

        /// <summary>Total characters in the active document.</summary>
        /// <returns>Character count.</returns>
        int ICodeModel.GetDocumentSize()
        {
            int result = 0;
            for (int i = 0; i <= current.line; i++)
                result += lines[i].TrimmedLine.Length + 2;
            for (int i = capacity - lineCount + current.line + 1; i < capacity; i++)
                result += lines[i].TrimmedLine.Length + 2;
            return result;
        }

        #endregion

        #region Bookmarks.

        /// <summary>Has the given line an associated bookmark?</summary>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>True if succeeds.</returns>
        bool ICodeModel.IsBookmark(int lineNumber)
        {
            return bookmarks.Contains(lineNumber);
        }

        /// <summary>Toggles a bookmark for the current line.</summary>
        /// <returns>True if it activates a bookmark.</returns>
        bool ICodeModel.ToggleBookmark()
        {
            int lineNo = current.line;
            if (lineNo < 0 || lineNo >= lineCount)
                return false;
            int p = bookmarks.IndexOf(lineNo);
            if (p != -1)
            {
                bookmarks.RemoveAt(p);
                view.Redraw(lineNo, lineNo);
                return false;
            }
            else
            {
                bookmarks.Add(lineNo);
                view.Redraw(lineNo, lineNo);
                return true;
            }
        }

        /// <summary>Moves the insertion point to the nearest bookmark.</summary>
        /// <param name="forward">True for the next bookmark.</param>
        void ICodeModel.GotoBookmark(bool forward)
        {
            int bestF = int.MaxValue, lowestF = int.MaxValue;
            int bestB = int.MinValue, highestB = int.MinValue;
            foreach (int bkm in bookmarks)
            {
                if (bkm > current.line && bkm < bestF)
                    bestF = bkm;
                else if (bkm < current.line && bkm > bestB)
                    bestB = bkm;
                if (bkm < lowestF)
                    lowestF = bkm;
                if (bkm > highestB)
                    highestB = bkm;
            }
            if (forward)
            {
                if (bestF == int.MaxValue)
                    if (lowestF < int.MaxValue)
                        bestF = lowestF;
                    else
                        return;
                MoveTo(new(bestF, 0), false);
            }
            else
            {
                if (bestB == int.MinValue)
                    if (highestB > int.MinValue)
                        bestB = highestB;
                    else
                        return;
                MoveTo(new(bestB, 0), false);
            }
        }

        /// <summary>Are there any active bookmarks in this control?</summary>
        bool ICodeModel.HasBookmarks { get { return bookmarks.Count > 0; } }

        private void LinesInserted(int first, int count)
        {
            for (int i = 0; i < bookmarks.Count; i++)
            {
                int bkm = bookmarks[i];
                if (bkm >= first)
                    bookmarks[i] = bkm + count;
            }
        }

        private void LinesDeleted(int first, int count, bool inclusive)
        {
            for (int i = 0; i < bookmarks.Count; )
            {
                int bkm = bookmarks[i];
                if (bkm < first || bkm == first && !inclusive)
                    i++;
                else if (bkm < first + count)
                    bookmarks.RemoveAt(i);
                else
                {
                    bookmarks[i] = bkm - count;
                    i++;
                }
            }
        }

        #endregion

        #region Selection and clipboard.

        /// <summary>Copies selected text into the clipboard.</summary>
        /// <param name="cut">When true, removes the selected text.</param>
        void ICodeModel.MoveToClipboard(bool cut)
        {
            if (RememberSelection())
            {
                Clipboard.SetText(TextRange(old0, old1));
                if (cut)
                    DeleteSelection(true);
            }
        }

        /// <summary>Inserts clipboard content at the insertion point.</summary>
        void ICodeModel.Paste()
        {
            if (Clipboard.ContainsText())
            {
                DeleteSelection(false);
                Position start = current;
                InsertRange(current, Clipboard.GetText());
                RecalculateLineStates(start.line, lineCount - 1);
                Modify(new PasteAction(start, current));
            }
        }

        /// <summary>Checks whether a given position is inside the selected text area.</summary>
        /// <param name="position">A position inside the text.</param>
        /// <returns>True if succeeds.</returns>
        bool ICodeModel.InsideSelection(Position position)
        {
            if (from.line == current.line)
                if (from.column == current.column)
                    return false;
                else if (from.column < current.column)
                    return from.IsLesserEqual(position) && position.IsLesserThan(current);
                else
                    return current.IsLesserEqual(position) && position.IsLesserThan(from);
            else if (from.line < current.line)
                return from.IsLesserEqual(position) && position.IsLesserThan(current);
            else
                return current.IsLesserEqual(position) && position.IsLesserThan(from);
        }

        /// <summary>Returns all text in the control.</summary>
        string ICodeModel.Text =>
            lineCount == 0
                    ? string.Empty
                    : TextRange(Position.Zero,
                        new Position(lineCount - 1, this[lineCount - 1].Length));

        /// <summary>Returns text inside selection, or an empty string.</summary>
        string ICodeModel.SelectedText
        {
            get { return TextRange(from, current); }
            set
            {
                string original;
                if (RememberSelection())
                    original = TextRange(old0, old1);
                else
                {
                    old0 = old1 = current;
                    original = string.Empty;
                }
                if (string.Compare(original, value) != 0)
                {
                    InternalDelete(old0, old1);
                    view.Redraw();
                    InsertRange(old0, value);
                    Modify(new ReplaceAction(old0, current, original));
                    RecalculateLineStates(old0.line, lineCount - 1);
                }
            }
        }

        /// <summary>Does this control have any selected text?</summary>
        bool ICodeModel.HasSelection { get { return !from.Equals(current); } }

        /// <summary>Selects all text in the control.</summary>
        void ICodeModel.SelectAll()
        {
            if (lineCount == 0)
            {
                lineCount = 1;
                lines[0] = Header.Empty;
            }
            RememberSelection();
            from = Position.Zero;
            Current = Position.Infinite;
            // This version of Redraw scrolls the window to show the last line.
            view.Redraw(0, lineCount - 1);
        }

        /// <summary>Selects text in a whole line.</summary>
        /// <param name="lineNumber">Line to be selected.</param>
        void ICodeModel.Select(int lineNumber)
        {
            if (lineNumber < 0)
                lineNumber = 0;
            else if (lineNumber >= lineCount)
                lineNumber = lineCount - 1;
            MoveTo(new(lineNumber, 0), false);
            MoveTo(
                lineNumber == lineCount - 1 ?
                    new(lineNumber, int.MaxValue) :
                    new(lineNumber + 1, 0),
                true);
        }

        void ICodeModel.SelectTrim(Position start, Position end)
        {
            MoveTo(start, false);
            if (end.line < lineCount)
            {
                string line = this[end.line];
                if (end.column > line.Length)
                    end.column = line.Length;
            LOOP:
                while (end.IsGreaterThan(start) && end.column > 0 &&
                    char.IsWhiteSpace(line[end.column - 1]))
                    end.column--;
                if (end.IsGreaterThan(start) && end.column == 0)
                {
                    end.line--;
                    line = this[end.line];
                    end.column = line.Length;
                    goto LOOP;
                }
            }
            MoveTo(end, true);
        }

        /// <summary>Selects a word at a given position.</summary>
        /// <param name="position">A position inside the word.</param>
        void ICodeModel.SelectWord(Position position)
        {
            if (lineCount == 0) return;
            if (position.line < 0)
                position.line = 0;
            else if (position.line >= lineCount)
                position.line = lineCount - 1;
            string s = this[position.line];
            if (s.Length == 0) return;
            if (position.column >= s.Length)
                position.column = s.Length - 1;

            int start = position.column;
            RememberSelection();
            switch (GetCharClass(s, start))
            {
                case CharClass.Ident:
                    while (start > 0 && GetCharClass(s, start - 1) == CharClass.Ident)
                        start--;
                    while (position.column < s.Length - 1 &&
                        GetCharClass(s, position.column + 1) == CharClass.Ident)
                        position.column++;
                    bool allDigits = true;
                    for (int i = start; i <= position.column; i++)
                        if (!char.IsDigit(s, i))
                        {
                            allDigits = false;
                            break;
                        }
                    if (allDigits)
                    {
                        if (start > 0 && s[start - 1] == '.')
                        {
                            start--;
                            while (start > 0 && char.IsDigit(s, start - 1))
                                start--;
                        }
                        if (position.column < s.Length - 1 && s[position.column + 1] == '.')
                        {
                            position.column++;
                            while (position.column < s.Length - 1 &&
                                char.IsDigit(s, position.column + 1))
                                position.column++;
                        }
                    }
                    break;
                case CharClass.Blank:
                    while (start > 0 && char.IsWhiteSpace(s, start - 1))
                        start--;
                    while (position.column < s.Length - 1 &&
                        char.IsWhiteSpace(s, position.column + 1))
                        position.column++;
                    break;
            }
            position.column++;
            Current = position;
            from = new Position(position.line, start);
            Redraw(true);
        }

        /// <summary>Gets the identifier before the insertion point.</summary>
        /// <returns>The identifier, if any; otherwise, an empty string.</returns>
        string ICodeModel.GetIdentifier()
        {
            if (from.NotEquals(current))
                return TextRange(from, current);
            if (lineCount > 0)
            {
                string s = lines[current.line].Line;
                int col = current.column;
                if (GetCharClass(s, col) == CharClass.Ident ||
                    col-- > 0 && GetCharClass(s, col) == CharClass.Ident)
                {
                    int start = col;
                    while (start > 0 && GetCharClass(s, start - 1) == CharClass.Ident)
                        start--;
                    while (col < s.Length - 1 &&
                        GetCharClass(s, col + 1) == CharClass.Ident)
                        col++;
                    from = new Position(current.line, start);
                    current = new Position(current.line, col + 1);
                    return s.Substring(start, col - start + 1);
                }
            }
            return string.Empty;
        }

        /// <summary>Gets the identifier at the given coordinates.</summary>
        /// <param name="position">Position to check.</param>
        /// <returns>The identifier, if any; otherwise, an empty string.</returns>
        string ICodeModel.GetIdentifier(CodeEditor.Position position)
        {
            if (position.line < 0 || position.line >= lineCount)
                return null;
            string s = this[position.line];
            if (position.column < 0 || position.column >= s.Length ||
                GetCharClass(s, position.column) != CharClass.Ident)
                return null;
            int start = position.column;
            while (start > 0 && GetCharClass(s, start - 1) == CharClass.Ident)
                start--;
            if (!char.IsLetter(s[start]))
                return null;
            int last = position.column;
            while (last < s.Length - 1 && GetCharClass(s, last + 1) == CharClass.Ident)
                last++;
            return s.Substring(start, last - start + 1);
        }

        string ICodeModel.GetLineSufix(CodeEditor.Position position)
        {
            if (position.line < 0 || position.line >= lineCount)
                return null;
            else
            {
                string s = this[position.line];
                if (position.column < 0 || position.column >= s.Length)
                    return null;
                int start = position.column;
                if (GetCharClass(s, start) == CharClass.Ident)
                {
                    while (start > 0 && GetCharClass(s, start - 1) == CharClass.Ident)
                        start--;
                }
                if (start == 0)
                    return s;
                else
                    return s.Remove(0, start);
            }
        }

        #endregion

        #region Information retrieval.

        /// <summary>Returns a line given its index.</summary>
        /// <param name="index">Zero based line position.</param>
        /// <returns>The corresponding line.</returns>
        public string this[int index]
        {
            get
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < lineCount);
                return lines[index <= current.line ?
                    index : capacity - lineCount + index].Line;
            }
            protected set
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < lineCount);
                if (index > current.line)
                    index += capacity - lineCount;
                lines[index] = new Header(value, lines[index].State);
            }
        }

        bool ICodeModel.IsPreviousText(string text, out int indent)
        {
            Header hdr = lines[current.line];
            if (hdr.State != LineState.Inside && hdr.Line[..current.column]
                .EndsWith(text, StringComparison.CurrentCultureIgnoreCase))
            {
                indent = hdr.Indent;
                return true;
            }
            indent = 0;
            return false;
        }

        int ICodeModel.GetIndentation(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= lineCount)
                return 0;
            else
                return lines[lineNumber <= current.line ?
                    lineNumber : capacity - lineCount + lineNumber].Indent;
        }

        string ICodeModel.GetFirstToken(int lineNumber, out int indent)
        {
            if (lineNumber < 0 || lineNumber >= lineCount)
            {
                indent = 0;
                return string.Empty;
            }
            string line = lines[lineNumber <= current.line ?
                lineNumber : capacity - lineCount + lineNumber].Line;
            int i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;
            indent = i;
            if (i >= line.Length)
                return string.Empty;
            if (char.IsLetterOrDigit(line[i]) || line[i] == '_')
                do i++;
                while (i < line.Length &&
                    (char.IsLetterOrDigit(line[i]) || line[i] == '_'));
            else
                do i++;
                while (i < line.Length &&
                    !char.IsLetterOrDigit(line[i]) && line[i] != '_');
            return line[indent..i];
        }

        private LineState GetState(int lineNumber)
        {
            return lines[lineNumber <= current.line ?
                lineNumber : capacity - lineCount + lineNumber].State;
        }

        /// <summary>Number of lines in the document.</summary>
        int ICodeModel.LineCount { get { return lineCount; } }

        /// <summary>Coordinates of the insertion point.</summary>
        public Position Current
        {
            get { return current; }
            set
            {
                if (value.line < 0)
                    value.line = 0;
                else if (value.line >= lineCount)
                    value.line = lineCount - 1;
                if (current.line != value.line)
                {
                    int top = capacity - lineCount + current.line;
                    if (value.line < current.line)
                        do
                        {
                            lines[top] = lines[current.line];
                            current.line--;
                            top--;
                        }
                        while (value.line < current.line);
                    else
                        do
                        {
                            current.line++;
                            top++;
                            lines[current.line] = lines[top];
                        }
                        while (value.line > current.line);
                }
                if (value.column < 0)
                    current.column = 0;
                else
                {
                    int max = lines[current.line].Length;
                    current.column = value.column > max ? max : value.column;
                }
            }
        }

        /// <summary>Coordinates of the selection start.</summary>
        public Position From { get { return from; } }

        int ICodeModel.RangeWidth(int fromLine, int toLine)
        {
            int result = 1;
            if (toLine >= lineCount)
                toLine = lineCount - 1;
            while (fromLine <= toLine)
            {
                int w = this[fromLine].Length;
                if (w > result)
                    result = w;
                fromLine++;
            }
            return result;
        }

        /// <summary>Number of spaces when expanding a tabulation.</summary>
        int ICodeModel.TabLength
        {
            get { return tabLength; }
            set { tabLength = value; }
        }

        /// <summary>Activates the smart indentation mode.</summary>
        bool ICodeModel.SmartIndentation
        {
            get { return smartIndentation; }
            set { smartIndentation = value; }
        }

        /// <summary>Can we pack commands in the undo/redo lists?</summary>
        bool ICodeModel.MergeUndoCommands
        {
            get { return mergeUndoCommands; }
            set { mergeUndoCommands = value; }
        }

        #endregion

        #region Bulk text transformations.

        /// <summary>Adds or removes line comments from the selected text.</summary>
        /// <param name="comment">True for adding, false for removing.</param>
        void ICodeModel.CommentSelection(bool comment)
        {
            if (lineCount == 0)
                return;
            string lineComment = scanner.LineComment;
            if (string.IsNullOrEmpty(lineComment))
                return;
            if (!RememberSelection())
            {
                // No selection has been found: we'll select the whole current line.
                int len = lines[current.line].Length;
                if (len == 0)
                    return;
                old0 = new Position(current.line, 0);
                old1 = new Position(current.line, len);
            }
            // Avoids indentation of the line following a selection.
            if (old1.column == 0)
            {
                old1.line--;
                if (old1.line < old0.line) return;
                old1.column = this[old1.line].Length;
            }
            int margin = int.MaxValue;
            bool changed = false;
            BaseCommentAction action = comment ? new CommentAction(lineComment) :
                (BaseCommentAction)new UncommentAction(lineComment);
            for (int i = old0.line; i <= old1.line; i++)
            {
                string s = this[i];
                if (!string.IsNullOrEmpty(s.TrimEnd(trimChars)))
                {
                    int first = 0;
                    for (int j = 0; j < s.Length; j++)
                        if (!char.IsWhiteSpace(s[j]))
                        {
                            first = j;
                            break;
                        }
                    if (comment)
                    {
                        if (first < margin) margin = first;
                    }
                    else if (s[first..].StartsWith(lineComment))
                    {
                        this[i] = s.Remove(first, lineComment.Length);
                        action.Add(new Position(i, first));
                        changed = true;
                    }
                }
            }
            if (!comment)
            {
                if (changed) Modify(action);
            }
            else if (margin < int.MaxValue)
            {
                for (int i = old0.line; i <= old1.line; i++)
                {
                    string s = this[i];
                    if (!string.IsNullOrEmpty(s.TrimEnd(trimChars)) &&
                        !s.TrimStart(trimChars).StartsWith(lineComment))
                    {
                        this[i] = s.Insert(margin, lineComment);
                        action.Add(new Position(i, margin));
                    }
                }
                Modify(action);
            }
            from.line = old0.line;
            from.column = 0;
            old1.column = int.MaxValue;
            Current = old1;
            view.Redraw(old0.line, old1.line);
        }

        private static string Transform(string s, bool upper)
        {
            return upper ? s.ToUpperInvariant() : s.ToLowerInvariant();
        }

        /// <summary>Change letter case for the selected text.</summary>
        /// <param name="upper">When true, change text to uppercase.</param>
        void ICodeModel.ChangeCase(bool upper)
        {
            if (lineCount == 0 || !RememberSelection())
                return;
            UndoAction action = new CaseChangeAction(
                old0, old1, TextRange(old0, old1), upper);
            if (old0.line == old1.line)
            {
                string s = this[old0.line];
                this[old0.line] = s[..old0.column] +
                    Transform(s[old0.column..old1.column], upper) +
                    s[old1.column..];
            }
            else
                for (int i = old0.line; i <= old1.line; i++)
                {
                    string s = this[i];
                    if (i == old0.line)
                        this[i] = string.Concat(s.AsSpan(0, old0.column), Transform(s[old0.column..], upper));
                    else if (i == old1.line)
                        this[i] = Transform(s[..old1.column], upper) +
                            s[old1.column..];
                    else
                        this[i] = Transform(s, upper);
                }
            Modify(action);
            view.Redraw(old0.line, old1.line);
        }

        #endregion

        #region Snippets.

        private const string SnippetCursor = "$end$";
        private const string SnippetSelect = "$selected$";

        private static IEnumerable<string> SnippetTokens(string snippet)
        {
            int flushFrom = 0;
            while (flushFrom < snippet.Length)
            {
                int p = snippet.IndexOf('$', flushFrom);
                if (p == -1 || p == snippet.Length - 1)
                {
                    yield return snippet[flushFrom..];
                    break;
                }
                int q = snippet.IndexOf('$', p + 1);
                if (q == -1)
                {
                    yield return snippet[flushFrom..];
                    break;
                }
                yield return snippet[flushFrom..p];
                string token = snippet.Substring(p, q - p + 1);
                if (string.Compare(token, SnippetCursor, true) == 0)
                    yield return SnippetCursor;
                else if (string.Compare(token, SnippetSelect, true) == 0)
                    yield return SnippetSelect;
                else if (token == "$$")
                    yield return "$";
                flushFrom = q + 1;
            }
        }

        private static string AddIndent(string text, int indent)
        {
            if (indent <= 0 || text.Length == 0)
                return text;
            string indentStr = new(' ', indent);
            string[] split = text.Split(lineSeparators, StringSplitOptions.None);
            for (int i = 1; i < split.Length; i++)
                split[i] = indentStr + split[i];
            return string.Join(CR_LF, split);
        }

        void ICodeModel.ExpandSnippet(ICodeSnippet snippet, bool isSurround)
        {
            string surroundText = string.Empty, savedText = string.Empty;
            if (RememberSelection())
            {
                savedText = TextRange(old0, old1);
                if (isSurround)
                    surroundText = savedText;
                InternalDelete(old0, old1);
                view.Redraw();
            }
            int indent = current.column;
            string snippetText = AddIndent(snippet.Text, indent);
            Position savedPosition = Position.Infinite;
            foreach (string s in SnippetTokens(snippetText))
                if (s == SnippetSelect)
                    InsertRange(current, AddIndent(surroundText, current.column - indent));
                else if (s == SnippetCursor)
                    savedPosition = current;
                else
                    InsertRange(current, s);
            Modify(new SnippetAction(old0, current, savedText));
            if (savedPosition.line != int.MaxValue)
                Current = savedPosition;
            RecalculateLineStates(old0.line, lineCount - 1);
            Redraw(false);
        }

        void ICodeModel.SetScanner(ICodeScanner scanner)
        {
            this.scanner = scanner ?? defaultScanner;
        }

        #endregion

        #region Low level transformations.

        /// <summary>Source text between two positions.</summary>
        /// <param name="start">Initial position.</param>
        /// <param name="end">Final position.</param>
        /// <returns>Text in between.</returns>
        public string TextRange(Position start, Position end)
        {
            if (lineCount == 0)
                return string.Empty;
            else if (start.line == end.line)
                return start.column < end.column
                    ? this[start.line][start.column..end.column]
                    : this[start.line][end.column..start.column];
            else
            {
                if (start.line > end.line)
                {
                    (end, start) = (start, end);
                }
                StringBuilder sb = new();
                string line = this[start.line];
                sb.Append(line, start.column, line.Length - start.column).Append(CR_LF);
                for (int i = start.line + 1; i < end.line; i++)
                    sb.Append(this[i]).Append(CR_LF);
                return sb.Append(this[end.line], 0, end.column).ToString();
            }
        }

        /// <summary>Low level text insertion.</summary>
        /// <param name="start">Insertion coordinates.</param>
        /// <param name="text">Text to insert.</param>
        /// <remarks>
        /// <c>InsertRange</c> neither touches the <c>Modified</c> flag,
        /// nor adds actions to the Undo list.
        /// </remarks>
        public int InsertRange(Position start, string text)
        {
            if (lineCount == 0)
            {
                lineCount = 1;
                lines[0] = Header.Empty;
                view.LineCountChanged();
            }
            Current = start;
            from = current;
            string remainder = string.Empty;
            string s = lines[current.line].Line;
            if (current.column < s.Length)
            {
                remainder = s[current.column..];
                lines[current.line].Line = s.Remove(current.column);
            }
            string[] splitLines = text.Split(lineSeparators, StringSplitOptions.None);
            for (int i = 0; i < splitLines.Length; i++)
            {
                string line = splitLines[i];
                lines[current.line].Line += line;
                if (i < splitLines.Length - 1)
                {
                    CheckCapacity();
                    lineCount++;
                    current.line++;
                    lines[current.line].Line = string.Empty;
                    view.LineCountChanged();
                }
                current.column = lines[current.line].Length;
            }
            if (!string.IsNullOrEmpty(remainder))
                lines[current.line].Line += remainder;
            int firstLine = from.line;
            from = current;
            LinesInserted(firstLine, splitLines.Length - 1);
            view.Redraw(firstLine, int.MaxValue);
            return firstLine;
        }

        private void InternalDelete(Position start, Position end)
        {
            Current = end;
            int drop = end.line - start.line - 1;
            if (drop > 0)
            {
                lines[start.line + 1] = lines[end.line];
                lineCount -= drop;
                end.line = start.line + 1;
            }
            else
                drop = 0;
            string s0 = lines[start.line].Line;
            if (start.line == end.line)
            {
                if (start.column < s0.Length)
                {
                    int count = end.column;
                    if (s0.Length < count) count = s0.Length;
                    lines[start.line].Line = s0.Remove(start.column, count - start.column);
                }
            }
            else
            {
                if (start.column < s0.Length)
                    s0 = s0.Remove(start.column);
                string s1 = lines[end.line].Line;
                if (end.column > 0)
                    s1 = end.column >= s1.Length ? string.Empty : s1.Remove(0, end.column);
                lines[start.line].Line = s0 + s1;
                lineCount--;
                drop++;
            }
            LinesDeleted(start.line, drop, start.column == 0);
            current = from = start;
            view.LineCountChanged();
        }

        void ICodeModel.DeleteRange(Position start, Position end)
        {
            if (lineCount > 0)
            {
                if (start.IsLesserEqual(end))
                    InternalDelete(start, end);
                else
                    InternalDelete(end, start);
                view.Redraw();
            }
        }

        void ICodeModel.IndentRange(Position start, Position end,
            int amount, params int[] exceptions)
        {
            if (amount > 0)
            {
                RememberSelection();
                int ln0, ln1;
                if (start.line <= end.line) { ln0 = start.line; ln1 = end.line; }
                else { ln0 = end.line; ln1 = start.line; }
                if (ln0 < 0) ln0 = 0;
                if (ln1 >= lineCount) ln1 = lineCount - 1;
                string text = new(' ', amount);
                for (int i = ln0; i <= ln1; i++)
                    if (exceptions == null || Array.IndexOf<int>(exceptions, i) == -1)
                    {
                        string s = this[i];
                        if (!string.IsNullOrEmpty(s))
                            this[i] = text + s;
                    }
                if (old0.line < ln0) ln0 = old0.line;
                if (old1.line > ln1) ln1 = old1.line;
                from = new Position(start.line, start.column + amount);
                Current = new Position(end.line, end.column + amount);
                view.Redraw(ln0, ln1);
            }
        }

        int[] ICodeModel.UnindentRange(Position start, Position end, int amount)
        {
            if (amount > 0)
            {
                RememberSelection();
                int ln0, ln1;
                if (start.line <= end.line) { ln0 = start.line; ln1 = end.line; }
                else { ln0 = end.line; ln1 = start.line; }
                if (ln0 < 0) ln0 = 0;
                if (ln1 >= lineCount) ln1 = lineCount - 1;
                List<int> exceptions = null;
                for (int i = ln0; i <= ln1; i++)
                {
                    string line = this[i];
                    if (line.Length == 0)
                    {
                        exceptions ??= new List<int>();
                        exceptions.Add(i);
                    }
                    else
                    {
                        int indent = 0;
                        while (indent < line.Length && indent < amount &&
                            char.IsWhiteSpace(line[indent]))
                            indent++;
                        if (indent > 0)
                            this[i] = line.Remove(0, indent);
                    }
                }
                if (old0.line < ln0) ln0 = old0.line;
                if (old1.line > ln1) ln1 = old1.line;
                from = new Position(start.line, start.column - amount);
                Current = new Position(end.line, end.column - amount);
                view.Redraw(ln0, ln1);
                if (exceptions != null)
                    return exceptions.ToArray();
            }
            return null;
        }

        #endregion

        #region Find and replace.

        bool ICodeModel.FindText(string text,
            Position start, bool wholeWords, bool matchCase)
        {
            RememberSelection();
            if (!matchCase)
                text = text.ToLowerInvariant();
            for (int i = start.line; i < lineCount; i++)
            {
                string s = this[i];
                if (!matchCase)
                    s = s.ToLowerInvariant();
                for (int j = (i != start.line ? 0 : start.column); j < s.Length; j++)
                {
                    int pos = s.IndexOf(text, j);
                    if (pos == -1)
                        break;
                    if (wholeWords)
                        if (pos > 0 && char.IsLetterOrDigit(s, pos - 1))
                            pos = -1;
                        else
                        {
                            int p = pos + text.Length;
                            if (p < s.Length && char.IsLetterOrDigit(s, p))
                                pos = -1;
                        }
                    if (pos != -1)
                    {
                        from = new Position(i, pos);
                        Current = new Position(i, pos + text.Length);
                        Redraw(true);
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        /// <summary>
        /// Store information about the current selection in private fields.
        /// </summary>
        /// <returns>True, when there was a non empty selection.</returns>
        private bool RememberSelection()
        {
            if (from.IsLesserEqual(current))
            {
                old0 = from;
                old1 = current;
                return old0.NotEquals(old1);
            }
            else
            {
                old0 = current;
                old1 = from;
                return true;
            }
        }

        /// <summary>
        /// Commands the code view to invalidate and draw part of the viewport.
        /// </summary>
        /// <param name="extend">Are we extending the selection?</param>
        private void Redraw(bool extend)
        {
            if (!extend)
                from = current;
            if (old0.Equals(old1) && from.Equals(current))
                // No selections involved.
                view.Redraw(0, -1);
            else if (extend && old0.NotEquals(old1))
                // A selection existed and it has been extended.
                if (old0.Equals(from))
                    view.Redraw(Math.Min(old1.line, current.line),
                        Math.Max(old1.line, current.line));
                else
                    view.Redraw(Math.Min(old0.line, current.line),
                        Math.Max(old0.line, current.line));
            else if (from.IsLesserEqual(current))
                view.Redraw(
                    Math.Min(old0.line, from.line), Math.Max(old1.line, current.line));
            else
                view.Redraw(
                    Math.Min(old0.line, current.line), Math.Max(old1.line, from.line));
        }

        private void CheckCapacity()
        {
            if (capacity - lineCount < 4)
            {
                Header[] newLines = new Header[capacity + 256];
                Array.Copy(lines, newLines, current.line + 1);
                int delta = lineCount - current.line - 1;
                if (delta > 0)
                    Array.Copy(lines, capacity - delta,
                        newLines, newLines.Length - delta, delta);
                lines = newLines;
                capacity = lines.Length;
            }
        }

        #region Brace matching.

        /// <summary>Finds a matching left parenthesis, bracket or brace.</summary>
        /// <param name="openChar">Left character.</param>
        /// <param name="closeChar">Right character.</param>
        /// <returns>True if succeeds.</returns>
        /// <remarks>The matching position is stored at <c>this.braceStart</c>.</remarks>
        private bool PreviousMatch(char openChar, char closeChar)
        {
            bool comment = false;
            switch (lines[current.line].State)
            {
                case LineState.Inside:
                case LineState.Error:
                    return false;
                case LineState.Close:
                    comment = true;
                    break;
            }
            Position p = current;
            p.column--;
            IEnumerator<Lexeme> tokenizer =
                scanner.Tokens(lines[p.line].Line, comment).GetEnumerator();
            StringBuilder sb = new();
        STATE0:
            if (!tokenizer.MoveNext())
                return false;
            Lexeme lex = tokenizer.Current;
            switch (lex.Kind)
            {
                case Lexeme.Token.PartialComment:
                    return false;
                case Lexeme.Token.Comment:
                case Lexeme.Token.String:
                    if (lex.EndColumn > p.column)
                        return false;
                    sb.Append(' ', lex.Text.Length);
                    goto STATE0;
                default:
                    sb.Append(lex.Text);
                    if (lex.EndColumn <= p.column)
                        goto STATE0;
                    break;
            }
            // We are not inside a string or comment.
            int nesting = 0;
            string s = sb.ToString();
            if (p.column >= s.Length)
                p.column = s.Length - 1;
        STATE1:
            while (p.column >= 0)
            {
                char ch = s[p.column--];
                if (ch == closeChar)
                    nesting++;
                else if (ch == openChar)
                {
                    if (--nesting == 0)
                    {
                        braceLeft = new Position(p.line, p.column + 1);
                        braceRight = new Position(current.line, current.column - 1);
                        return true;
                    }
                }
            }
            if (--p.line < 0)
                return false;
            LineState state = GetState(p.line);
            sb.Length = 0;
            foreach (Lexeme lex1 in scanner.Tokens(this[p.line],
                state == LineState.Inside || state == LineState.Close))
                switch (lex1.Kind)
                {
                    case Lexeme.Token.PartialComment:
                    case Lexeme.Token.NewLine:
                        break;
                    case Lexeme.Token.Comment:
                    case Lexeme.Token.String:
                        sb.Append(' ', lex1.Text.Length);
                        break;
                    default:
                        sb.Append(lex1.Text);
                        break;
                }
            s = sb.ToString();
            p.column = s.Length - 1;
            goto STATE1;
        }

        /// <summary>Finds a matching right parenthesis, bracket or brace.</summary>
        /// <param name="openChar">Left character.</param>
        /// <param name="closeChar">Right character.</param>
        /// <returns>True if succeeds.</returns>
        /// <remarks>The matching position is stored at <c>this.braceEnd</c>.</remarks>
        private bool NextMatch(char openChar, char closeChar)
        {
            bool comment = false;
            switch (lines[current.line].State)
            {
                case LineState.Inside:
                case LineState.Error:
                    return false;
                case LineState.Close:
                    comment = true;
                    break;
            }
            Position p = current;
            IEnumerator<Lexeme> tokenizer =
                scanner.Tokens(lines[p.line].Line, comment).GetEnumerator();
        STATE0:
            if (!tokenizer.MoveNext())
                return false;
            Lexeme lex = tokenizer.Current;
            switch (lex.Kind)
            {
                case Lexeme.Token.PartialComment:
                    return false;
                case Lexeme.Token.Comment:
                case Lexeme.Token.String:
                    if (lex.EndColumn > p.column)
                        return false;
                    goto STATE0;
                default:
                    if (lex.EndColumn <= p.column)
                        goto STATE0;
                    break;
            }
            p.column = p.column - lex.Column + 1;
            int nesting = 1;
        STATE1:
            for (int index = p.column; index < lex.Text.Length; )
            {
                char ch = lex.Text[index++];
                if (ch == openChar)
                    nesting++;
                else if (ch == closeChar)
                    if (--nesting == 0)
                    {
                        braceLeft = new Position(current.line, current.column);
                        braceRight = new Position(p.line, lex.Column + index - 1);
                        return true;
                    }
            }
            p.column = 0;
        STATE2:
            if (tokenizer.MoveNext())
                goto STATE4;
        STATE3:
            p.line++;
            if (p.line >= lineCount)
                return false;
            LineState state = GetState(p.line);
            tokenizer = scanner.Tokens(this[p.line],
                state == LineState.Inside || state == LineState.Close).GetEnumerator();
            goto STATE2;
        STATE4:
            lex = tokenizer.Current;
            switch (lex.Kind)
            {
                case Lexeme.Token.PartialComment:
                case Lexeme.Token.NewLine:
                    goto STATE3;
                case Lexeme.Token.Comment:
                case Lexeme.Token.Keyword:
                case Lexeme.Token.String:
                    goto STATE2;
                default:
                    goto STATE1;
            }
        }

        /// <summary>Move the insertion point to the matching brace.</summary>
        /// <param name="extend">When true, current selection is extended.</param>
        bool ICodeModel.MoveToBrace(bool extend)
        {
            if (CheckBraces())
            {
                if (current.Equals(braceLeft))
                {
                    MoveTo(braceRight, extend);
                    ((ICodeModel)this).MoveRight(false, extend);
                }
                else
                    MoveTo(braceLeft, extend);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds the matching parenthesis, bracket or brace, according to
        /// the current insertion point.
        /// </summary>
        /// <returns>True if succeeds.</returns>
        private bool CheckBraces()
        {
            if (lineCount > 0)
            {
                string s = lines[current.line].Line;
                if (current.column < s.Length)
                    switch (s[current.column])
                    {
                        case '(':
                            if (NextMatch('(', ')'))
                            {
                                view.MatchParentheses("(", ")", braceLeft, braceRight);
                                return true;
                            }
                            break;
                        case '[':
                            if (NextMatch('[', ']'))
                            {
                                view.MatchParentheses("[", "]", braceLeft, braceRight);
                                return true;
                            }
                            break;
                    }
                if (current.column > 0)
                    switch (s[current.column - 1])
                    {
                        case ')':
                            if (PreviousMatch('(', ')'))
                            {
                                view.MatchParentheses("(", ")", braceLeft, braceRight);
                                return true;
                            }
                            break;
                        case ']':
                            if (PreviousMatch('[', ']'))
                            {
                                view.MatchParentheses("[", "]", braceLeft, braceRight);
                                return true;
                            }
                            break;
                    }
            }
            view.MatchNoParentheses();
            return false;
        }

        #endregion

        #region ICodeModel navigation.

        /// <summary>Move the insertion point one character right.</summary>
        /// <param name="word">When true, a whole word is moved, instead.</param>
        /// <param name="extend">When true, the current selection is extended.</param>
        void ICodeModel.MoveRight(bool word, bool extend)
        {
            RememberSelection();
            if (word)
            {
                // Move a word to the right.
                string s = lines[current.line].Line;
                switch (GetCharClass(s, current.column))
                {
                    case CharClass.Ident:
                        do { current.column++; }
                        while (GetCharClass(s, current.column) == CharClass.Ident);
                        break;
                    case CharClass.Other:
                        current.column++;
                        break;
                }
                if (GetCharClass(s, current.column) == CharClass.Blank)
                    while (true)
                        if (current.column >= s.Length)
                            if (current.line + 1 >= lineCount)
                                break;
                            else
                            {
                                Current = new Position(current.line + 1, 0);
                                s = lines[current.line].Line;
                            }
                        else if (char.IsWhiteSpace(s, current.column))
                            current.column++;
                        else
                            break;
            }
            // Move one character right.
            else if (current.column < lines[current.line].Length)
                current.column++;
            else if (current.line < lineCount - 1)
                Current = new Position(current.line + 1, 0);
            else
                return;
            Redraw(extend);
        }

        /// <summary>Move the insertion point one character to the left.</summary>
        /// <param name="word">When true, a whole word is moved, instead.</param>
        /// <param name="extend">When true, the current selection is extended.</param>
        void ICodeModel.MoveLeft(bool word, bool extend)
        {
            RememberSelection();
            if (word)
            {
                if (current.column == 0)
                    if (current.line > 0)
                        Current = new Position(current.line - 1, int.MaxValue);
                    else
                    {
                        Redraw(extend);
                        return;
                    }
                string s = lines[current.line].Line;
                while (current.column > 0 && char.IsWhiteSpace(s, current.column - 1))
                    current.column--;
                if (current.column > 0)
                    if (GetCharClass(s, current.column - 1) == CharClass.Ident)
                        do { current.column--; }
                        while (current.column > 0 &&
                            GetCharClass(s, current.column - 1) == CharClass.Ident);
                    else
                        current.column--;
            }
            // Move one character left.
            else if (current.column > 0)
                current.column--;
            else if (current.line > 0)
                Current = new Position(current.line - 1, int.MaxValue);
            else
                return;
            Redraw(extend);
        }

        /// <summary>Move the insertion point one line up.</summary>
        /// <param name="extend">When true, the current selection is extended.</param>
        void ICodeModel.MoveUp(bool extend)
        {
            RememberSelection();
            if (current.line > 0)
            {
                Current = current.ChangeLine(current.line - 1);
                Redraw(extend);
            }
        }

        /// <summary>Move the insertion point one line down.</summary>
        /// <param name="extend">When true, this method respects selection start.</param>
        /// <remarks>MoveDown tries not to change the first visible line.</remarks>
        void ICodeModel.MoveDown(bool extend)
        {
            RememberSelection();
            if (current.line + 1 < lineCount)
            {
                Current = current.ChangeLine(current.line + 1);
                Redraw(extend);
            }
        }

        /// <summary>Move the caret to the beginning of the line.</summary>
        /// <param name="extend">When true, this method respects selection start.</param>
        void ICodeModel.MoveHome(bool extend)
        {
            RememberSelection();
            int first = lines[current.line].Indent;
            current.column = current.column != first ? first : 0;
            Redraw(extend);
        }

        /// <summary>Move the caret to the end of the line.</summary>
        /// <param name="extend">When true, this method respects selection start.</param>
        void ICodeModel.MoveEnd(bool extend)
        {
            RememberSelection();
            current.column = lines[current.line].Length;
            Redraw(extend);
        }

        private enum CharClass { Blank, Ident, Other }

        private static CharClass GetCharClass(string line, int position)
        {
            if (position >= line.Length)
                return CharClass.Blank;
            else
            {
                char ch = line[position];
                if (char.IsWhiteSpace(ch))
                    return CharClass.Blank;
                else if (char.IsLetterOrDigit(ch) || ch == '_')
                    return CharClass.Ident;
                else
                    return CharClass.Other;
            }
        }

        void ICodeModel.MovePageUp(ref int topLine, int linesInPage, bool extend)
        {
            if (current.line < linesInPage)
            {
                Current = new Position(0, current.column);
                topLine = 0;
            }
            else
            {
                int delta = linesInPage - 1;
                if (topLine < delta)
                    delta = topLine;
                topLine -= delta;
                Current = current.ChangeLine(current.line - delta);
            }
            if (!extend)
                from = current;
            view.LineCountChanged();
            view.Redraw();
        }

        void ICodeModel.MovePageDown(ref int topLine, int linesInPage, bool extend)
        {
            int delta = linesInPage - 1;
            if (topLine + delta > lineCount - 1)
                delta = lineCount - topLine - 1;
            topLine += delta;
            int lineNo = current.line + delta;
            if (lineNo >= lineCount)
                lineNo = lineCount - 1;
            Current = current.ChangeLine(lineNo);
            if (!extend)
                from = current;
            view.LineCountChanged();
            view.Redraw();
        }

        /// <summary>Move the insertion point to the given position.</summary>
        /// <param name="position">The final position of the insertion point.</param>
        /// <param name="extend">When true, the selection is extended.</param>
        public void MoveTo(Position position, bool extend)
        {
            if (lineCount > 0)
            {
                RememberSelection();
                if (current.NotEquals(position) || !extend && from.NotEquals(current))
                {
                    Current = position;
                    Redraw(extend);
                }
            }
        }

        void ICodeModel.ScrollTo(ref int topLine, int newTopLine, int linesInPage)
        {
            if (newTopLine >= 0 && newTopLine < lineCount)
            {
                topLine = newTopLine;
                Position pos = current;
                if (pos.line < topLine)
                {
                    pos.line = topLine;
                    Current = pos;
                    from = pos;
                }
                else if (pos.line - topLine >= linesInPage - 1)
                {
                    pos.line = topLine + linesInPage - 2;
                    Current = pos;
                    from = pos;
                }
                view.LineCountChanged();
                view.Redraw();
            }
        }

        /// <summary>Watches changes in current position and current timestamp.</summary>
        /// <returns>An IDisposable instance.</returns>
        IDisposable ICodeModel.WrapOperation()
        {
            if (watchLevel++ == 0)
            {
                watchOriginalFrom = from;
                watchOriginalTo = current;
                watchTimestamp = timestamp;
            }
            return wrapper;
        }

        private void CleanWrapper()
        {
            if (--watchLevel == 0)
            {
                bool posChanged = false;
                if (watchOriginalFrom.NotEquals(from) ||
                    watchOriginalTo.NotEquals(current))
                {
                    watchEventArgs.Reset(watchOriginalFrom, watchOriginalTo);
                    view.SelectionChanged(watchEventArgs);
                    posChanged = true;
                }
                if (posChanged || watchTimestamp != timestamp)
                    CheckBraces();
            }
        }

        private sealed class OperationWrapper : IDisposable
        {
            private readonly CodeModel model;

            public OperationWrapper(CodeModel model)
            {
                this.model = model;
            }

            public void Dispose()
            {
                model.CleanWrapper();
            }
        }

        #endregion

        #region Modification commands.

        /// <summary>Inserts a printable character at current position.</summary>
        /// <param name="ch">The printable character to insert.</param>
        void ICodeModel.Add(char ch)
        {
            bool redrawFromCurrent = DeleteSelection(false) == DeleteResult.MultiLine;
            InsertTextInLine(ch.ToString());
            if (scanner.IsCommentCharacter(ch))
                if (RecalculateLineStates(current.line, lineCount - 1))
                    redrawFromCurrent = true;
            view.Redraw(current.line, redrawFromCurrent ? int.MaxValue : current.line);
        }

        /// <summary>Unindents a multiline selection.</summary>
        void ICodeModel.ShiftTab()
        {
            // Check if this is an indentation request.
            if (lineCount > 0)
                ((ICodeModel)this).Outdent();
        }

        /// <summary>Inserts a tabulation at the insertion point.</summary>
        void ICodeModel.Tab()
        {
            // Check if it's an indentation request.
            if (lineCount > 0 && from.line != current.line)
            {
                ((ICodeModel)this).Indent();
                return;
            }
            // Find a previous non empty line, if any.
            int tabSize = tabLength;
            for (int i = current.line - 1; i >= 0; i--)
            {
                string line = lines[i].Line;
                int margin = 0;
                for (int j = 0; j < line.Length; j++)
                    if (!char.IsWhiteSpace(line[j]))
                    {
                        margin = j;
                        break;
                    }
                if (margin > 0 ||
                    line.Length > 0 && !char.IsWhiteSpace(line[0]))
                {
                    tabSize = margin - current.column;
                    break;
                }
            }
            // Insert the required number of blanks.
            bool redrawFromCurrent = DeleteSelection(false) == DeleteResult.MultiLine;
            if (tabSize <= 0) tabSize = tabLength;
            InsertTextInLine(new string(' ', tabSize));
            view.Redraw(current.line, redrawFromCurrent ? int.MaxValue : current.line);
        }

        /// <summary>Inserts text at the insertion point.</summary>
        /// <param name="text">Text to insert, containing no new lines.</param>
        private void InsertTextInLine(string text)
        {
            if (lineCount == 0)
            {
                lines[0] = new Header(text);
                lineCount = 1;
                current.column = text.Length;
                view.LineCountChanged();
            }
            else
            {
                string s = lines[current.line].Line;
                lines[current.line].Line = current.column == s.Length ?
                    s + text : s.Insert(current.column, text);
                current.column += text.Length;
            }
            Modify(new TypeAction(from, current));
            from = current;
        }

        /// <summary>Adds indentation to the selected block/current line.</summary>
        void ICodeModel.Indent()
        {
            if (from.IsLesserThan(current) && current.column == 0 && current.line > 0)
                MoveTo(new(current.line - 1, int.MaxValue), true);
            else if (from.IsGreaterThan(current) && from.column == 0)
                from = new(from.line - 1, this[from.line - 1].Length + 1);
            ((ICodeModel)this).IndentRange(from, current, tabLength, null);
            Modify(new IndentAction(from, current, tabLength));
        }

        /// <summary>Removes indentation from the selected block/current line.</summary>
        void ICodeModel.Outdent()
        {
            int ln0, ln1;
            if (from.line <= current.line) { ln0 = from.line; ln1 = current.line; }
            else { ln0 = current.line; ln1 = from.line; }
            int amount = int.MaxValue;
            while (ln0 <= ln1)
            {
                string line = this[ln0++];
                if (line.Length > 0)
                {
                    int indent = 0;
                    while (indent < line.Length && indent < tabLength &&
                        char.IsWhiteSpace(line[indent])) indent++;
                    amount = Math.Min(amount, indent);
                }
            }
            if (amount > tabLength)
                amount = tabLength;
            if (amount > 0)
            {
                int[] exceptions = ((ICodeModel)this).UnindentRange(from, current, tabLength);
                Modify(new UnindentAction(from, current, tabLength, exceptions));
            }
        }

        private enum DeleteResult { None, SingleLine, MultiLine }

        /// <summary>Deletes the current text selection.</summary>
        /// <param name="redraw">
        /// When true, this method redraws the control and marks it as modified.
        /// </param>
        /// <returns>A hint about how many lines were deleted.</returns>
        private DeleteResult DeleteSelection(bool redraw)
        {
            if (lineCount == 0 || !RememberSelection())
                return DeleteResult.None;
            else
            {
                string deleted = TextRange(old0, old1);
                Modify(new DeleteAction(old0, old1, deleted));
                InternalDelete(old0, old1);
                if (old0.line == old1.line &&
                    !scanner.ContainsCommentCharacters(deleted))
                {
                    if (redraw)
                        view.Redraw(current.line, current.line);
                    return DeleteResult.SingleLine;
                }
                else
                {
                    RecalculateLineStates(old0.line, lineCount - 1);
                    if (redraw)
                        view.Redraw(current.line, int.MaxValue);
                    return DeleteResult.MultiLine;
                }
            }
        }

        /// <summary>Inserts a new line at the current position.</summary>
        void ICodeModel.NewLine()
        {
            bool redrawPrevious = DeleteSelection(false) != DeleteResult.None;
            CheckCapacity();
            if (lineCount == 0)
            {
                lines[0] = Header.Empty;
                lineCount = 1;
            }
            string remainder = string.Empty;
            string s = lines[current.line].Line;
            if (current.column < s.Length)
            {
                remainder = s[current.column..];
                s = s.Remove(current.column);
                lines[current.line].Line = s;
                redrawPrevious = true;
            }
            // Smart indentation may add unneeded blanks in empty lines.
            // This fragment tries to delete some of them.
            if (s.Trim().Length == 0 && undoList.Count > 0)
                if (undoList.Peek().TryClearIndent(this))
                {
                    from.column = 0;
                    current.column = 0;
                    lines[current.line].Line = string.Empty;
                }
            int lastLineIndex = current.line, indent = 0;
            string lastLine = string.Empty;
            while (lastLineIndex >= 0)
            {
                Header hdr = lines[lastLineIndex];
                lastLine = hdr.Line.Trim();
                if (!string.IsNullOrEmpty(lastLine))
                {
                    indent = hdr.Indent;
                    break;
                }
                lastLineIndex--;
            }
            if (smartIndentation)
            {
                indent += tabLength * scanner.DeltaIndent(
                    view.Control, lastLine, lastLineIndex);
                remainder = remainder.TrimStart(trimChars);
            }
            current.line++;
            lineCount++;
            lines[current.line].Line = indent == 0 ?
                remainder : remainder.PadLeft(remainder.Length + indent);
            current.column = indent;
            Modify(new TypeAction(from, current));
            from = current;
            view.LineCountChanged();
            int redrawFrom = redrawPrevious ? current.line - 1 : current.line;
            LinesInserted(redrawFrom, 1);
            RecalculateLineStates(redrawFrom, lineCount - 1);
            view.Redraw(redrawFrom, int.MaxValue);
        }

        /// <summary>Deletes the character at the right side of the cursor.</summary>
        void ICodeModel.Delete()
        {
            // If there is a selection, we only delete the selected text.
            if (DeleteSelection(true) != DeleteResult.None)
                return;
            int first = current.line, to = first;
            string original = lines[first].Line;
            if (current.column < original.Length)
            {
                // The regular case: we're not after the last character of a line.
                Modify(new DeleteAction(
                    current, new Position(first, current.column + 1),
                    original.Substring(current.column, 1)));
                lines[first].Line = original.Remove(current.column, 1);
                if (scanner.IsCommentCharacter(original[current.column]))
                    if (RecalculateLineStates(first, lineCount - 1))
                        to = int.MaxValue;
            }
            else if (first < lineCount - 1)
            {
                // We must joint two lines by deleting a line separator.
                Modify(new DeleteAction(
                    current, new Position(first + 1, 0), CR_LF));
                string dropLine = this[first + 1];
                lineCount--;
                if (dropLine.Length > 0)
                    lines[first].Line += dropLine;
                else
                    first++;
                to = int.MaxValue;
                LinesDeleted(current.line, 1, current.column == 0);
                RecalculateLineStates(current.line, current.line);
                view.LineCountChanged();
            }
            else
                return;
            from = current;
            view.Redraw(first, to);
        }

        /// <summary>Deletes the character at the left side of the cursor.</summary>
        void ICodeModel.Backspace()
        {
            // When there's a selection, only the selection is deleted.
            if (DeleteSelection(true) != DeleteResult.None)
                return;
            // The regular case: we're not in the first character of a line.
            if (current.column > 0)
            {
                string original = lines[current.line].Line, deleted;
                if (current.column > 1 && current.line > 0 &&
                    lines[current.line].IsFirstChar(current.column))
                {
                    // We are at the very first non blank character of this line.
                    // We'll delete spaces to unindent this line.
                    int margin = 0;
                    for (int i = current.line; i > 0; )
                    {
                        string s = lines[--i].Line;
                        int max = s.Length;
                        if (max > current.column) max = current.column;
                        int j = 0;
                        while (j < max && char.IsWhiteSpace(s[j]))
                            j++;
                        if (j < max)
                        {
                            margin = j;
                            break;
                        }
                    }
                    deleted = original[margin..current.column];
                    lines[current.line].Line =
                        original.Remove(margin, current.column - margin);
                    current.column = margin;
                }
                else
                {
                    deleted = original.Substring(current.column - 1, 1);
                    lines[current.line].Line =
                        original.Remove(current.column - 1, 1);
                    current.column--;
                }
                Modify(new DeleteAction(current, from, deleted));
                from = current;
                if (scanner.ContainsCommentCharacters(deleted) &&
                    RecalculateLineStates(current.line, lineCount - 1))
                    view.Redraw(current.line, int.MaxValue);
                else
                    view.Redraw(current.line, current.line);
            }
            // Too bad: we have to delete a line feed (and join two lines).
            else if (current.line > 0)
            {
                string dropLine = lines[current.line].Line;
                current.column = lines[--current.line].Length;
                lineCount--;
                view.LineCountChanged();
                Modify(new DeleteAction(current, from, CR_LF));
                from = current;
                if (dropLine.Length > 0)
                {
                    lines[current.line].Line += dropLine;
                    LinesDeleted(current.line, 1, false);
                    view.Redraw(current.line, int.MaxValue);
                }
                else
                {
                    LinesDeleted(current.line + 1, 1, false);
                    view.Redraw(current.line + 1, int.MaxValue);
                }
            }
            else
                return;
        }

        void ICodeModel.DragSelection(Position position, bool copy)
        {
            if (RememberSelection())
                if (((ICodeModel)this).InsideSelection(position))
                {
                    Current = position;
                    Redraw(false);
                }
                else
                {
                    string text = TextRange(old0, old1);
                    if (copy)
                    {
                        // Inserts a copy of the selected text.
                        Current = position;
                        RecalculateLineStates(InsertRange(position, text), lineCount - 1);
                        Modify(new DragCopyAction(position, current));
                        if (position.IsGreaterEqual(old1))
                            view.Redraw(old0.line, old1.line);
                    }
                    else
                    {
                        Position end;
                        if (position.IsGreaterEqual(old1))
                        {
                            // The insertion won't change selection coordinates.
                            InsertRange(position, text);
                            end = current;
                            InternalDelete(old0, old1);
                            view.Redraw();
                            from = end;
                            Current = from.RangeDecrement(old0, old1);
                            // Update caret position.
                            view.Redraw(0, -1);
                            RecalculateLineStates(old0.line, lineCount - 1);
                        }
                        else
                        {
                            // The deletion won't affect selection coordinates.
                            InternalDelete(old0, old1);
                            view.Redraw();
                            int firstLine = InsertRange(position, text);
                            end = current;
                            RecalculateLineStates(firstLine, lineCount - 1);
                        }
                        Modify(new DragMoveAction(old0, old1, position, end, text));
                    }
                }
        }

        #endregion

        #region The Undo/Redo manager.

        public bool Modified
        {
            get { return modified; }
            set
            {
                if (modified != value)
                {
                    modified = value;
                    view.ModifiedChanged();
                }
                view.DocumentChanged();
                timestamp++;
            }
        }

        /// <summary>
        /// Adds a command to the undo list, clears the redo list
        /// and sets the Modified flag.
        /// </summary>
        /// <param name="action">The command to add to the redo list.</param>
        private void Modify(UndoAction action)
        {
            if (undoList.Count == 0 || !mergeUndoCommands ||
                !undoList.Peek().TryMerge(action))
                undoList.Push(action);
            redoList.Clear();
            Modified = true;
        }

        /// <summary>Reverts the effect of the last valid operation.</summary>
        void ICodeModel.Undo()
        {
            if (undoList.Count > 0)
            {
                UndoAction action = undoList.Pop();
                action.Undo(this);
                redoList.Push(action);
                RecalculateLineStates(
                    Math.Min(action.FirstLine, lineCount - 1), lineCount - 1);
                if (undoList.Count == 0)
                    Modified = false;
            }
        }

        /// <summary>Repeats the last undone operation.</summary>
        void ICodeModel.Redo()
        {
            if (redoList.Count > 0)
            {
                UndoAction action = redoList.Pop();
                action.Redo(this);
                undoList.Push(action);
                RecalculateLineStates(
                    Math.Min(action.FirstLine, lineCount - 1), lineCount - 1);
                Modified = true;
            }
        }

        bool ICodeModel.CanUndo { get { return undoList.Count > 0; } }
        bool ICodeModel.CanRedo { get { return redoList.Count > 0; } }

        /// <summary>Name of next revertible operation.</summary>
        string ICodeModel.UndoName =>
            undoList.Count == 0 ? string.Empty : undoList.Peek().Description;

        /// <summary>Name of next repeatable operation.</summary>
        string ICodeModel.RedoName =>
            redoList.Count == 0 ? string.Empty : redoList.Peek().Description;

        /// <summary>Estimates the memory used by the Undo manager.</summary>
        /// <returns>A size in bytes.</returns>
        int ICodeModel.GetUndoStackSize()
        {
            int result = 0;
            foreach (UndoAction action in undoList)
                result += action.ByteCost;
            foreach (UndoAction action in redoList)
                result += action.ByteCost;
            return result;
        }

        #endregion

        #region Lexical analysis.

        /// <summary>Recomputes comment status for a given line range.</summary>
        /// <param name="fromLine">Initial line number.</param>
        /// <param name="toLine">Final line number.</param>
        /// <returns>True, if some line's state changes; false, otherwise.</returns>
        private bool RecalculateLineStates(int fromLine, int toLine)
        {
            bool comment = false;
            bool stateChanges = false;
            if (fromLine > 0)
            {
                LineState prevState = GetState(fromLine - 1);
                if (prevState == LineState.Inside || prevState == LineState.Open)
                    comment = true;
            }
            for (int i = fromLine; i <= toLine; i++)
            {
                bool partialComments = false, innerComments = false, anyText = false;
                foreach (Lexeme lex in scanner.Tokens(this[i], comment))
                {
                    if (lex.Kind == Lexeme.Token.PartialComment)
                        partialComments = true;
                    else if (lex.Kind == Lexeme.Token.Comment)
                        innerComments = true;
                    anyText = true;
                }
                int idx = i <= current.line ? i : capacity - lineCount + i;
                LineState oldState = lines[idx].State;
                LineState newState;
                if (comment)
                    if (partialComments || !anyText)
                        newState = LineState.Inside;
                    else
                        newState = LineState.Close;
                else if (partialComments)
                    newState = LineState.Open;
                else if (innerComments)
                    newState = LineState.Contains;
                else
                    newState = LineState.Clean;
                if (oldState != newState)
                {
                    stateChanges = true;
                    lines[idx].State = newState;
                }
                comment = partialComments || comment && !anyText;
            }
            return stateChanges;
        }

        IEnumerable<Lexeme> ICodeModel.Tokens()
        {
            bool comment = false;
            for (int i = 0; i < lineCount; i++)
            {
                bool partialComments = false;
                foreach (Lexeme lex in scanner.Tokens(this[i], comment))
                {
                    if (lex.Kind == Lexeme.Token.PartialComment)
                        partialComments = true;
                    yield return lex;
                }
                yield return Lexeme.NewLine;
                comment = partialComments;
            }
        }

        IEnumerable<Lexeme> ICodeModel.Tokens(int lineNumber, int firstColumn, int width)
        {
            Position s0, s1;
            if (from.IsLesserThan(current))
            {
                s0 = from; s1 = current;
            }
            else
            {
                s0 = current; s1 = from;
            }
            bool checkSel = s0.NotEquals(s1) &&
                s0.line <= lineNumber && lineNumber <= s1.line;
            int lastColumn = firstColumn + width;
            LineState state = GetState(lineNumber);
            foreach (Lexeme lex in scanner.Tokens(this[lineNumber],
                state == LineState.Inside || state == LineState.Close))
            {
                if (lex.EndColumn <= firstColumn)
                    continue;
                if (lex.Column >= lastColumn)
                    break;
                // This token doesn't cross the selection area.
                if (!checkSel || lineNumber == s1.line && lex.Column >= s1.column ||
                    lineNumber == s0.line && lex.EndColumn <= s0.column)
                    yield return lex;
                // This token is totally contained inside the selection area
                else if ((s0.line < lineNumber ||
                    s0.line == lineNumber && s0.column <= lex.Column) &&
                    (lineNumber < s1.line ||
                    lineNumber == s1.line && lex.EndColumn <= s1.column))
                    yield return new(lex, lex.Column, lex.Text.Length);
                // Selection is within token boundaries.
                else if (s0.line == lineNumber && lineNumber == s1.line &&
                    lex.Column <= s0.column && s1.column <= lex.EndColumn)
                {
                    if (lex.Column < s0.column)
                        yield return new(
                            lex, lex.Column, s0.column - lex.Column, lex.Kind);
                    yield return new(lex, s0.column, s1.column - s0.column);
                    if (lex.EndColumn > s1.column)
                        yield return new(
                            lex, s1.column, lex.EndColumn - s1.column, lex.Kind);
                }
                // Selection chops the head of the token.
                else if (s0.line < lineNumber ||
                    s0.line == lineNumber && s0.column <= lex.Column)
                {
                    yield return new(lex, lex.Column, s1.column - lex.Column);
                    yield return new(
                        lex, s1.column, lex.EndColumn - s1.column, lex.Kind);
                }
                // Selection chops the tail of the token.
                else
                {
                    yield return new(
                        lex, lex.Column, s0.column - lex.Column, lex.Kind);
                    yield return new(lex, s0.column, lex.EndColumn - s0.column);
                }
            }
        }

        #endregion
    }
}
