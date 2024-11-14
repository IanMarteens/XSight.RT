using IntSight.Controls;
using IntSight.Parser;
using IntSight.RayTracing.Engine;
using IntSight.RayTracing.Language;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rsc = RayEd.Properties.Resources;

namespace RayEd;

public partial class MainForm : Form
{
    private readonly AnimationForm.Parameters animationParameters = new();
    private readonly RenderProcessor renderProcessor = new();
    private Control helpParent;
    private Cursor saveCursor;
    private PixelMap lastRenderedScene;
    private string lastRenderedFileName;
    private IScene lastCompiledScene;
    private int visualStyle;

    public MainForm()
    {
        InitializeComponent();
        Instance = this;
        // Initialize the code editor
        codeEditor.CodeScanner = new SillyScanner();
        codeEditor.Reset();
        // Register exported types from the Engine assembly.
        XrtRegistry.Register(typeof(ISampler).Assembly);
        CreateInsertMenu();
        // Read XSight RT options.
        Properties.Settings settings = Properties.Settings.Default;
        codeEditor.DataBindings.Add("TabLength", settings, "TabLength");
        codeEditor.DataBindings.Add("SmartIndentation", settings, "SmartIndentation");
        codeEditor.DataBindings.Add("LeftMargin", settings, "LeftMargin");
        codeEditor.DataBindings.Add("FontName", settings, "FontName");
        codeEditor.DataBindings.Add("FontSize", settings, "FontSize");
        DataBindings.Add(nameof(VisualStyle), settings, "VisualStyle");
        // Capture the idle event.
        Application.Idle += Application_Idle;
        ActiveControl = codeEditor;
        // Initialize the render processor.
        renderProcessor.RenderProgressChanged += RenderProgressHandler;
        renderProcessor.RenderCompleted += RenderCompletedHandler;
        renderProcessor.RenderPreview +=
            (sender, e) => BmpForm.ShowPreview(e.Map, e.FromRow, e.ToRow);
        paramsPanel.RenderProcessor = renderProcessor;
    }

    public MainForm(string filename)
        : this()
    {
        if (!string.IsNullOrEmpty(filename))
            mruFileList.Add(filename);
    }

    /// <summary>Gets the singleton instance of this window.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static MainForm Instance { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int VisualStyle
    {
        get => visualStyle;
        set
        {
            ToolStripManager.RenderMode = ToolStripManagerRenderMode.Professional;
            ToolStripManager.VisualStylesEnabled = true;
            switch (value)
            {
                case 0:     // Blue
                    ToolStripManager.Renderer = new ToolStripProfessionalRenderer(
                        new BlueColorTable());
                    Instance.codeEditor.MarginColor = Color.LightSteelBlue;
                    Instance.editorContainer.Panel1.BackColor = Color.FromArgb(109, 137, 221);
                    break;
                case 1:     // Tan
                    ToolStripManager.Renderer = new ToolStripProfessionalRenderer(
                        new TanColorTable());
                    Instance.codeEditor.MarginColor = Color.FromArgb(0xe5, 0xe5, 0xd7);
                    Instance.editorContainer.Panel1.BackColor = Color.FromArgb(211, 208, 192);
                    break;
                case 2:     // Silver
                    ToolStripManager.Renderer = new ToolStripProfessionalRenderer(
                        new SilverColorTable());
                    Instance.codeEditor.MarginColor = Color.Gainsboro;
                    Instance.editorContainer.Panel1.BackColor = Color.FromArgb(0xC4, 0xCB, 0xDB);
                    break;
            }
            visualStyle = value;
        }
    }

    private void CodeEditor_FileNameChanged(object sender, EventArgs e)
    {
        string newName = codeEditor.FileName;
        if (string.IsNullOrEmpty(newName))
        {
            Text = string.Format(Rsc.MainFormCaption, Rsc.UntitledScene);
            printDocument.DocumentName = Rsc.PrintDocumentDefaultName;
        }
        else
        {
            Text = string.Format(Rsc.MainFormCaption, Path.GetFileName(newName));
            printDocument.DocumentName = newName;
        }
    }

    #region Form's lifetime management.

    private bool jitted;

    private void Application_Idle(object sender, EventArgs e)
    {
        if (!jitted)
        {
            jitted = true;
            Task.Run(() => Benchmark.Run(0, true, 4));
        }
        lineLabel.Text = string.Format(Rsc.LineFormat, codeEditor.CurrentLine);
        columnLabel.Text = string.Format(Rsc.ColumnFormat, codeEditor.CurrentColumn);
        miCut.Enabled = bnCut.Enabled =
            miCopy.Enabled = bnCopy.Enabled = codeEditor.HasSelection;
        ctCut.Enabled = ctCopy.Enabled = codeEditor.HasSelection;
        miPaste.Enabled = bnPaste.Enabled = ctPaste.Enabled = codeEditor.CanPaste;
        miUndo.Enabled = bnUndo.Enabled = ctUndo.Enabled = codeEditor.CanUndo;
        miRedo.Enabled = bnRedo.Enabled = ctRedo.Enabled = codeEditor.CanRedo;
        miSave.Enabled = bnSave.Enabled = codeEditor.Modified;
        miFolder.Enabled = !string.IsNullOrEmpty(codeEditor.FileName);
        miViewImage.Enabled = lastRenderedScene != null;
        miViewTree.Enabled = bnSceneTree.Enabled = lastCompiledScene != null;
        miViewTree.Checked = bnSceneTree.Checked = IsSceneTreeVisible;
    }

    private void MainForm_FormClosed(object sender, FormClosedEventArgs e) =>
        Application.Idle -= Application_Idle;

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        e.Cancel = !CanClose();
        if (!e.Cancel)
            if (renderProcessor.IsBusy)
            {
                renderProcessor.CancelRender();
                int t0 = Environment.TickCount;
                while (renderProcessor.IsBusy && Environment.TickCount - t0 < 3000)
                    Application.DoEvents();
            }
    }

    private bool CanClose()
    {
        if (codeEditor.Modified)
        {
            string message = string.IsNullOrEmpty(codeEditor.FileName) ?
                Rsc.MessageThisFile : Path.GetFileName(codeEditor.FileName);
            DialogResult rslt = MessageBox.Show(this,
                string.Format(Rsc.MessageThisFileHasChanged, message),
                Rsc.MessageConfirmation,
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            switch (rslt)
            {
                case DialogResult.No:
                    return true;
                case DialogResult.Cancel:
                    return false;
                default:
                    miSave.PerformClick();
                    return !codeEditor.Modified;
            }
        }
        else
            return true;
    }

    #endregion

    #region File menu command.

    private void New_Click(object sender, EventArgs e)
    {
        if (CanClose())
        {
            codeEditor.Reset();
            splitContainer.Panel2Collapsed = true;
            errors.Items.Clear();
            statusLabel.Text = string.Empty;
        }
    }

    private void Open_Click(object sender, EventArgs e)
    {
        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            mruFileList.Add(openFileDialog.FileName);
    }

    private void Save_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(codeEditor.FileName))
            miSaveAs.PerformClick();
        else
            codeEditor.Save(codeEditor.FileName);
    }

    private void SaveAs_Click(object sender, EventArgs e)
    {
        string oldName = codeEditor.FileName;
        saveFileDialog.FileName = oldName;
        if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
        {
            codeEditor.Save(saveFileDialog.FileName);
            mruFileList.Rename(oldName, codeEditor.FileName);
        }
    }

    private void Print_Click(object sender, EventArgs e) =>
        PrintPreview.Execute(printDocument, this);

    private void FileList_FileOpen(object sender, MruFileListEventArgs e)
    {
        if (string.Compare(codeEditor.FileName, e.FileName, true) != 0 && CanClose())
        {
            codeEditor.Load(e.FileName);
            splitContainer.Panel2Collapsed = true;
            errors.Items.Clear();
        }
    }

    private void Folder_Click(object sender, EventArgs e) =>
        Process.Start("explorer.exe", Path.GetDirectoryName(codeEditor.FileName));

    private void Properties_Click(object sender, EventArgs e) =>
        PropsDialog.Execute(codeEditor.FileName, codeEditor);

    private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

    #endregion

    #region Edit menu commands.

    private void Edit_DropDownOpening(object sender, EventArgs e)
    {
        miUndo.Text = codeEditor.CanUndo ? "&Undo " + codeEditor.UndoName : "&Undo";
        miRedo.Text = codeEditor.CanRedo ? "&Redo " + codeEditor.RedoName : "&Redo";
    }

    private void EditorStrip_Opening(object sender, CancelEventArgs e)
    {
        ctUndo.Text = codeEditor.CanUndo ? "&Undo " + codeEditor.UndoName : "&Undo";
        ctRedo.Text = codeEditor.CanRedo ? "&Redo " + codeEditor.RedoName : "&Redo";
    }

    private void Undo_Click(object sender, EventArgs e) => codeEditor.Undo();

    private void Redo_Click(object sender, EventArgs e) => codeEditor.Redo();

    private void Cut_Click(object sender, EventArgs e) => codeEditor.Cut();

    private void Copy_Click(object sender, EventArgs e) => codeEditor.Copy();

    private void Paste_Click(object sender, EventArgs e) => codeEditor.Paste();

    private void Delete_Click(object sender, EventArgs e) => codeEditor.Delete();

    private void SelectAll_Click(object sender, EventArgs e) => codeEditor.SelectAll();

    private void FindCompletedHandler(object sender, FindCompleteEventArgs e)
    {
        statusLabel.Text = e.Reason switch
        {
            FindResult.Found => Rsc.TextFound,
            FindResult.Replaced => Rsc.TextReplaced,
            FindResult.ReplacedAll => string.Format(Rsc.TextReplacedCount, e.Count),
            _ => Rsc.TextNotFound,
        };
    }

    private void Find_Click(object sender, EventArgs e)
    {
        statusLabel.Text = string.Empty;
        FindDialog.Show(codeEditor, FindCompletedHandler);
    }

    private void Replace_Click(object sender, EventArgs e)
    {
        statusLabel.Text = string.Empty;
        ReplaceDialog.Show(codeEditor, FindCompletedHandler);
    }

    private void Goto_Click(object sender, EventArgs e) =>
        codeEditor.ExecuteGotoLineDialog();

    private void Comment_Click(object sender, EventArgs e) =>
        codeEditor.CommentSelection(true);

    private void Uncomment_Click(object sender, EventArgs e) =>
        codeEditor.CommentSelection(false);

    #endregion

    #region View menu commands.

    private void ViewImage_Click(object sender, EventArgs e)
    {
        if (BmpForm.Instance != null)
            BmpForm.ShowForm();
        else if (lastRenderedScene != null)
            BmpForm.ShowForm(lastRenderedScene, lastRenderedFileName);
    }

    private void ViewTree_Click(object sender, EventArgs e)
    {
        if (lastCompiledScene != null)
            if (IsSceneTreeVisible)
                CloseSceneTree();
            else
                LoadSceneTree(lastCompiledScene);
    }

    private void SceneWizard_Click(object sender, EventArgs e)
    {
        AstScene scene = AstBuilder.Parse(
            new CodeEditorDocument(codeEditor).Open(), 0.0, out _);
        if (CanClose())
        {
            codeEditor.Reset();
            splitContainer.Panel2Collapsed = true;
            errors.Items.Clear();
            if (SceneWizard.Run(this, scene, out string sceneDescription))
                codeEditor.SelectedText = sceneDescription;
        }
    }

    private void Noise_Click(object sender, EventArgs e) => NoiseForm.ShowForm();

    private void Benchmarks_Click(object sender, EventArgs e) => BenchForm.ShowForm();

    #endregion

    #region Insert menu commands.

    private void CreateInsertMenu()
    {
        SnippetManager sman = new();
        foreach (var type in from t in XrtRegistry.Types
                             orderby t.Name
                             select t)
        {
            if (type.GetInterface(typeof(IShape).FullName) != null)
                miInsert.DropDownItems.Add(
                    new ToolStripMenuItem(type.Name, null,
                        (from t in type.GetConstructors()
                         select ConstructorItem(t)).ToArray()));
            foreach (ConstructorInfo constructor in type.GetConstructors())
                sman.AddSnippet(
                    GetConstructorName(constructor), GetConstructorText(constructor));
        }
        codeEditor.SnippetManager = sman;
        foreach (var group in ColorTree.GetColorList())
        {
            var item = (ToolStripMenuItem)miNamedColors.DropDownItems.Add(group.Key);
            foreach (var c in group)
            {
                item.DropDownItems.Add(c.Name, CreateImg(c)).Click += (menu, args) =>
                    { codeEditor.SelectedText = ((ToolStripMenuItem)menu).Text; };
            }
        }
    }

    private static Bitmap CreateImg(Color color)
    {
        Bitmap bmp = new(16, 16);
        using (var g = Graphics.FromImage(bmp))
        using (Brush b = new SolidBrush(color))
        {
            g.FillRectangle(b, 0, 0, 16, 16);
            g.DrawRectangle(Pens.Black, 0, 0, 15, 15);
        }
        return bmp;
    }

    private static string GetConstructorName(ConstructorInfo constructor)
    {
        StringBuilder sb = new();
        foreach (ParameterInfo param in constructor.GetParameters())
        {
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(param.Name);
        }
        sb.Append(')');
        return constructor.DeclaringType.Name + "(" + sb.ToString();
    }

    private static string GetConstructorText(ConstructorInfo constructor)
    {
        StringBuilder sb = new();
        foreach (ParameterInfo param in constructor.GetParameters())
        {
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(param.Name);
            object[] attrs = param.GetCustomAttributes(typeof(ProposedAttribute), false);
            if (attrs.Length > 0)
                sb.Append(": ").Append(((ProposedAttribute)attrs[0]).DefaultValue);
            else if (param.ParameterType == typeof(Vector))
                sb.Append(": [0,0,0]");
            else if (param.ParameterType == typeof(double))
                sb.Append(": 0.0");
            else if (param.ParameterType == typeof(int))
                sb.Append(": 0");
            else if (param.ParameterType == typeof(Pixel))
                sb.Append(": Black");
        }
        if (typeof(IMaterial).IsAssignableFrom(constructor.DeclaringType) ||
            typeof(IPigment).IsAssignableFrom(constructor.DeclaringType) ||
            typeof(IBlobItem).IsAssignableFrom(constructor.DeclaringType))
            sb.Append(')');
        else
            sb.Append(");");
        return constructor.DeclaringType.Name + "(" + sb.ToString();
    }

    private ToolStripMenuItem ConstructorItem(ConstructorInfo constructorInfo) =>
        new(GetConstructorName(constructorInfo), null,
            (s, e) => { codeEditor.SelectedText = ((ToolStripMenuItem)s).Tag.ToString(); })
        {
            Tag = GetConstructorText(constructorInfo)
        };

    private void Color_Click(object sender, EventArgs e)
    {
        string s = codeEditor.SelectedText;
        if (s.StartsWith("color ", StringComparison.InvariantCultureIgnoreCase))
            s = s[6..];
        string formatStr = "rgb({0:F2}, {1:F2}, {2:F2})";
        if (XrtRegistry.IsColorName(s, out Color c))
            colorLabel.BackColor = colorDialog.Color = c;
        else
        {
            Match match = GetRxExpr0().Match(s);
            if (match.Success)
                formatStr = "{0:F2}, {1:F2}, {2:F2}";
            else
                match = GetRxExpr1().Match(s);
            if (match.Success)
            {
                double r = double.Parse(
                    match.Groups[1].Value, CultureInfo.InvariantCulture);
                double g = double.Parse(
                    match.Groups[2].Value, CultureInfo.InvariantCulture);
                double b = double.Parse(
                    match.Groups[3].Value, CultureInfo.InvariantCulture);
                if (r > 1.0) r = 1.0;
                if (g > 1.0) g = 1.0;
                if (b > 1.0) b = 1.0;
                colorLabel.BackColor = colorDialog.Color = Color.FromArgb(
                    (int)(r * 255), (int)(g * 255), (int)(b * 255));
            }
        }
        if (colorDialog.ShowDialog(this) == DialogResult.OK)
        {
            c = colorDialog.Color;
            foreach (KnownColor kc in Enum.GetValues<KnownColor>())
                if (typeof(Color).GetProperty(kc.ToString()) != null)
                    if (Color.FromKnownColor(kc) == c)
                    {
                        codeEditor.SelectedText = kc.ToString();
                        return;
                    }
            codeEditor.SelectedText =
                formatStr.InvFormat(c.R / 255.0, c.G / 255.0, c.B / 255.0);
        }
    }

    #endregion

    #region Project menu commands.

    private void Check_Click(object sender, EventArgs e)
    {
        AstScene scene = ParseScene(out Errors parseErrors);
        if (parseErrors.Count == 0)
        {
            lastCompiledScene = scene.Scene;
            if (IsSceneTreeVisible)
                LoadSceneTree(lastCompiledScene);
        }
    }

    public static void Render() => Instance.miRender.PerformClick();

    private async void Render_Click(object sender, EventArgs e)
    {
        PrepareForRender();
        if (paramsPanel.MotionBlur)
            await renderProcessor.ExecuteAsync(
                document: new CodeEditorDocument(codeEditor),
                clock: paramsPanel.Clock,
                mode: paramsPanel.RenderMode,
                multithreading: paramsPanel.DualMode,
                rotateCamera: paramsPanel.RotateCamera,
                keepCameraHeight: paramsPanel.KeepHeight,
                samples: paramsPanel.Samples,
                width: paramsPanel.SamplingWidth);
        else
            await renderProcessor.ExecuteAsync(
                document: new CodeEditorDocument(codeEditor),
                clock: paramsPanel.Clock,
                mode: paramsPanel.RenderMode,
                multithreading: paramsPanel.DualMode,
                rotateCamera: paramsPanel.RotateCamera,
                keepCameraHeight: paramsPanel.KeepHeight);
    }

    private async void Animate_Click(object sender, EventArgs e)
    {
        ParseScene(out Errors parseErrors);
        if (parseErrors != null && parseErrors.Count > 0)
            return;
        if (string.IsNullOrEmpty(codeEditor.FileName))
        {
            animationParameters.Directory = Application.StartupPath;
            animationParameters.FileName = Rsc.UntitledScene;
        }
        else
        {
            animationParameters.Directory = Path.GetDirectoryName(codeEditor.FileName);
            animationParameters.FileName = Path.GetFileNameWithoutExtension(codeEditor.FileName);
        }
        if (AnimationForm.Execute(animationParameters))
        {
            PrepareForRender();
            await renderProcessor.ExecuteAsync(
                document: new CodeEditorDocument(codeEditor),
                mode: paramsPanel.RenderMode,
                multithreading: paramsPanel.DualMode,
                rotateCamera: paramsPanel.RotateCamera, 
                keepCameraHeight: paramsPanel.KeepHeight,
                timeShape: animationParameters.Shape,
                firstFrame: animationParameters.FirstFrame,
                lastFrame: animationParameters.LastFrame,
                totalFrames: animationParameters.TotalFrames,
                directory: animationParameters.Directory, 
                fileName: animationParameters.FileName);
        }
    }

    private void Cancel_Click(object sender, EventArgs e)
    {
        if (renderProcessor.IsBusy)
        {
            renderProcessor.CancelRender();
            int t0 = Environment.TickCount;
            while (renderProcessor.IsBusy && Environment.TickCount - t0 < 3000)
                Application.DoEvents();
        }
    }

    private void Options_Click(object sender, EventArgs e) => OptionsForm.Execute(this);

    #endregion

    #region Help menu commands.

    private void HelpToc_Click(object sender, EventArgs e)
    {
        string helpFile = Path.Combine(Application.StartupPath, "xsight.chm");
        if (File.Exists(helpFile))
        {
            if (helpParent == null)
            {
                helpParent = new Control();
                helpParent.CreateControl();
            }
            Help.ShowHelp(helpParent, helpFile, HelpNavigator.TableOfContents);
        }
    }

    private void WebSite_Click(object sender, EventArgs e) =>
        Process.Start(new ProcessStartInfo(
            "cmd", $"/c start {@"http://www.marteens.com/xsight"}"));

    private void About_Click(object sender, EventArgs e)
    {
        using var about = new About();
        about.ShowDialog();
    }

    #endregion

    #region Scene tree support.

    public static void LoadSceneTree(IScene scene)
    {
        Instance.sceneTree.LoadScene(scene);
        Instance.verticalSplitter.Panel2Collapsed = false;
        Instance.BringToFront();
    }

    public static bool IsSceneTreeVisible => !Instance.verticalSplitter.Panel2Collapsed;

    public static void CloseSceneTree() =>
        Instance.verticalSplitter.Panel2Collapsed = true;

    #endregion

    #region Parsing and error reporting.

    private static string FormatTime(int time) =>
        time <= 0 ? Rsc.ZeroSeconds :
            time < 60000 ? Rsc.FmtStrSeconds.InvFormat(time / 1000.0) :
            Rsc.FmtStrMinutes.InvFormat(time / 60000, (time % 60000) / 1000.0);

    private AstScene ParseScene(out Errors parseErrors)
    {
        statusLabel.Text = Rsc.RenderCompiling;
        statusStrip.Update();
        int parsingTime = Environment.TickCount;
        CodeEditorDocument document = new(codeEditor);
        AstScene scene = AstBuilder.Parse(
            document.Open(), paramsPanel.Clock, out parseErrors);
        parsingTime = Environment.TickCount - parsingTime;
        ShowErrors(parseErrors);
        if (parseErrors == null || parseErrors.Count == 0)
            statusLabel.ToolTipText = statusLabel.Text =
                string.Format(Rsc.RenderCompileCompleted,
                    FormatTime(parsingTime), FormatTime(scene.Scene.OptimizationTime));
        return scene;
    }

    private void ShowErrors(Errors parseErrors)
    {
        if (parseErrors != null && parseErrors.Count > 0)
        {
            errors.Items.Clear();
            if (splitContainer.Panel2Collapsed)
            {
                splitContainer.Panel2Collapsed = false;
                splitContainer.SplitterDistance =
                    Math.Max(splitContainer.Height - 64, splitContainer.SplitterDistance);
            }
            statusLabel.Text = string.Format(Rsc.RenderErrorCount, parseErrors.Count);
            foreach (Errors.Error err in parseErrors)
            {
                ListViewItem item = errors.Items.Add(err.Message);
                SourceRange pos = err.Position;
                if (pos.FromLine > 0 && pos.FromLine < int.MaxValue)
                    item.SubItems.Add(pos.ToString());
                item.Tag = pos;
            }
        }
        else
        {
            errors.Items.Clear();
            splitContainer.Panel2Collapsed = true;
            statusLabel.Visible = true;
        }
    }

    private void PrepareForRender()
    {
        statusLabel.Text = Rsc.RenderRendering;
        statusProgress.Maximum = 100;
        statusProgress.Value = 0;
        statusProgress.Visible = true;
        miRender.Enabled = false;
        miAnimate.Enabled = false;
        bnRender.Visible = false;
        bnCancel.Visible = true;
        splitContainer.Panel2Collapsed = true;
        errors.Items.Clear();
        Update();
        saveCursor = Cursor.Current;
        Cursor.Current = Cursors.WaitCursor;
        FileService.SourceFolder =
            string.IsNullOrEmpty(codeEditor.FileName)
            ? "" : Path.GetDirectoryName(codeEditor.FileName);
    }

    private void UnprepareFromRender()
    {
        Cursor.Current = saveCursor;
        saveCursor = null;
        miRender.Enabled = true;
        miAnimate.Enabled = true;
        bnRender.Visible = true;
        bnCancel.Visible = false;
        statusProgress.Visible = false;
        statusLabel.Visible = true;
    }

    private bool SelectRange(SourceRange r)
    {
        if (r.FromLine < int.MaxValue && r.FromLine > 0)
        {
            codeEditor.Select(
                new CodeEditor.Position(r.FromLine - 1, r.FromColumn - 1),
                new CodeEditor.Position(r.ToLine - 1, r.ToColumn - 1));
            return true;
        }
        else
            return false;
    }

    private void Errors_DoubleClick(object sender, EventArgs e)
    {
        if (errors.SelectedItems.Count == 1 && 
            errors.SelectedItems[0].Tag is SourceRange range && SelectRange(range))
            codeEditor.Focus();
    }

    #endregion

    #region Event handlers for the render processor.

    private void RenderCompletedHandler(object sender, RenderCompletedEventArgs e)
    {
        PixelMap map = null;
        if (!e.Cancelled)
        {
            map = e.PixelMap;
            if (map != null)
            {
                BmpForm.ShowForm(map, codeEditor.FileName);
                lastRenderedScene = map;
                lastRenderedFileName = codeEditor.FileName;
                lastCompiledScene = map.Scene;
            }
        }
        UnprepareFromRender();
        if (e.Error != null)
            e.Errors.Add(e.Error);
        ShowErrors(e.Errors);
        if (e.Cancelled)
            statusLabel.Text = Rsc.RenderCancelled;
        if (map != null)
        {
            statusLabel.ToolTipText = Rsc.RenderCompletedDetails.InvFormat(
                map.SamplesPerPixel, map.CollectionCount, map.MaxDepth);
            statusLabel.Text = Rsc.RenderCompleted.InvFormat(
                FormatTime(map.ParsingTime), FormatTime(map.RenderTime));
        }
        GC.Collect();
    }

    private void RenderProgressHandler(object sender, RenderProgressChangedEventArgs e)
    {
        RenderProgressInfo info = e.ProgressInfo;
        statusProgress.Maximum = info.Rows;
        statusProgress.Value = Math.Min(info.Completed, info.Rows);
        statusProgress.ToolTipText = info.Speed;
        statusLabel.Text = e.TotalFrames == 0
            ? string.Format(Rsc.RenderProgress1, info.Completed)
            : string.Format(Rsc.RenderProgress2, e.CurrentFrame + 1, info.Completed);
        statusLabel.ToolTipText = (((double)info.Completed) / info.Rows).ToString(
            "P", CultureInfo.InvariantCulture);
        if (info.Pixels.Incremental)
            BmpForm.ShowForm(info.Pixels);
    }

    #endregion

    #region Intellitips

    private string intelliId;
    private ToolTip intelliTip;
    private int intelliTick;

    private void CodeEditor_MouseMove(object sender, MouseEventArgs e)
    {
        if (intelliTip != null && Environment.TickCount - intelliTick > 300)
        {
            intelliTip.Hide(codeEditor);
            intelliTick = int.MaxValue;
        }
        string mouseText = codeEditor.GetMouseText();
        if (!string.IsNullOrEmpty(mouseText))
        {
            string newId = IntelliTips.GetTip(mouseText);
            if (newId != null && newId != intelliId)
            {
                intelliTimer.Enabled = false;
                intelliId = newId;
                intelliTimer.Enabled = true;
                if (XrtRegistry.IsColorName(mouseText, out Color c))
                    colorLabel.BackColor = c;
            }
            else if (mouseText == "rgb")
            {
                mouseText = codeEditor.GetMouseText(true) ?? string.Empty;
                Match match = GetRxCore0().Match(mouseText);
                if (match.Success)
                {
                    double r = double.Parse(
                        match.Groups["r"].Value, CultureInfo.InvariantCulture);
                    double g = double.Parse(
                        match.Groups["g"].Value, CultureInfo.InvariantCulture);
                    double b = double.Parse(
                        match.Groups["b"].Value, CultureInfo.InvariantCulture);
                    if (r > 1.0) r = 1.0;
                    if (g > 1.0) g = 1.0;
                    if (b > 1.0) b = 1.0;
                    colorLabel.BackColor = Color.FromArgb(
                        (byte)(int)(r * 255), (byte)(int)(g * 255), (byte)(int)(b * 255));
                }
                else
                {
                    match = GetRxCore1().Match(mouseText);
                    if (match.Success)
                    {
                        double v = double.Parse(
                            match.Groups["v"].Value, CultureInfo.InvariantCulture);
                        if (v > 1.0) v = 1.0;
                        colorLabel.BackColor = Color.FromArgb(
                            (byte)(int)(v * 255), (byte)(int)(v * 255), (byte)(int)(v * 255));
                    }
                }
            }
        }
    }

    private void CodeEditor_MouseLeave(object sender, EventArgs e)
    {
        if (intelliTip != null)
        {
            intelliTip.Hide(codeEditor);
            intelliTick = int.MaxValue;
        }
        intelliTimer.Enabled = false;
    }

    private void IntelliTimer_Tick(object sender, EventArgs e)
    {
        intelliTimer.Enabled = false;
        intelliTip ??= new(components) { InitialDelay = 100 };
        Point p = codeEditor.PointToClient(MousePosition);
        p.Offset(10, 0);
        intelliTip.Show(intelliId, codeEditor, p);
        intelliTick = Environment.TickCount;
    }

    private const string RxCore0 = @"\s*(\d\.\d*)\s*\,\s*(\d\.\d*)\s*\,\s*(\d\.\d*)\s*";
    private const string RxCore1 = @"rgb\s*\(" + RxCore0 + @"\)\s*";
    private const string RxExpr0 = "^" + RxCore0 + "$";
    private const string RxExpr1 = "^" + RxCore1 + "$";

    [GeneratedRegex(RxExpr0)]
    private static partial Regex GetRxExpr0();
    [GeneratedRegex(RxExpr1)]
    private static partial Regex GetRxExpr1();
    [GeneratedRegex("^rgb\\s*\\(\\s*(?'r'\\d(\\.\\d*)?)\\s*\\,\\s*(?'g'\\d(\\.\\d*)?)\\s*\\,\\s*(?'b'\\d(\\.\\d*)?)\\s*\\)")]
    private static partial Regex GetRxCore0();
    [GeneratedRegex("^rgb\\s*(?'v'\\d(\\.\\d*)?)")]
    private static partial Regex GetRxCore1();

    #endregion
}