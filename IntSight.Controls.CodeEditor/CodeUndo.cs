using IntSight.Controls.CodeModel;
using Rsc = IntSight.Controls.Properties.Resources;

namespace IntSight.Controls;

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

    private sealed class DeleteAction(CodeEditor.Position start, CodeEditor.Position end, string text)
        : UndoAction(start, end)
    {
        private string text = text;

        public bool MatchesInsertion(Position iEnd) =>
            iEnd.Equals(end) && start.line == end.line && start.column == end.column - 1;

        public override bool TryMerge(UndoAction another)
        {
            if (another is not DeleteAction other)
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

    private sealed class DragMoveAction(CodeEditor.Position start, CodeEditor.Position end,
        CodeEditor.Position startText, CodeEditor.Position endText, string text) : UndoAction(start, end)
    {
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

    private abstract class BaseCommentAction(string lineComment)
        : UndoAction(Position.Infinite, Position.Infinite)
    {
        protected List<Position> insertPoints = [];
        protected string lineComment = lineComment;

        public void Add(Position position)
        {
            insertPoints.Add(position);
            if (position.IsLesserThan(start))
                start = position;
        }

        public sealed override int ByteCost => 20 + 8 * insertPoints.Count;
    }

    private sealed class UncommentAction(string lineComment)
        : BaseCommentAction(lineComment)
    {
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

    private sealed class CommentAction(string lineComment)
        : BaseCommentAction(lineComment)
    {
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

    private abstract class InsertAction(Position start, Position end)
        : UndoAction(start, end)
    {
        private string text = string.Empty;

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

    private sealed class TypeAction(Position start, Position end) : InsertAction(start, end)
    {
        public override bool TryMerge(UndoAction another)
        {
            if (another is not TypeAction other)
            {
                // Check if new action is a backspace.
                if (another is DeleteAction delete && delete.MatchesInsertion(end))
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
            if (model.Current.Equals(end))
            {
                Position newEnd = new(end.line, 0);
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

    private sealed class DragCopyAction(Position start, Position end) : InsertAction(start, end)
    {
        public override string Description => Rsc.UndoCmdDragCopy;
    }

    private sealed class PasteAction(Position start, Position end) : InsertAction(start, end)
    {
        public override string Description => Rsc.UndoCmdPaste;
    }

    private sealed class CaseChangeAction(Position start, Position end, string text, bool toUpper)
        : UndoAction(start, end)
    {
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

    private class ReplaceAction(Position start, Position end, string text)
        : UndoAction(start, end)
    {
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

    private sealed class SnippetAction(Position start, Position end, string text)
        : ReplaceAction(start, end, text)
    {
        public override string Description => Rsc.UndoCmdSnippet;
    }

    private sealed class IndentAction(Position start, Position end, int amount)
        : UndoAction(start, end)
    {
        public override void Redo(ICodeModel model) => model.IndentRange(start, end, amount, null);

        public override void Undo(ICodeModel model) => model.UnindentRange(start, end, amount);

        public override string Description => Rsc.UndoCmdIndent;
        public override int ByteCost => 20;
    }

    private sealed class UnindentAction(Position start, Position end, int amount,
        params int[] exceptions) : UndoAction(start, end)
    {
        private readonly int[] exceptions = exceptions ?? [];

        public override void Redo(ICodeModel model) => model.UnindentRange(start, end, amount);

        public override void Undo(ICodeModel model) => model.IndentRange(start, end, amount, exceptions);

        public override string Description => Rsc.UndoCmdOutdent;
        public override int ByteCost => 24 + exceptions.Length;
    }
}