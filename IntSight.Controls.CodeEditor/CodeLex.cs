using IntSight.Controls.CodeModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntSight.Controls
{
    public partial class CodeEditor
    {
        /// <summary>
        /// The default implementation of ICodeScanner for no particular language.
        /// </summary>
        /// <remarks>
        /// This is the default scanner used by the code editor.
        /// When <c>CodeEditor.CodeScanner</c> contains a null pointer, the control
        /// uses an internal instance of this class.
        /// </remarks>
        private sealed class DefaultScanner : ICodeScanner
        {
            public DefaultScanner() { }

            #region ICodeScanner Members.

            void ICodeScanner.RegisterScanner(CodeEditor codeEditor) { }

            int ICodeScanner.DeltaIndent(
                CodeEditor codeEditor, string lastLine, int lastLineIndex) => 0;

            /// <summary>Does the supplied string contains any comment characters?</summary>
            /// <param name="text">String to be tested.</param>
            /// <returns>True if succeeds.</returns>
            bool ICodeScanner.ContainsCommentCharacters(string text) => false;

            /// <summary>Is a given character part of a comment delimiter?</summary>
            /// <param name="ch">Character to be tested.</param>
            /// <returns>True if succeeds.</returns>
            bool ICodeScanner.IsCommentCharacter(char ch) => false;

            /// <summary>Gets the sequence that starts a line comment.</summary>
            string ICodeScanner.LineComment => string.Empty;

            void ICodeScanner.TokenTyped(CodeEditor codeEditor, string token, int indent) { }

            IEnumerable<Lexeme> ICodeScanner.Tokens(string text, bool comment)
            {
                StringBuilder sb = new StringBuilder();
                int p = 0, column = 0;
            START:
                if (p >= text.Length)
                {
                    if (sb.Length > 0)
                        yield return new(sb, ref column);
                    yield break;
                }
                char ch = text[p];
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
                    default:
                        // Whitespace, what else?
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

            #endregion
        }
    }
}
