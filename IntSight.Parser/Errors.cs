using System;
using System.Collections.Generic;
using System.Text;
using Rsc = IntSight.Parser.Properties.Resources;

namespace IntSight.Parser
{
    /// <summary>A specialized exception for the parsing stage.</summary>
    /// <remarks>
    /// This exception is raised for aborting the parsing algorithm.
    /// </remarks>
    [Serializable]
    public sealed class ParsingException : Exception
    {
        public ParsingException() { }
        public ParsingException(string message) : base(message) { }
        public ParsingException(string message, Exception innerException)
            : base(message, innerException) { }
        public ParsingException(string message, params object[] args)
            : base(string.Format(message, args)) { }
        private ParsingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public ParsingException(SourceRange position, string message, params object[] args)
            : base(string.Format(message, args)) => Position = position;

        public SourceRange Position { get; }
    }

    [Serializable]
    public sealed class AbortException : Exception
    {
        public AbortException() : base(Rsc.ParsingAborted) { }
        public AbortException(string message) : base(message) { }
        public AbortException(string message, Exception innerException)
            : base(message, innerException) { }
        private AbortException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }


    /// <summary>A collection of errors, sorted by source code position.</summary>
    public sealed class Errors
    {
        public sealed class Error : IComparable<Error>
        {
            internal Error(SourceRange position, string message)
            {
                Position = position;
                Message = message;
            }

            public SourceRange Position { get; }
            public string Message { get; }

            public override string ToString() =>
                string.Format(Rsc.FormatMessage, Position, Message);

            #region IComparable<Error> members.

            public int CompareTo(Error other)
            {
                int result = Position.CompareTo(other.Position);
                return result != 0 ? result : Message.CompareTo(other.Message);
            }

            #endregion
        }

        private readonly List<Error> errors = new List<Error>();
        private bool dirty;

        /// <summary>Creates an empty list of errors.</summary>
        public Errors() { }

        /// <summary>Converts an exception into an error.</summary>
        /// <param name="exception">The intercepted exception.</param>
        public void Add(Exception exception)
        {
            if (!(exception is NullReferenceException))
                Add(exception is ParsingException pe ? pe.Position : SourceRange.Default,
                    exception.Message);
        }

        public void Add(SourceRange position, string format, params object[] args)
        {
            errors.Add(new Error(position, string.Format(format, args)));
            dirty = true;
        }

        public void Add(IAstNode anchorNode, string format, params object[] args)
        {
            errors.Add(new Error(
                anchorNode != null ? anchorNode.Position : SourceRange.Default,
                string.Format(format, args)));
            dirty = true;
        }

        public void Throw(IAstNode anchorNode, string format, params object[] args)
        {
            errors.Add(new Error(
                anchorNode != null ? anchorNode.Position : SourceRange.Default,
                string.Format(format, args)));
            dirty = true;
            throw new AbortException();
        }

        public void Clear()
        {
            errors.Clear();
            dirty = false;
        }

        public IEnumerator<Error> GetEnumerator()
        {
            Sort();
            return errors.GetEnumerator();
        }

        public int Count => errors.Count;

        public string Report()
        {
            Sort();
            StringBuilder sb = new();
            foreach (Error error in errors)
                sb.Append(error).AppendLine();
            return sb.ToString();
        }

        private void Sort()
        {
            if (dirty)
            {
                errors.Sort();
                dirty = false;
            }
        }

        public void Release(int mark)
        {
            while (errors.Count > mark)
                errors.RemoveAt(errors.Count - 1);
        }
    }
}
