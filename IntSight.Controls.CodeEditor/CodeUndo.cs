using System;
using System.Collections.Generic;
using IntSight.Controls.CodeModel;
using Rsc = IntSight.Controls.Properties.Resources;

namespace IntSight.Controls
{
    public partial class CodeEditor
    {
        /// <summary>The common abstract base for all undo commands.</summary>
        private abstract class UndoAction
        {
            protected Position start, end;

            public UndoAction(Position start, Position end) =>
                (this.start, this.end) = (start, end);

            /// <summary>An attempt to merge or group two editor commands.</summary>
            /// <param name="another">The newest command.</param>
            /// <returns>Returns true when successful.</returns>
            /// <remarks>
            /// When <c>TryMerge</c> succeeds, <c>this</c> is updated to contain
            /// all relevant information from <c>another</c>.
            /// </remarks>
            public virtual bool TryMerge(UndoAction another) => false;

            public virtual int FirstLine => Math.Min(start.line, end.line);

            public virtual bool TryClearIndent(ICodeModel model) => false;

            /// <summary>Undo the represented editor command.</summary>
            /// <param name="model">The document being edited.</param>
            public abstract void Undo(ICodeModel model);

            /// <summary>Repeats the action of the represented editor command.</summary>
            /// <param name="model">The document being edited.</param>
            public abstract void Redo(ICodeModel model);

            public abstract string Description { get; }
            public abstract int ByteCost { get; }
        }

        private sealed class DeleteAction : UndoAction
        {
            private string text;

            public DeleteAction(Position start, Position end, string text)
                : base(start, end) => this.text = text;

            public bool MatchesInsertion(Position iEnd) =>
                iEnd.Equals(end) && start.line == end.line && start.column == end.column - 1;

            public override bool TryMerge(UndoAction another)
            {
                if (!(another is DeleteAction other))
                    return false;
                else if (start.Equals(other.end))
                {
                    text = other.text + text;
                    start = other.start;
                    return true;
                }
                else if (start.Equals(other.start) && other.text.Length == 1)
                {
                    text += other.text;
                    end.column++;
                    return true;
                }
                else
                    return false;
            }

            public override void Redo(ICodeModel model) => model.DeleteRange(start, end);

            public override void Undo(ICodeModel model)
            {
                model.MoveTo(start, false);
                model.InsertRange(start, text);
            }

            public override string Description => Rsc.UndoCmdDeletion;
            public override int ByteCost => 20 + text.Length;
        }

        private sealed class DragMoveAction : UndoAction
        {
            private readonly string text;
            private Position startText, endText;

            public DragMoveAction(Position start, Position end,
                Position startText, Position endText, string text)
                : base(start, end)
            {
                this.startText = startText;
                this.endText = endText;
                this.text = text;
            }

            public override int FirstLine => Math.Min(Math.Min(base.FirstLine, startText.line), endText.line);

            public override void Undo(ICodeModel model)
            {
                if (startText.IsGreaterEqual(end))
                {
                    model.InsertRange(start, text);
                    model.DeleteRange(startText, endText);
                }
                else
                {
                    model.DeleteRange(startText, endText);
                    model.InsertRange(start, text);
                }
                model.MoveTo(start, false);
                model.MoveTo(end, true);
            }

            public override void Redo(ICodeModel model)
            {
                if (startText.IsGreaterEqual(end))
                {
                    model.InsertRange(startText, text);
                    model.DeleteRange(start, end);
                }
                else
                {
                    model.DeleteRange(start, end);
                    model.InsertRange(startText, text);
                }
            }

            public override string Description => Rsc.UndoCmdDragMove;
            public override int ByteCost => 36 + text.Length;
        }

        private abstract class BaseCommentAction : UndoAction
        {
            protected List<Position> insertPoints = new List<Position>();
            protected string lineComment;

            public BaseCommentAction(string lineComment)
                : base(Position.Infinite, Position.Infinite) =>
                this.lineComment = lineComment;

            public void Add(Position position)
            {
                insertPoints.Add(position);
                if (position.IsLesserThan(start))
                    start = position;
            }

            public sealed override int ByteCost => 20 + 8 * insertPoints.Count;
        }

        private sealed class UncommentAction : BaseCommentAction
        {
            public UncommentAction(string lineComment) : base(lineComment) { }

            public override void Undo(ICodeModel model)
            {
                foreach (Position p in insertPoints)
                    model.InsertRange(p, lineComment);
            }

            public override void Redo(ICodeModel model)
            {
                foreach (Position p in insertPoints)
                    model.DeleteRange(p, new Position(p.line, p.column + lineComment.Length));
            }

            public override string Description => Rsc.UndoCmdUncomment;
        }

        private sealed class CommentAction : BaseCommentAction
        {
            public CommentAction(string lineComment) : base(lineComment) { }

            public override void Undo(ICodeModel model)
            {
                for (int i = insertPoints.Count - 1; i >= 0; i--)
                {
                    Position p = insertPoints[i];
                    model.DeleteRange(p, new Position(p.line, p.column + lineComment.Length));
                }
            }

            public override void Redo(ICodeModel model)
            {
                foreach (Position p in insertPoints)
                    model.InsertRange(p, lineComment);
            }

            public override string Description => Rsc.UndoCmdComment;
        }

        private abstract class InsertAction : UndoAction
        {
            private string text = string.Empty;

            protected InsertAction(Position start, Position end)
                : base(start, end) { }

            public override void Redo(ICodeModel model)
            {
                model.InsertRange(start, text);
                text = string.Empty;
            }

            public override void Undo(ICodeModel model)
            {
                text = model.TextRange(start, end);
                model.DeleteRange(start, end);
            }

            public sealed override int ByteCost => 20 + text.Length;
        }

        private sealed class TypeAction : InsertAction
        {
            public TypeAction(Position start, Position end)
                : base(start, end) { }

            public override bool TryMerge(UndoAction another)
            {
                if (!(another is TypeAction other))
                {
                    // Check if new action is a backspace.
                    if (another is DeleteAction delete && delete.MatchesInsertion(this.end))
                    {
                        end.column--;
                        return true;
                    }
                    else
                        return false;
                }
                else if (end.Equals(other.start))
                {
                    end = other.end;
                    return true;
                }
                else
                    return false;
            }

            public override bool TryClearIndent(ICodeModel model)
            {
                if (model.Current.Equals(this.end))
                {
                    Position newEnd = new Position(this.end.line, 0);
                    if (start.IsLesserThan(newEnd))
                    {
                        end = newEnd;
                        return true;
                    }
                }
                return false;
            }

            public override string Description => Rsc.UndoCmdTyping;
        }

        private sealed class DragCopyAction : InsertAction
        {
            public DragCopyAction(Position start, Position end)
                : base(start, end) { }

            public override string Description => Rsc.UndoCmdDragCopy;
        }

        private sealed class PasteAction : InsertAction
        {
            public PasteAction(Position start, Position end)
                : base(start, end) { }

            public override string Description => Rsc.UndoCmdPaste;
        }

        private sealed class CaseChangeAction : UndoAction
        {
            private string text;
            private readonly bool toUpper;

            public CaseChangeAction(Position start, Position end, string text, bool toUpper)
                : base(start, end)
            {
                this.text = text;
                this.toUpper = toUpper;
            }

            public override void Undo(ICodeModel model)
            {
                model.DeleteRange(start, end);
                model.InsertRange(start, text);
                text = string.Empty;
            }

            public override void Redo(ICodeModel model)
            {
                text = model.TextRange(start, end);
                model.DeleteRange(start, end);
                model.InsertRange(start,
                    toUpper ? text.ToUpperInvariant() : text.ToLowerInvariant());
            }

            public override string Description => Rsc.UndoCmdTransform;
            public override int ByteCost => 21 + text.Length;
        }

        private class ReplaceAction : UndoAction
        {
            private string text;

            public ReplaceAction(Position start, Position end, string text)
                : base(start, end) =>
                this.text = text;

            public override void Undo(ICodeModel model)
            {
                string s = model.TextRange(start, end);
                model.DeleteRange(start, end);
                model.InsertRange(start, text);
                end = model.Current;
                text = s;
            }

            public override void Redo(ICodeModel model) => Undo(model);

            public override string Description => Rsc.UndoCmdReplace;
            public override int ByteCost => 20 + text.Length;
        }

        private sealed class SnippetAction : ReplaceAction
        {
            public SnippetAction(Position start, Position end, string text)
                : base(start, end, text) { }

            public override string Description => Rsc.UndoCmdSnippet;
        }

        private sealed class IndentAction : UndoAction
        {
            private readonly int amount;

            public IndentAction(Position start, Position end, int amount)
                : base(start, end) => this.amount = amount;

            public override void Redo(ICodeModel model) => model.IndentRange(start, end, amount, null);

            public override void Undo(ICodeModel model) => model.UnindentRange(start, end, amount);

            public override string Description => Rsc.UndoCmdIndent;
            public override int ByteCost => 20;
        }

        private sealed class UnindentAction : UndoAction
        {
            private readonly int amount;
            private readonly int[] exceptions;

            public UnindentAction(Position start, Position end, int amount,
                params int[] exceptions)
                : base(start, end)
            {
                this.amount = amount;
                this.exceptions = exceptions ?? Array.Empty<int>();
            }

            public override void Redo(ICodeModel model) => model.UnindentRange(start, end, amount);

            public override void Undo(ICodeModel model) => model.IndentRange(start, end, amount, exceptions);

            public override string Description => Rsc.UndoCmdOutdent;
            public override int ByteCost => 24 + exceptions.Length;
        }
    }
}