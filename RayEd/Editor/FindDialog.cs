using IntSight.Controls;

namespace RayEd;

public partial class FindDialog : FindBase
{
    private static FindDialog instance;

    public static void Show(CodeEditor editor, EventHandler<FindCompleteEventArgs> findComplete)
    {
        if (instance == null)
        {
            ReplaceDialog.Shutdown();
            instance = new(editor, findComplete);
            instance.Show(editor.FindForm());
        }
    }

    protected FindDialog() => InitializeComponent();

    public FindDialog(CodeEditor editor, EventHandler<FindCompleteEventArgs> findComplete)
        : base(editor, findComplete)
    {
        InitializeComponent();
        RestoreValues();
    }

    public static void Shutdown() => instance?.Close();

    private void FindDialog_FormClosed(object sender, FormClosedEventArgs e)
    {
        SaveValues();
        instance = null;
    }

    private void FindDialog_KeyDown(object sender, KeyEventArgs e)
    {
        if ((e.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt)) == Keys.Control &&
            e.KeyCode == Keys.R)
        {
            ReplaceDialog.Show(editor, findComplete);
            e.Handled = true;
        }
    }
}