using System;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using Rsc = IntSight.Parser.Properties.Resources;

namespace IntSight.Parser
{
    public sealed class FileDocument : IDocument
    {
        private readonly string fileName;

        public FileDocument(string fileName) => this.fileName = fileName;

        public override string ToString() => fileName;

        #region IDocument members.

        string IDocument.Url => fileName;
        string IDocument.Name => Path.GetFileName(fileName);

        ISymbolDocumentWriter IDocument.SymbolWriter { get; set; }

        ISource IDocument.Open()
        {
            try
            {
                return new FileSource(this, File.OpenText(fileName));
            }
            catch (Exception e)
            {
                throw new ParsingException(string.Format(Rsc.FileCannotOpen, fileName), e);
            }
        }

        #endregion

        #region IComparable<IDocument> members.

        int IComparable<IDocument>.CompareTo(IDocument other) =>
            other == null
            ? -1 : string.Compare(fileName, other.Url, StringComparison.InvariantCulture);

        #endregion
    }

    public sealed class StreamDocument : IDocument
    {
        private readonly string fileName;
        private readonly Stream stream;

        public StreamDocument(string fileName, Stream stream)
        {
            this.fileName = fileName;
            this.stream = stream;
        }

        public override string ToString() => fileName;

        #region IDocument members.

        string IDocument.Url => fileName;
        string IDocument.Name => Path.GetFileName(fileName);

        ISymbolDocumentWriter IDocument.SymbolWriter { get; set; }

        ISource IDocument.Open()
        {
            try
            {
                return new FileSource(this, new StreamReader(stream));
            }
            catch (Exception e)
            {
                throw new ParsingException(string.Format(Rsc.FileCannotOpen, fileName), e);
            }
        }

        #endregion

        #region IComparable<IDocument> members.

        int IComparable<IDocument>.CompareTo(IDocument other) =>
            other == null ? -1 :
            string.Compare(fileName, other.Url, StringComparison.InvariantCulture);

        #endregion
    }

    public sealed class FileSource : ISource
    {
        private const int BUFLEN = 8192;

        private readonly IDocument document;
        private readonly TextReader reader;
        private readonly char[] buffer;
        private int current, length;
        private int line, column, tokenPos;

        public FileSource(IDocument document, TextReader reader)
        {
            this.document = document;
            this.reader = reader;
            buffer = new char[BUFLEN];
            ReadBuffer(0);
            line = column = tokenPos = 1;
        }

        private void ReadBuffer(int delta)
        {
            current = 0;
            length = delta + reader.Read(buffer, delta, BUFLEN - delta);
            if (length < BUFLEN)
                buffer[length++] = '\u0000';
        }

        #region ISource members.

        void IDisposable.Dispose() => reader.Close();

        ushort ISource.FirstChar
        {
            get
            {
            state0:
                if (current == length)
                    ReadBuffer(0);
                switch (buffer[current])
                {
                    case '\u0000':
                        tokenPos = column;
                        return 0;
                    case '\u0009':
                    case '\u0020':
                    case '\u00A0':
                        column++;
                        current++;
                        goto state0;
                    case '\u000A':
                        line++;
                        column = 1;
                        current++;
                        goto state0;
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                        current++;
                        goto state0;
                    case '{':
                        column++;
                        current++;
                        goto state1;
                    case '/':
                        if (this[1] != '/')
                            return '/';
                        column += 2;
                        current += 2;
                        goto state2;
                    default:
                        tokenPos = column;
                        return buffer[current];
                }

            state1:
                if (current == length)
                    ReadBuffer(0);
                switch (buffer[current])
                {
                    case '\u0000':
                        tokenPos = column;
                        return 0;
                    case '\u000A':
                        line++;
                        column = 1;
                        current++;
                        goto state1;
                    case '}':
                        column++;
                        current++;
                        goto state0;
                    default:
                        column++;
                        current++;
                        goto state1;
                }

            state2:
                if (current == length)
                    ReadBuffer(0);
                switch (buffer[current])
                {
                    case '\u0000':
                        tokenPos = column;
                        return 0;
                    case '\u000A':
                        line++;
                        column = 1;
                        current++;
                        goto state0;
                    default:
                        column++;
                        current++;
                        goto state2;
                }
            }
        }

        IDocument ISource.Document => document;

        SourceRange ISource.GetRange(int length) =>
            new SourceRange(document,
                line, (short)tokenPos, line, (short)(tokenPos + length));

        public ushort this[int position]
        {
            get
            {
                // ASSERT: position > 0
                if ((position += current) >= length)
                {
                    Array.Copy(buffer, current, buffer, 0, position -= current);
                    ReadBuffer(position);
                }
                return buffer[position];
            }
        }

        string ISource.Read(int size)
        {
            Debug.Assert(size <= length - current);
            var result = new string(buffer, current, size);
            column += size;
            current += size;
            // No valid token contains \u000A or \u000D.
            // There's no need to count line feeds here.
            return result;
        }

        string ISource.Skip(int size)
        {
            Debug.Assert(size <= length - current);
            column += size;
            current += size;
            return null;
        }

        #endregion
    }
}