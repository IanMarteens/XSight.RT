using System.Reflection;
using System.Runtime.Versioning;

namespace RayEd;

partial class About : Form
{
    public About()
    {
        InitializeComponent();
        txVersion.Text = string.Format(
            Properties.Resources.VersionLabel, 
            new AssemblyName(Assembly.GetExecutingAssembly().FullName)
            .Version.ToString(3));
        txFramework.Text = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
    }

    private void LinkLabel1_Click(object sender, EventArgs e) => Close();

    private void About_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)27)
            Close();
    }

    private const int WM_NCHITTEST = 0x0084;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_NCHITTEST)
            if (m.Result == (IntPtr)1)
                if (GetChildAtPoint(PointToClient(Control.MousePosition)) != txClose)
                    m.Result = (IntPtr)2;           
    }

    private void Close_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => Close();
}