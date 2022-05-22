namespace IntSight.Controls;

public partial class CodeEditor
{
    #region Keyboard.

    /// <summary>Called when the control receives the focus.</summary>
    /// <param name="e">Can be ignored.</param>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        CreateCaret(Handle, IntPtr.Zero, 0, lineHeight);
        SetCaretPos();
    }

    /// <summary>Called when the control looses the focus.</summary>
    /// <param name="e">Can be ignored.</param>
    protected override void OnLostFocus(EventArgs e)
    {
        DestroyCaret();
        base.OnLostFocus(e);
    }

    /// <summary>Takes care of keystrokes not processed by <c>OnKeyPress</c>.</summary>
    /// <param name="e">Event data.</param>
    protected void HandleKey(KeyEventArgs e)
    {
        bool hasCtrl = (e.Modifiers & (Keys.Control | Keys.Alt)) == Keys.Control;
        bool hasShift = (e.Modifiers & (Keys.Shift | Keys.Alt)) == Keys.Shift;
        switch (e.KeyCode)
        {
            case Keys.Right:
                model.MoveRight(hasCtrl, hasShift);
                break;
            case Keys.Left:
                model.MoveLeft(hasCtrl, hasShift);
                break;
            case Keys.Down:
                if (hasCtrl)
                    if (hasShift)
                        return;
                    else
                        model.ScrollTo(ref topLine, topLine + 1, linesInPage);
                else if (e.Alt && !e.Shift)
                    model.GotoBookmark(true);
                else
                    model.MoveDown(hasShift);
                break;
            case Keys.Up:
                if (hasCtrl)
                    if (hasShift)
                        return;
                    else
                        model.ScrollTo(ref topLine, topLine - 1, linesInPage);
                else if (e.Alt && !e.Shift)
                    model.GotoBookmark(false);
                else
                    model.MoveUp(hasShift);
                break;
            case Keys.Home:
                if (hasCtrl)
                    model.MoveTo(Position.Zero, hasShift);
                else
                    model.MoveHome(hasShift);
                break;
            case Keys.End:
                if (hasCtrl)
                    model.MoveTo(Position.Infinite, hasShift);
                else
                    model.MoveEnd(hasShift);
                break;
            case Keys.PageUp:
                if (hasCtrl)
                    model.MoveTo(model.Current.ChangeLine(topLine), hasShift);
                else
                    model.MovePageUp(ref topLine, linesInPage, hasShift);
                break;
            case Keys.PageDown:
                if (hasCtrl)
                    model.MoveTo(model.Current.ChangeLine(
                        topLine + linesInPage - 2), hasShift);
                else
                    model.MovePageDown(ref topLine, linesInPage, hasShift);
                break;
            case Keys.Back:
                model.Backspace();
                break;
            case Keys.Delete:
                if (hasCtrl)
                {
                    model.MoveRight(true, true);
                    model.Delete();
                }
                else if (hasShift)
                    model.MoveToClipboard(true);
                else
                    model.Delete();
                break;
            case Keys.Enter:
                model.NewLine();
                break;
            case Keys.Insert:
                if (hasCtrl && !hasShift)
                    model.MoveToClipboard(false);
                else if (!hasCtrl && hasShift)
                    model.Paste();
                else
                    return;
                break;
            case Keys.Escape:
                if (hasCtrl || hasShift)
                    return;
                model.MoveTo(model.Current, false);
                break;
            default:
                if (e.KeyValue == '8' && e.Control)
                {
                    model.MoveToBrace(e.Shift);
                    break;
                }
                else
                    return;
        }
        e.Handled = true;
    }

    /// <summary>Called when a key is pressed or released and the control has the focus.</summary>
    /// <param name="e">Contains information about the keyboard state.</param>
    /// <remarks>This method fires the <c>KeyDown</c> event.</remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        using (model.WrapOperation())
        {
            // HandleKey must set e.Handled true when needed.
            HandleKey(e);
            // But, in any case, we must call the inherited method.
            base.OnKeyDown(e);
        }
    }

    /// <summary>Called when a key combination produces a character code.</summary>
    /// <param name="e">Contains information about the key sequence.</param>
    /// <remarks>This method fires the <c>KeyPressed</c> event.</remarks>
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        using (model.WrapOperation())
        {
            if (e.KeyChar >= 32)
            {
                model.Add(e.KeyChar);
                e.Handled = true;
                if (triggers.TryGetValue(e.KeyChar, out string[] tokens))
                    foreach (string token in tokens)
                        if (model.IsPreviousText(token, out int indent))
                        {
                            scanner.TokenTyped(this, token, indent);
                            break;
                        }
            }
            else if (e.KeyChar == '\t')
            {
                if (ModifierKeys == Keys.None)
                {
                    model.Tab();
                    e.Handled = true;
                }
                else if (ModifierKeys == Keys.Shift)
                {
                    model.ShiftTab();
                    e.Handled = true;
                }
            }
            else if (e.KeyChar == 23 && ModifierKeys == (Keys.Control | Keys.Shift))
                model.SelectWord(model.Current);   // Shift+Ctrl+W: Select word
            else if (ModifierKeys == Keys.Control)
            {
                switch ((int)e.KeyChar)
                {
                    case 1:  // Ctrl+A
                        model.SelectAll();
                        break;
                    case 2:  // Ctrl+B
                        model.ToggleBookmark();
                        break;
                    case 3:  // Ctrl+C
                        model.MoveToClipboard(false);
                        break;
                    case 10: // Ctrl+J
                        SelectSnippet();
                        break;
                    case 22: // Ctrl+V
                        model.Paste();
                        break;
                    case 24: // Ctrl+X
                        model.MoveToClipboard(true);
                        break;
                    case 25: // Ctrl+Y
                        model.Redo();
                        break;
                    case 26: // Ctrl+Z
                        model.Undo();
                        break;
                    default:
                        base.OnKeyPress(e);
                        return;
                }
                e.Handled = true;
            }
            base.OnKeyPress(e);
        }
    }

    /// <summary>Check if a keystroke must be handled by this control.</summary>
    /// <param name="keyData">Key to be checked.</param>
    /// <returns>True, if the key must be handled by this control.</returns>
    protected override bool IsInputKey(Keys keyData) =>
        Array.IndexOf(inputKeys, keyData) != -1 ||
        base.IsInputKey(keyData);

    #endregion
}

public delegate void CharAddedEventHandler(object sender, CharAddedEventArgs e);

public sealed class CharAddedEventArgs : EventArgs
{
    public char Character { get; }
    public string Prefix { get; }

    public CharAddedEventArgs(char character, string prefix) =>
        (Character, Prefix) = (character, prefix);
}
