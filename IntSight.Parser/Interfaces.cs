using System;
using System.Diagnostics.SymbolStore;

namespace IntSight.Parser
{
    /// <summary>Creates a ISource entity by demand.</summary>
    public interface IDocument : IComparable<IDocument>
    {
        /// <summary>Creates a compiler source from this document.</summary>
        /// <returns>A source that can be used by the lexical analyzer.</returns>
        ISource Open();
        string Url { get; }
        string Name { get; }
        ISymbolDocumentWriter SymbolWriter { get; set; }
    }

    /// <summary>Represents a single source code file.</summary>
    public interface ISource : IDisposable
    {
        IDocument Document { get; }
        SourceRange GetRange(int length);
        ushort FirstChar { get; }
        ushort this[int position] { get; }
        string Read(int size);
        string Skip(int size);
    }

    /// <summary>Recommended interface to be implemented by AST nodes.</summary>
    public interface IAstNode
    {
        /// <summary>Node's position inside the source file.</summary>
        SourceRange Position { get; set; }
    }
}
