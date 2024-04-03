using IntSight.Controls;
using IntSight.Controls.CodeModel;

namespace RayEd;

class SillyScanner : ICodeScanner
{
    public SillyScanner() { }

    private sealed class Set<T> : Dictionary<T, T>
    {
        public Set(IEqualityComparer<T> comparer, params T[] values)
            : base(comparer)
        {
            foreach (T value in values)
                Add(value, value);
        }
    }

    private static readonly Set<string> keywords = new(
        StringComparer.InvariantCultureIgnoreCase,
        "sampler", "camera", "background", "objects", "lights", "ambient",
        "end", "spin", "move", "size", "loop", "rgb", "set", "to", "by", "around",
        "var", "media", "shear", "scene"
    );

    #region ICodeScanner Members

    bool ICodeScanner.ContainsCommentCharacters(string text) =>
        text.Contains('{') || text.Contains('}') || text.Contains('/');

    bool ICodeScanner.IsCommentCharacter(char ch) => ch == '{' || ch == '}' || ch == '/';

    int ICodeScanner.DeltaIndent(CodeEditor codeEditor, string lastLine, int lastLineIndex) =>
        string.Compare(lastLine, "objects", true) == 0 ? 1 : 0;

    IEnumerable<Lexeme> ICodeScanner.Tokens(string text, bool comment)
    {
        StringBuilder sb = new();
        int p = 0, column = 0;
        char ch;
    START:
        if (p >= text.Length)
        {
            if (sb.Length > 0)
                yield return new(sb, ref column);
            yield break;
        }
        if (comment)
            ch = '{';
        else
            ch = text[p];
        switch (ch)
        {
            case '\n':
            case '\r':
                {
                    p++;
                    if (ch == '\r' && p < text.Length && text[p] == '\n')
                        p++;
                    if (sb.Length > 0)
                        yield return new(sb, ref column);
                    yield return Lexeme.NewLine;
                    column = 0;
                }
                goto START;
            case '$':
                {
                    int p0 = p;
                    do { p++; }
                    while (p < text.Length &&
                        "0123456789ABCDEFabcdef".Contains(text[p]));
                    sb.Append(text, p0, p - p0);
                }
                goto START;
            case '\'':
            case '"':
                {
                    int p0 = p;
                    do { p++; }
                    while (p < text.Length && text[p] != ch &&
                        "\r\n".IndexOf(text[p]) < 0);
                    if (p < text.Length && text[p] == ch)
                        p++;
                    if (sb.Length > 0)
                        yield return new(sb, ref column);
                    yield return new(Lexeme.Token.String, text[p0..p], ref column);
                }
                goto START;
            case '#':
                {
                    int p0 = p;
                    p++;
                    if (p < text.Length && (text[p] == 'U' || text[p] == 'u'))
                    {
                        p++;
                        while (p < text.Length &&
                            "0123456789ABCDEFabcdef".Contains(text[p]))
                            p++;
                    }
                    else
                        while (p < text.Length && char.IsDigit(text, p))
                            p++;
                    if (sb.Length > 0)
                        yield return new(sb, ref column);
                    yield return new(Lexeme.Token.String, text[p0..p], ref column);
                }
                goto START;
            case '/':
                {
                    int p0 = p;
                    p++;
                    if (p >= text.Length || text[p] != '/')
                    {
                        sb.Append('/');
                        goto START;
                    }
                    do { p++; }
                    while (p < text.Length && "\r\n".IndexOf(text[p]) < 0);
                    if (sb.Length > 0)
                        yield return new(sb, ref column);
                    yield return new(Lexeme.Token.Comment, text[p0..p], ref column);
                }
                goto START;
            case '{':
                {
                    if (sb.Length > 0)
                        yield return new(sb, ref column);
                    int p0 = p;
                    if (comment)
                    {
                        p--;
                        comment = false;
                    }
                    while (true)
                    {
                        p++;
                        if (p >= text.Length)
                        {
                            if (p > p0)
                                yield return new(Lexeme.Token.PartialComment,
                                    text[p0..p], ref column);
                            yield break;
                        }
                        if (text[p] == '}')
                        {
                            p++;
                            break;
                        }
                        if (text[p] == '\n' || text[p] == '\r')
                        {
                            if (p > p0)
                                yield return new(Lexeme.Token.Comment,
                                    text[p0..p], ref column);
                            yield return Lexeme.NewLine;
                            column = 0;
                            p++;
                            if (text[p - 1] == '\r')
                                if (p < text.Length && text[p] == '\n')
                                    p++;
                            p0 = p;
                        }
                    }
                    if (p > p0)
                        yield return new(Lexeme.Token.Comment,
                            text[p0..p], ref column);
                }
                goto START;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                {
                    int p0 = p;
                    do { p++; }
                    while (p < text.Length && char.IsDigit(text, p));
                    sb.Append(text, p0, p - p0);
                }
                goto START;
            case '}':
                p++;
                if (sb.Length > 0)
                    yield return new(sb, ref column);
                yield return new(Lexeme.Token.Error, "}", ref column);
                goto START;
            default:
                if (char.IsLetter(ch) || ch == '_' || ch == '@')
                {
                    int p0 = p;
                    do { p++; }
                    while (p < text.Length &&
                        (char.IsLetterOrDigit(text, p) || text[p] == '_'));
                    string s = text[p0..p];
                    if (keywords.ContainsKey(s))
                    {
                        if (sb.Length > 0)
                            yield return new Lexeme(sb, ref column);
                        yield return new(Lexeme.Token.Keyword, s, ref column);
                    }
                    else
                        sb.Append(s);
                    goto START;
                }
                if (char.IsWhiteSpace(ch))
                {
                    int p0 = p;
                    do { p++; }
                    while (p < text.Length && text[p] != '\r' &&
                        text[p] != '\n' && char.IsWhiteSpace(text, p));
                    sb.Append(text, p0, p - p0);
                    goto START;
                }
                p++;
                sb.Append(text, p - 1, 1);
                goto START;
        }
    }

    void ICodeScanner.RegisterScanner(CodeEditor editor) { }

    void ICodeScanner.TokenTyped(CodeEditor codeEditor, string token, int indent) { }

    string ICodeScanner.LineComment => "//";

    #endregion
}
