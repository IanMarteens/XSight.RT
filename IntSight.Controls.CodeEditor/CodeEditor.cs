namespace IntSight.Controls;

using IntSight.Controls.CodeModel;

/// <summary>
/// A configurable code editor with syntax highlighting, full undo/redo,
/// brackets/parentheses matching, bookmarks and code snippets.
/// </summary>
/// <remarks>
/// Still missing:
/// - Code folding.
/// - Parameters for snippet expansion.
/// </remarks>
public partial class CodeEditor : Control, ICodeView, ICodeSnippetCallback
{
    /// <summary>Cursor for the left margin strip.</summary>
    private static readonly Cursor inverseArrow =
        new(typeof(CodeEditor), "InverseArrow.cur");

    public struct Position(int line, int column) : IComparable<Position>, IEquatable<Position>
    {
        public static readonly Position Zero;
        public static readonly Position Infinite =
            new(int.MaxValue, int.MaxValue);

        internal int line = line, column = column < 0 ? 0 : column;

        public int Line
        { 
            readonly get => line;
            set => line = value;
        }

        public int Column
        {
            readonly get => column;
            set => column = value;
        }

        internal readonly Position ChangeLine(int value) => new(value, column);

        public override readonly bool Equals(object obj)
        {
            if (obj != null && obj.GetType() == typeof(Position))
            {
                Position pos = (Position)obj;
                return line == pos.line && column == pos.column;
            }
            else
                return false;
        }

        public override readonly int GetHashCode() => line.GetHashCode() ^ column.GetHashCode();

        public override readonly string ToString() => $"{line}, {column}";

        public Position RangeDecrement(Position p1, Position p2)
        {
            if (p1.IsGreaterThan(p2))
            {
                (p2, p1) = (p1, p2);
            }
            if (IsGreaterEqual(p2))
            {
                if (line == p2.line)
                    column = column - p2.column + p1.column;
                line -= p2.line - p1.line;
            }
            return this;
        }

        /// <summary>Efficient test for position inequality.</summary>
        /// <param name="other">Position to compare with.</param>
        /// <returns>True if positions are not equals.</returns>
        /// <remarks>Operators pass both parameters by value.</remarks>
        internal readonly bool NotEquals(Position other) =>
            line != other.line || column != other.column;

        internal readonly bool IsLesserThan(Position p) =>
            line < p.line || line == p.line && column < p.column;

        internal readonly bool IsGreaterThan(Position p) =>
            line > p.line || line == p.line && column > p.column;

        internal readonly bool IsLesserEqual(Position p) =>
            line < p.line || line == p.line && column <= p.column;

        internal readonly bool IsGreaterEqual(Position p) =>
            line > p.line || line == p.line && column >= p.column;

        public static bool operator ==(Position p1, Position p2) =>
            p1.line == p2.line && p1.column == p2.column;

        public static bool operator !=(Position p1, Position p2) =>
            p1.line != p2.line || p1.column != p2.column;

        public static bool operator <(Position p1, Position p2) =>
            p1.line < p2.line || p1.line == p2.line && p1.column < p2.column;

        public static bool operator >(Position p1, Position p2) =>
            p1.line > p2.line || p1.line == p2.line && p1.column > p2.column;

        public static bool operator <=(Position p1, Position p2) =>
            p1.line < p2.line || p1.line == p2.line && p1.column <= p2.column;

        public static bool operator >=(Position p1, Position p2) =>
            p1.line > p2.line || p1.line == p2.line && p1.column >= p2.column;

        #region IComparable<Position> members.

        public readonly int CompareTo(Position other)
        {
            int result = line.CompareTo(other.line);
            return result != 0 ? result : column.CompareTo(other.column);
        }

        #endregion

        #region IEquatable<Position> members.

        public readonly bool Equals(Position other) => line == other.line && column == other.column;

        #endregion
    }

    private readonly ICodeView view;
    private readonly ICodeModel model;
    private ICodeScanner scanner;
    private int topLine, leftColumn, margin;
    private int linesInPage, columnsInPage;
    private int charWidth, lineHeight;
    private Size lastSize;
    private Color keywordColor, commentColor, stringColor, marginColor, bracketColor;
    private bool doubleClicked, marginSelected, dragMoved, changingCursor;
    private ToolTip toolTip;
    private readonly Dictionary<char, string[]> triggers;

    private static readonly Keys[] inputKeys =
    [
        Keys.Down, Keys.Up, Keys.Right, Keys.Left,
        Keys.Home, Keys.End, Keys.PageDown, Keys.PageUp,
        Keys.Back, Keys.Delete, Keys.Enter,
        Keys.Right | Keys.Shift, Keys.Left | Keys.Shift,
        Keys.Up | Keys.Shift, Keys.Down | Keys.Shift,
        Keys.Home | Keys.Shift, Keys.End | Keys.Shift,
        Keys.Tab, Keys.Tab | Keys.Shift
    ];

    /// <summary>Creates a code editor instance.</summary>
    public CodeEditor()
    {
        view = this;
        DoubleBuffered = true;
        triggers = new Dictionary<char, string[]>(CharComparer.Instance);
        model = new CodeModel(this);
        margin = 8;
        linesInPage = 20;
        columnsInPage = 80;
        Font = new Font("Courier New", 10.0F);
        BackColor = SystemColors.Window;
        ForeColor = SystemColors.WindowText;
        SetStyle(ControlStyles.Selectable, true);
        Cursor = Cursors.IBeam;
        AllowDrop = true;
        keywordColor = Color.Blue;
        commentColor = Color.Green;
        stringColor = Color.Maroon;
        marginColor = Color.Gainsboro;
        bracketColor = Color.Gainsboro;
        // This ToolTip instance shows while the user moves the thumb
        // from the vertical scrollbar up and down.
        toolTip = new ToolTip
        {
            InitialDelay = 100
        };
        base.Text = string.Empty;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && toolTip != null)
        {
            toolTip.Dispose();
            toolTip = null;
        }
        base.Dispose(disposing);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FontName
    {
        get => Font.Name;
        set
        {
            if (value != Font.Name)
                Font = new Font(value, Font.Size);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float FontSize
    {
        get => Font.Size;
        set
        {
            if (value != Font.Size)
                Font = new Font(Font.Name, value);
        }
    }

    [DefaultValue(true)]
    public override bool AllowDrop
    {
        get => base.AllowDrop;
        set => base.AllowDrop = value;
    }

    [DefaultValue(typeof(Cursor), "IBeam")]
    public override Cursor Cursor
    {
        get => base.Cursor;
        set => base.Cursor = value;
    }

    [DefaultValue(typeof(Color), "Window")]
    public override Color BackColor
    {
        get => base.BackColor;
        set => base.BackColor = value;
    }

    [DefaultValue(typeof(Color), "WindowText")]
    public override Color ForeColor
    {
        get => base.ForeColor;
        set => base.ForeColor = value;
    }

    /// <summary>Foreground color used to display keywords.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), "Blue")]
    [Description("The foreground color used to display keywords.")]
    public Color KeywordColor
    {
        get => keywordColor;
        set
        {
            if (value != keywordColor)
            {
                keywordColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>Color used to draw the left margin.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), "Gainsboro")]
    [Description("The color used to draw the left margin.")]
    public Color MarginColor
    {
        get => marginColor;
        set
        {
            if (value != marginColor)
            {
                marginColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>Foreground color for displaying code comments.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), "Green")]
    [Description("The foreground color used to display code comments.")]
    public Color CommentColor
    {
        get => commentColor;
        set
        {
            if (value != commentColor)
            {
                commentColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>Foreground color for displaying string literals.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), "Maroon")]
    [Description("The foreground color used to display string literals.")]
    public Color StringColor
    {
        get => stringColor;
        set
        {
            if (value != stringColor)
            {
                stringColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>Background color for highlighting parenthesis and brackets.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), "Gainsboro")]
    [Description("The background color used to highlight parenthesis and brackets.")]
    public Color BracketColor
    {
        get => bracketColor;
        set
        {
            if (value != bracketColor)
            {
                bracketColor = value;
                Invalidate();
            }
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
        get => model.Text;
        set => base.Text = value;
    }

    /// <summary>Gets the number of lines in the open document.</summary>
    [Browsable(false)]
    public int LineCount => model.LineCount;

    /// <summary>Gets the current position of the insertion point.</summary>
    [Browsable(false)]
    public Position Current => model.Current;

    /// <summary>Gets the initial position of the selected block.</summary>
    [Browsable(false)]
    public Position SelectionStart => model.From;

    [Browsable(false)]
    public int CurrentColumn => model.Current.column + 1;

    [Browsable(false)]
    public bool HasSelection => model.HasSelection;

    [Browsable(false)]
    public int FirstVisibleLine => topLine + 1;

    [Browsable(false)]
    public int LastVisibleLine => topLine + linesInPage;

    /// <summary>Ordinal number of current selected line in code editor.</summary>
    [Browsable(false)]
    [Description("Ordinal number of current selected line in code editor.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentLine
    {
        get => model.Current.line + 1;
        set
        {
            using (model.WrapOperation())
                model.MoveTo(model.Current.ChangeLine(value - 1), false);
        }
    }

    /// <summary>Executes the Go To Line modal dialog.</summary>
    /// <returns>True if accepted; false, otherwise.</returns>
    public bool ExecuteGotoLineDialog() =>
        model.LineCount > 0 && GotoLineDialog.Execute(this);

    /// <summary>Number of spaces for each hard coded tabulation.</summary>
    [Browsable(true), Category("Behavior"), DefaultValue(4)]
    [Description("Number of spaces for each hard coded tabulation.")]
    public int TabLength
    {
        get => model.TabLength;
        set => model.TabLength = Math.Max(1, value);
    }

    /// <summary>Activates automatic indentation according to the language.</summary>
    [Browsable(true), Category("Behavior"), DefaultValue(true)]
    [Description("Activates automatic indentation according to the language.")]
    public bool SmartIndentation
    {
        get => model.SmartIndentation;
        set => model.SmartIndentation = value;
    }

    /// <summary>
    /// Merge consecutive insertions and deletions inside the undo list.
    /// </summary>
    [Browsable(true), Category("Behavior"), DefaultValue(true)]
    [Description("Merge consecutive insertions and deletions inside the undo list.")]
    public bool MergeUndoCommands
    {
        get => model.MergeUndoCommands;
        set => model.MergeUndoCommands = value;
    }

    /// <summary>Width in pixels of the left margin.</summary>
    [Browsable(true), Category("Behavior"), DefaultValue(8)]
    [Description("Width in pixels of the left margin.")]
    public int LeftMargin
    {
        get => margin;
        set
        {
            if (margin != value)
            {
                margin = value;
                CalculateMeasures();
                view.Redraw();
            }
        }
    }

    /// <summary>The lexical analyzer used by the editor.</summary>
    [Browsable(true), Category("Behavior"), DefaultValue(null)]
    [Description("The lexical analyzer used by the editor.")]
    public ICodeScanner CodeScanner
    {
        get => scanner;
        set
        {
            scanner = value;
            model.SetScanner(value);
            if (value != null)
                value.RegisterScanner(this);
            else
                DeleteTriggers();
        }
    }

    /// <summary>Iterates over all tokens in the code editor.</summary>
    /// <returns>The sequence of tokens, according to the language.</returns>
    /// <remarks><c>Tokens</c> is used by <c>EditorDocument</c>.</remarks>
    public IEnumerable<Lexeme> Tokens() => model.Tokens();

    /// <summary>Gets text at a given line.</summary>
    /// <param name="lineNumber">Line number.</param>
    /// <returns>Text at the specified line.</returns>
    [Browsable(false)]
    public string this[int lineNumber] => model[lineNumber];

    public string GetFirstToken(int lineNumber, out int indent) =>
        model.GetFirstToken(lineNumber, out indent);

    public int GetIndentation(int lineNumber) =>
        model.GetIndentation(lineNumber);

    #region Triggers.

    /// <summary>Ask the editor to emit a notification after a word is typed.</summary>
    /// <param name="trigger">Word that will trigger the notification.</param>
    /// <remarks>The notification is received by the code scanner.</remarks>
    public void AddTrigger(string trigger)
    {
        if (!string.IsNullOrEmpty(trigger))
        {
            trigger = trigger.ToLowerInvariant();
            char key = trigger[^1];
            if (triggers.TryGetValue(key, out string[] value))
            {
                string[] list = value;
                Array.Resize(ref list, list.Length + 1);
                list[^1] = trigger;
                triggers[key] = list;
            }
            else
                triggers.Add(key, [trigger]);
        }
    }

    /// <summary>Delete all registered triggers.</summary>
    public void DeleteTriggers() => triggers.Clear();

    #endregion

    #region Bookmarks.

    /// <summary>Moves the insertion point to the nearest bookmark in sequence.</summary>
    /// <param name="forward">Does we must move forward or backward?</param>
    public void GotoBookmark(bool forward)
    {
        using (model.WrapOperation())
            model.GotoBookmark(forward);
    }

    /// <summary>Does this control have active bookmarks?</summary>
    [Browsable(false)]
    public bool HasBookmarks => model.HasBookmarks;

    /// <summary>Does the current line have a bookmark?</summary>
    /// <returns>True if succeeds.</returns>
    public bool InBookmark() => model.IsBookmark(model.Current.line);

    /// <summary>Turns on and off a bookmark at the current line.</summary>
    /// <returns>True, if a bookmark has been set.</returns>
    public bool ToggleBookmark() => model.ToggleBookmark();

    #endregion

    #region Undo manager.

    /// <summary>Repeats the last undone operation.</summary>
    public void Redo()
    {
        using (model.WrapOperation())
            model.Redo();
    }

    /// <summary>Cancels the last effective operation.</summary>
    public void Undo()
    {
        using (model.WrapOperation())
            model.Undo();
    }

    [Browsable(false)]
    public bool CanUndo => model.CanUndo;

    [Browsable(false)]
    public bool CanRedo => model.CanRedo;

    /// <summary>A descriptive name for the last undoable operation.</summary>
    [Browsable(false)]
    public string UndoName => model.UndoName;

    /// <summary>A descriptive name for the next redoable operation.</summary>
    [Browsable(false)]
    public string RedoName => model.RedoName;

    /// <summary>The value of the <c>Modified</c> property has changed.</summary>
    void ICodeView.ModifiedChanged() => OnModifiedChanged(EventArgs.Empty);

    /// <summary>Editor's content has been modified.</summary>
    void ICodeView.DocumentChanged() => OnDocumentChanged(EventArgs.Empty);

    /// <summary>The insertion point has changed.</summary>
    /// <param name="e">Information about changes.</param>
    void ICodeView.SelectionChanged(SelectionChangedEventArgs e) => OnSelectionChanged(e);

    /// <summary>Fires the <c>DocumentChanged</c> event.</summary>
    /// <param name="e">Unused.</param>
    protected virtual void OnDocumentChanged(EventArgs e) => DocumentChanged?.Invoke(this, e);

    /// <summary>Fires the <c>ModifiedChanged</c> event.</summary>
    /// <param name="e">Unused.</param>
    protected virtual void OnModifiedChanged(EventArgs e) => ModifiedChanged?.Invoke(this, e);

    /// <summary>Fires the <c>SelectionChanged</c> event.</summary>
    /// <param name="e">Information about the changed selection.</param>
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs e) =>
        SelectionChanged?.Invoke(this, e);

    [Browsable(true), Category("Property Changed")]
    [Description("Raised every time the editor's content is changed.")]
    public event EventHandler DocumentChanged;

    [Browsable(true), Category("Property Changed")]
    [Description("Raised when the value of the Modified property is changed.")]
    public event EventHandler ModifiedChanged;

    [Browsable(true), Category("Property Changed")]
    [Description("Raised when the value of the Selection property is changed.")]
    public event SelectionChangedEventHandler SelectionChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Modified
    {
        get => model.Modified;
        set => model.Modified = value;
    }

    /// <summary>Estimates the memory used by the Undo manager.</summary>
    /// <returns>Total number of bytes used by the undo/redo stacks.</returns>
    public int GetUndoStackSize() => model.GetUndoStackSize();

    #endregion

    #region Document management.

    /// <summary>Name of the currently loaded file.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FileName { get; private set; }

    [Description("Fired when the FileName property changes.")]
    public event EventHandler FileNameChanged;

    /// <summary>Initializes an empty document.</summary>
    public void Reset()
    {
        using (model.WrapOperation())
        {
            topLine = 0;
            leftColumn = 0;
            model.Reset();
            HorizontalScrollChanged();
            view.Redraw();
            SetFileName(string.Empty);
        }
    }

    /// <summary>Loads contents from a text reader.</summary>
    /// <param name="reader">Text reader with source code.</param>
    public void Load(TextReader reader)
    {
        using (model.WrapOperation())
        {
            topLine = 0;
            leftColumn = 0;
            model.Load(reader);
            HorizontalScrollChanged();
            view.Redraw();
        }
    }

    /// <summary>Loads contents from a stream.</summary>
    /// <param name="stream">Data stream with source code.</param>
    public void Load(Stream stream)
    {
        using TextReader reader = new StreamReader(stream);
        Load(reader);
    }

    /// <summary>Loads contents from an OS file.</summary>
    /// <param name="path">Name of file with source code.</param>
    public void Load(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            Reset();
        else
        {
            using (TextReader reader = new StreamReader(path))
                Load(reader);
            SetFileName(path);
        }
    }

    public void Save(TextWriter writer) => model.Save(writer);

    public void Save(Stream stream)
    {
        using TextWriter writer = new StreamWriter(stream);
        Save(writer);
    }

    /// <summary>Saves editor's contents to a file.</summary>
    /// <param name="path">Full path for the file.</param>
    public void Save(string path)
    {
        using (TextWriter writer = new StreamWriter(path))
            Save(writer);
        SetFileName(path);
    }

    /// <summary>Total characters in the active document.</summary>
    /// <returns>Number of characters.</returns>
    public int GetDocumentSize() => model.GetDocumentSize();

    private void SetFileName(string filename)
    {
        FileName = filename;
        FileNameChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Clipboard and selected text.

    /// <summary>Copies selected text to the clipboard.</summary>
    public void Copy() => model.MoveToClipboard(false);

    /// <summary>Moves selected text to the clipboard.</summary>
    public void Cut()
    {
        using (model.WrapOperation())
            model.MoveToClipboard(true);
    }

    /// <summary>Inserts text from the clipboard into the code editor.</summary>
    public void Paste()
    {
        using (model.WrapOperation())
            model.Paste();
    }

    /// <summary>Selects all text in the code editor.</summary>
    public void SelectAll()
    {
        using (model.WrapOperation())
            model.SelectAll();
    }

    /// <summary>Selects a range inside the code editor.</summary>
    /// <param name="start">Selection start.</param>
    /// <param name="end">Selection end.</param>
    public void Select(Position start, Position end)
    {
        using (model.WrapOperation())
        {
            model.MoveTo(start, false);
            model.MoveTo(end, true);
        }
    }

    /// <summary>Selects a trimmed range inside the code editor.</summary>
    /// <param name="start">Selection start.</param>
    /// <param name="end">Selection end.</param>
    public void SelectTrim(Position start, Position end)
    {
        using (model.WrapOperation())
        {
            MakeLineVisible(Math.Min(start.line, end.line));
            model.SelectTrim(start, end);
        }
    }

    /// <summary>Selects a range inside a line in the code editor.</summary>
    /// <param name="lineNumber">Line number.</param>
    /// <param name="fromColumn">Starting column.</param>
    /// <param name="toColumn">Ending column.</param>
    public void Select(int lineNumber, int fromColumn, int toColumn)
    {
        using (model.WrapOperation())
        {
            MakeLineVisible(lineNumber);
            model.MoveTo(new(lineNumber, fromColumn), false);
            model.MoveTo(new(lineNumber, toColumn), true);
        }
    }

    /// <summary>Select a whole line.</summary>
    /// <param name="lineNumber">Line number.</param>
    public void Select(int lineNumber)
    {
        using (model.WrapOperation())
        {
            lineNumber = MakeLineVisible(lineNumber);
            // Select the desired range.
            model.MoveTo(new(lineNumber + 1, 0), false);
            model.MoveTo(new(lineNumber, 0), true);
        }
    }

    /// <summary>Move cursor to first non-blank after position.</summary>
    /// <param name="lineNumber">Line number.</param>
    /// <param name="columnNumber">Column number.</param>
    public void Select(int lineNumber, int columnNumber)
    {
        using (model.WrapOperation())
        {
            lineNumber = MakeLineVisible(lineNumber);
            model.MoveTo(new(lineNumber, columnNumber), false);
            string line = model[lineNumber];
            if (columnNumber >= line.Length ||
                char.IsWhiteSpace(line, columnNumber))
                model.MoveRight(true, false);
        }
    }

    /// <summary>
    /// Changes the first visible line to ensure a line is comfortably visible.
    /// </summary>
    /// <param name="lineNumber">Line we want to make visible.</param>
    /// <returns>The adjusted line number.</returns>
    private int MakeLineVisible(int lineNumber)
    {
        const int BOTTOM_MARGIN = 8;

        if (lineNumber < 0)
            lineNumber = 0;
        else if (lineNumber >= model.LineCount)
            lineNumber = model.LineCount - 1;
        // Compute a new feasible top line number.
        if (lineNumber < topLine)
            model.ScrollTo(ref topLine, lineNumber, linesInPage);
        else if (lineNumber - topLine > linesInPage - BOTTOM_MARGIN)
            model.ScrollTo(ref topLine, lineNumber - linesInPage / 2, linesInPage);
        return lineNumber;
    }

    /// <summary>Can we paste text from the clipboard?</summary>
    [Browsable(false)]
    public bool CanPaste => Clipboard.ContainsText();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SelectedText
    {
        get => model.SelectedText;
        set
        {
            using (model.WrapOperation())
                model.SelectedText = value;
        }
    }

    /// <summary>Increase left margin for the selected text.</summary>
    public void Indent()
    {
        if (model.LineCount > 0)
            using (model.WrapOperation())
                model.Indent();
    }

    /// <summary>Decrease left margin for the selected text.</summary>
    public void Outdent()
    {
        if (model.LineCount > 0)
            using (model.WrapOperation())
                model.Outdent();
    }

    /// <summary>Adds or removes line comments from the selected text.</summary>
    /// <param name="comment">True for adding, false for removing.</param>
    public void CommentSelection(bool comment)
    {
        using (model.WrapOperation())
            model.CommentSelection(comment);
    }

    /// <summary>Transform letter case for the selected text.</summary>
    /// <param name="upper">When true, makes text uppercase.</param>
    public void ChangeCase(bool upper) => model.ChangeCase(upper);

    /// <summary>
    /// Deletes selected text or, when there's no selection,
    /// the character following the insertion point.
    /// </summary>
    public void Delete()
    {
        using (model.WrapOperation())
            model.Delete();
    }

    /// <summary>Finds a given text inside the code editor.</summary>
    /// <param name="text">Text for searching.</param>
    /// <param name="start">Initial position.</param>
    /// <param name="wholeWords">Report isolate words only.</param>
    /// <param name="matchCase">Text must have identical case.</param>
    /// <returns>True, if text was found.</returns>
    public bool FindText(string text, Position start, bool wholeWords, bool matchCase)
    {
        using (model.WrapOperation())
            return model.FindText(text, start, wholeWords, matchCase);
    }

    public void ExpandSnippet(ICodeSnippet snippet)
    {
        using (model.WrapOperation())
            model.ExpandSnippet(snippet, true);
    }

    protected void SelectSnippet(ICodeSnippet[] snippets, bool isSurround)
    {
        using (model.WrapOperation())
            if (snippets != null && snippets.Length == 1)
                model.ExpandSnippet(snippets[0], isSurround);
            else
                SnippetManager.Select(snippets, this,
                    margin + (model.Current.column - leftColumn) * charWidth + 1,
                    (model.Current.line - topLine + 1) * lineHeight, isSurround, this);
    }

    public Point GetCursorPosition() => new(
        margin + (model.Current.column - leftColumn) * charWidth + 1,
        (model.Current.line - topLine + 1) * lineHeight);

    protected void SelectSnippet()
    {
        if (SnippetManager != null)
            using (model.WrapOperation())
            {
                ICodeSnippet[] pets = SnippetManager.GetSnippets(model.GetIdentifier());
                if ((pets == null || pets.Length == 0) && !model.HasSelection)
                    pets = SnippetManager.GetSnippets(string.Empty);
                SelectSnippet(pets, false);
            }
    }

    /// <summary>
    /// Ask the Snippet Manager to show the list with all snippets,
    /// for a surround expansion.
    /// </summary>
    /// <remarks>
    /// If there is a selection, it will be surrounded with the expanded code.
    /// </remarks>
    public void ShowSnippets()
    {
        if (SnippetManager != null)
            using (model.WrapOperation())
                SelectSnippet(SnippetManager.GetSnippets(string.Empty), true);
    }

    [Category("Behavior"), DefaultValue(null)]
    public ISnippetManager SnippetManager { get; set; }

    #endregion

    #region ICodeSnippetCallback Members

    void ICodeSnippetCallback.Expand(ICodeSnippet snippet, bool isSurround)
    {
        using (model.WrapOperation())
            model.ExpandSnippet(snippet, isSurround);
    }

    #endregion

    private class CharComparer : IEqualityComparer<char>
    {
        public static readonly IEqualityComparer<char> Instance = new CharComparer();

        #region IEqualityComparer<char> members.

        bool IEqualityComparer<char>.Equals(char x, char y) =>
            char.ToLowerInvariant(x) == char.ToLowerInvariant(y);

        int IEqualityComparer<char>.GetHashCode(char obj) =>
            char.ToLowerInvariant(obj).GetHashCode();

        #endregion
    }
}
