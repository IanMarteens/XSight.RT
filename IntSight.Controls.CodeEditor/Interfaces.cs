using System.IO;
using System.Text;

[assembly: System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]

namespace IntSight.Controls.CodeModel;

/// <summary>Represents a document for the code editor.</summary>
/// <remarks>
/// ICodeModel only handles view independent properties.
/// It doesn't know about lines by page, the current top line, etc.
/// </remarks>
internal interface ICodeModel
{
    /// <summary>Creates a new empty document in memory.</summary>
    void Reset();
    /// <summary>Reads a new document into memory.</summary>
    /// <param name="reader">The document source.</param>
    void Load(TextReader reader);
    /// <summary>Saves current document content.</summary>
    /// <param name="writer">Document target.</param>
    void Save(TextWriter writer);
    /// <summary>Total characters in the active document.</summary>
    /// <returns>Character count.</returns>
    /// <remarks>
    /// This methods returns the number of characters instead of the number of bytes.
    /// Each line separator counts as two characters.
    /// </remarks>
    int GetDocumentSize();

    /// <summary>Number of lines in the document.</summary>
    int LineCount { get; }
    /// <summary>Coordinates of the insertion point.</summary>
    CodeEditor.Position Current { get; set; }
    /// <summary>Coordinates of the selection start.</summary>
    CodeEditor.Position From { get; }
    int RangeWidth(int fromLine, int toLine);
    /// <summary>Number of spaces when expanding a tabulation.</summary>
    int TabLength { get; set; }
    /// <summary>Activates the smart indentation mode.</summary>
    bool SmartIndentation { get; set; }
    /// <summary>Can we pack commands in the undo/redo lists?</summary>
    bool MergeUndoCommands { get; set; }

    /// <summary>Does this control have any selected text?</summary>
    bool HasSelection { get; }
    /// <summary>Copies selected text into the clipboard.</summary>
    /// <param name="cut">When true, removes the selected text.</param>
    void MoveToClipboard(bool cut);
    /// <summary>Copies clipboard contents at the insertion point.</summary>
    void Paste();
    /// <summary>Selectes all text in the editor.</summary>
    void SelectAll();
    /// <summary>Selects text in a whole line.</summary>
    /// <param name="lineNumber">Line to be selected.</param>
    void Select(int lineNumber);
    void SelectTrim(CodeEditor.Position start, CodeEditor.Position end);
    /// <summary>Checks whether the current selection contains a position.</summary>
    /// <param name="position">Position to check.</param>
    /// <returns>True if position is contained.</returns>
    bool InsideSelection(CodeEditor.Position position);
    /// <summary>Selects a word at a given position.</summary>
    /// <param name="position">A position inside the word.</param>
    void SelectWord(CodeEditor.Position position);
    /// <summary>Gets the identifier before the insertion point.</summary>
    /// <returns>The identifier, if any; otherwise, an empty string.</returns>
    string GetIdentifier();
    /// <summary>Gets the identifier at the given coordinates.</summary>
    /// <param name="position">Position to check.</param>
    /// <returns>The identifier, if any; otherwise, an empty string.</returns>
    string GetIdentifier(CodeEditor.Position position);
    string GetLineSufix(CodeEditor.Position position);
    bool IsPreviousText(string text, out int indent);
    string GetFirstToken(int lineNumber, out int indent);
    int GetIndentation(int lineNumber);

    string this[int lineNumber] { get; }
    /// <summary>Source text between two positions.</summary>
    /// <param name="start">Initial position.</param>
    /// <param name="end">Final position.</param>
    /// <returns>Text in between.</returns>
    string TextRange(CodeEditor.Position start, CodeEditor.Position end);
    /// <summary>Deletes all text between two positions.</summary>
    /// <param name="start">Initial position.</param>
    /// <param name="end">Final position.</param>
    /// <remarks>
    /// This method is most used by undo commands.
    /// Inside <c>CodeModel</c>, <c>InternalDelete</c> is used instead.
    /// </remarks>
    void DeleteRange(CodeEditor.Position start, CodeEditor.Position end);
    int InsertRange(CodeEditor.Position start, string text);
    void IndentRange(CodeEditor.Position start, CodeEditor.Position end,
        int amount, params int[] exceptions);
    int[] UnindentRange(CodeEditor.Position start, CodeEditor.Position end, int amount);
    string SelectedText { get; set; }
    /// <summary>The whole source text being edited.</summary>
    string Text { get; }

    /// <summary>Move the insertion point one character right.</summary>
    /// <param name="word">When true, a whole word is moved, instead.</param>
    /// <param name="extend">When true, current selection is extended.</param>
    void MoveRight(bool word, bool extend);
    /// <summary>Move the insertion point one character to the left.</summary>
    /// <param name="word">When true, a whole word is moved, instead.</param>
    /// <param name="extend">When true, current selection is extended.</param>
    void MoveLeft(bool word, bool extend);
    /// <summary>Move the insertion point one line up.</summary>
    /// <param name="extend">When true, current selection is extended.</param>
    void MoveUp(bool extend);
    /// <summary>Move the insertion point one line down.</summary>
    /// <param name="extend">When true, current selection is extended.</param>
    void MoveDown(bool extend);
    /// <summary>Move the caret to the beginning of the line.</summary>
    /// <param name="extend">When true, current selection is extended.</param>
    /// <remarks>
    /// <c>MoveHome</c> alternates between column zero and the first non blank character.
    /// </remarks>
    void MoveHome(bool extend);
    /// <summary>Move the caret to the end of the line.</summary>
    /// <param name="extend">When true, current selection is extended.</param>
    void MoveEnd(bool extend);
    void MovePageUp(ref int topLine, int linesInPage, bool extend);
    void MovePageDown(ref int topLine, int linesInPage, bool extend);
    void MoveTo(CodeEditor.Position position, bool extend);
    /// <summary>Move the insertion point to the matching brace.</summary>
    /// <param name="extend">When true, current selection is extended.</param>
    bool MoveToBrace(bool extend);
    void ScrollTo(ref int topLine, int newTopLine, int linesInPage);
    /// <summary>Watches changes in current position and current timestamp.</summary>
    /// <returns>An IDisposable instance: when disposed, it'll remove the wrap.</returns>
    /// <remarks>
    /// The returned instance has no other state than a method reference.
    /// So, the instance is reused every time <c>WrapOperation</c> is called.
    /// </remarks>
    IDisposable WrapOperation();

    /// <summary>Inserts a printable character at current position.</summary>
    /// <param name="ch">The printable character to insert.</param>
    void Add(char ch);
    /// <summary>Inserts a tabulation at the insertion point.</summary>
    void Tab();
    /// <summary>Unindents a multiline selection.</summary>
    void ShiftTab();
    /// <summary>Inserts a new line at current position.</summary>
    void NewLine();
    /// <summary>Deletes the character at the left side of the cursor.</summary>
    void Backspace();
    /// <summary>Deletes the character at the right side of the cursor.</summary>
    void Delete();
    void DragSelection(CodeEditor.Position position, bool copy);
    /// <summary>Adds indentation to the selected block/current line.</summary>
    void Indent();
    /// <summary>Removes indentation from the selected block/current line.</summary>
    void Outdent();

    /// <summary>Reverts the effect of the last valid operation.</summary>
    void Undo();
    /// <summary>Repeats the last undone operation.</summary>
    void Redo();
    bool CanUndo { get; }
    bool CanRedo { get; }
    /// <summary>Name of next undoable operation.</summary>
    string UndoName { get; }
    /// <summary>Name of next redoable operation.</summary>
    string RedoName { get; }
    /// <summary>Are there modifications in the editor's content?</summary>
    bool Modified { get; set; }
    /// <summary>Estimates the memory used by the Undo manager.</summary>
    /// <returns>Combined size of the undo/redo stacks in bytes.</returns>
    int GetUndoStackSize();

    /// <summary>Iterates over all tokens in the loaded document.</summary>
    /// <returns>The enumerable instance.</returns>
    IEnumerable<Lexeme> Tokens();
    IEnumerable<Lexeme> Tokens(int lineNumber, int firstChar, int width);

    bool FindText(string text, CodeEditor.Position start, bool wholeWords, bool matchCase);
    void ChangeCase(bool upper);
    /// <summary>Adds or removes line comments from the selected text.</summary>
    /// <param name="comment">True for adding, false for removing.</param>
    void CommentSelection(bool comment);
    void ExpandSnippet(ICodeSnippet snippet, bool isSurround);
    /// <summary>Assign a language-dependent lexical analyzer to the model.</summary>
    /// <param name="value">The lexical analyzer implementation.</param>
    void SetScanner(ICodeScanner value);

    /// <summary>Moves the insertion point to the nearest bookmark in sequence.</summary>
    /// <param name="forward">Does we must move forward or backward?</param>
    void GotoBookmark(bool forward);
    /// <summary>Are any bookmarks defined in this control?</summary>
    bool HasBookmarks { get; }
    /// <summary>Has the given line an associated bookmark?</summary>
    /// <param name="lineNumber">The line number.</param>
    /// <returns>True if succeeds.</returns>
    bool IsBookmark(int lineNumber);
    /// <summary>Activates or deactivates a bookmark at the current line.</summary>
    /// <returns>True, if a bookmark has been set; false, otherwise.</returns>
    bool ToggleBookmark();
}

/// <summary>
/// A callback interface internally implemented by CodeModel to support snippets managers.
/// </summary>
public interface ICodeSnippetCallback
{
    void Expand(ICodeSnippet snippet, bool isSurround);
}

/// <summary>
/// A callback interface used by <c>ICodeModel</c> to notify changes to the code editor.
/// </summary>
public interface ICodeView
{
    /// <summary>Returns the code editor control attached to this interface.</summary>
    CodeEditor Control { get; }

    /// <summary>
    /// Invalidates the whole client area of the control and updates the caret position.
    /// </summary>
    void Redraw();
    /// <summary>
    /// Invalidates lines in a given range, scrolls the client area when the current line
    /// moves outside the current view, and updates the caret position.
    /// </summary>
    /// <param name="from">First line to redraw.</param>
    /// <param name="to">Last line to redraw.</param>
    /// <remarks><c>from</c> can be greater than <c>to</c>.</remarks>
    void Redraw(int from, int to);

    /// <summary>
    /// Notifies the code editor the value of <c>model.Modified</c> has been changed.
    /// </summary>
    void ModifiedChanged();
    /// <summary>
    /// Notifies the code editor the editing document has been changed.
    /// </summary>
    void DocumentChanged();
    /// <summary>
    /// Notifies the code editor when the number of lines changes, and when
    /// the first visible line has been changed. The code editor reacts by changing the
    /// information from the vertical scrollbar.
    /// </summary>
    void LineCountChanged();
    /// <summary>Notifies the code editor the insertion point has changed.</summary>
    /// <param name="e">Information details about changes.</param>
    void SelectionChanged(SelectionChangedEventArgs e);
    /// <summary>Notifies the code editor about matching parentheses/brackets.</summary>
    /// <param name="leftStr">String representation of the opening character.</param>
    /// <param name="rightStr">String representation of the closing character.</param>
    /// <param name="leftPos">Position of the opening character.</param>
    /// <param name="rightPos">Position of the closing character.</param>
    void MatchParentheses(string leftStr, string rightStr,
        CodeEditor.Position leftPos, CodeEditor.Position rightPos);
    /// <summary>Notifies the viewport there are no parentheses to hilite.</summary>
    void MatchNoParentheses();
}

/// <summary>
/// The interface with a lexical analyzer for syntax highlighting.
/// </summary>
public interface ICodeScanner
{
    /// <summary>Allows the scanner to initialize the associated code editor.</summary>
    /// <param name="editor">The control this scanner is attached to.</param>
    void RegisterScanner(CodeEditor editor);
    /// <summary>Divides a line into tokens for iteration.</summary>
    /// <param name="text">The text to be analyzed.</param>
    /// <param name="comment">Is this line inside a multiline comment?</param>
    /// <returns>A enumerator for line tokens.</returns>
    IEnumerable<Lexeme> Tokens(string text, bool comment);
    /// <summary>Does the supplied string contains any comment characters?</summary>
    /// <param name="text">String to be tested.</param>
    /// <returns>True if succeeds.</returns>
    bool ContainsCommentCharacters(string text);
    /// <summary>Is a given character part of a comment delimiter?</summary>
    /// <param name="ch">Character to be tested.</param>
    /// <returns>True if succeeds.</returns>
    bool IsCommentCharacter(char ch);
    /// <summary>Gets the sequence that starts a line comment.</summary>
    string LineComment { get; }
    /// <summary>Called when SmartIndentation is true and a new line is entered.</summary>
    /// <param name="codeEditor">This control.</param>
    /// <param name="lastLine">Last not empty line of text.</param>
    /// <param name="lastLineIndex">Position for that line.</param>
    /// <returns>Number of tabs to insert or remove.</returns>
    int DeltaIndent(CodeEditor codeEditor, string lastLine, int lastLineIndex);
    /// <summary>
    /// Called when a token registered with <c>CodeEditor.AddTrigger</c>
    /// has been typed.
    /// </summary>
    /// <param name="codeEditor">The code editor control.</param>
    /// <param name="token">The token just typed.</param>
    /// <param name="indent">Current line indentation.</param>
    void TokenTyped(CodeEditor codeEditor, string token, int indent);
}

/// <summary>Information stored for a code snippet.</summary>
public interface ICodeSnippet
{
    /// <summary>A short name for uniquely identifying the snippet.</summary>
    string Name { get; }
    /// <summary>A readable account of the snippet's purpose.</summary>
    string Description { get; }
    /// <summary>Text inserted when the snippet is expanded.</summary>
    string Text { get; }
}

/// <summary>Takes care of reading the code snippets database.</summary>
public interface ISnippetManager
{
    /// <summary>
    /// Given a prefix, returns all snippets with names that match.
    /// </summary>
    /// <param name="partialName">Snippet name prefix.</param>
    /// <returns>All matching snippet definitions.</returns>
    ICodeSnippet[] GetSnippets(string partialName);
    /// <summary>
    /// Forces the snippet manager to refresh its internal list of available snippets.
    /// </summary>
    void Refresh();
    /// <summary>
    /// Shows a popup list to allow the user to select a snippet from the list.
    /// </summary>
    /// <param name="snippets">The snippet list.</param>
    /// <param name="editor">The code editor control.</param>
    /// <param name="x">Client X position.</param>
    /// <param name="y">Client Y position.</param>
    /// <param name="isSurround">Are we expanding a surround snippet?</param>
    /// <param name="callback">Must be called when a snippet is selected.</param>
    void Select(ICodeSnippet[] snippets, Control editor, int x, int y,
        bool isSurround, ICodeSnippetCallback callback);
}

/// <summary>
/// A line segment returned by the lexical analyzer (a.k.a. code scanner).
/// </summary>
public struct Lexeme
{
    /// <summary>Lexical tokens.</summary>
    public enum Token
    {
        /// <summary>Plain text.</summary>
        Text,
        /// <summary>Selected text.</summary>
        Selection,
        /// <summary>A language keyword.</summary>
        Keyword,
        /// <summary>A character string.</summary>
        String,
        /// <summary>A comment closed in the same line it starts.</summary>
        Comment,
        /// <summary>Unfinished multiline comment.</summary>
        PartialComment,
        /// <summary>Always, the last token in a line.</summary>
        NewLine,
        /// <summary>A dangling end of comment.</summary>
        Error
    }

    public static readonly Lexeme NewLine = new(string.Empty);

    public readonly string Text;
    public readonly Token Kind;
    public readonly int Column;

    public int EndColumn => Column + Text.Length;

    private Lexeme(string emptyText) =>
        (Text, Kind, Column) = (emptyText, Token.NewLine, 0);

    public Lexeme(Lexeme source, int start, int count)
    {
        Kind = Token.Selection;
        Text = source.Text.Substring(start - source.Column, count);
        Column = start;
    }

    public Lexeme(Lexeme source, int start, int count, Token newKind)
    {
        Kind = newKind;
        Text = source.Text.Substring(start - source.Column, count);
        Column = start;
    }

    public Lexeme(Lexeme source)
    {
        this.Kind = Token.Selection;
        this.Text = source.Text;
        this.Column = source.Column;
    }

    public Lexeme(string text, ref int column)
    {
        this.Text = text;
        this.Kind = Token.Text;
        this.Column = column;
        column += text.Length;
    }

    public Lexeme(Token kind, string text, ref int column)
    {
        this.Text = text;
        this.Kind = kind;
        this.Column = column;
        column += text.Length;
    }

    public Lexeme(StringBuilder sb, ref int column)
    {
        this.Text = sb.ToString();
        this.Kind = Token.Text;
        this.Column = column;
        column += this.Text.Length;
        sb.Length = 0;
    }
}
