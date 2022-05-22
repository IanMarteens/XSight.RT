using System.Runtime.Intrinsics.X86;
using System.Threading;

namespace RayEd;

/// <summary>The root object for the application.</summary>
internal static class Program
{
    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    private static void Main(params string[] args)
    {
        Application.EnableVisualStyles();
#if USE_SSE
        if (!Avx.IsSupported || !Fma.IsSupported)
        {
            MessageBox.Show(
                text: "This application requires AVX/FMA support.",
                caption: "Error",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            return;
        }
#endif
        Application.ThreadException += Application_ThreadException;
        string filename = null;
        if (args != null && args.Length > 0)
        {
            filename = args[0];
            if (string.IsNullOrEmpty(filename))
                filename = null;
        }
        AsyncFlowControl flow = ExecutionContext.SuppressFlow();
        Application.Run(new MainForm(filename));
        flow.Undo();
    }

    /// <summary>Shows an error message.</summary>
    /// <param name="message">The text from the error message.</param>
    public static void ShowException(string message) =>
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

    /// <summary>Traps all unhandled exceptions.</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Information about the exception.</param>
    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) =>
        ShowException(e.Exception.Message + "\n" + e.Exception.StackTrace);
}