namespace IntSight.Controls;

public delegate void SelectionChangedEventHandler(
    object sender, SelectionChangedEventArgs e);

public class SelectionChangedEventArgs(
    CodeEditor.Position originalFrom, CodeEditor.Position originalTo) : EventArgs
{
    private CodeEditor.Position originalFrom = originalFrom, originalTo = originalTo;

    internal void Reset(CodeEditor.Position originalFrom, CodeEditor.Position originalTo)
    {
        this.originalFrom = originalFrom;
        this.originalTo = originalTo;
    }

    public CodeEditor.Position OriginalFrom => originalFrom;
    public CodeEditor.Position OriginalTo => originalTo;
}
