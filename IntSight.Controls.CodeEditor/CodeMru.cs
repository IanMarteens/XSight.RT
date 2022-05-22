using System.ComponentModel;
using Rsc = IntSight.Controls.Properties.Resources;

namespace IntSight.Controls;

/// <summary>
/// Manages a MRU file list stored in the Windows registry.
/// </summary>
public class MruFileList : Component, ISupportInitialize
{
    private const int DefaultCapacity = 8;

    private readonly List<string> fileList = new List<string>();
    private string keyName = string.Empty;
    private string valueName = string.Empty;
    private int capacity = DefaultCapacity;
    private ToolStripMenuItem menuItem;
    private bool initializing, loaded;

    public MruFileList()
    {
    }

    public MruFileList(IContainer container)
    {
        container.Add(this);
    }

    public event MruFileListEventHandler FileOpen;

    [Browsable(true)]
    [DefaultValue(DefaultCapacity)]
    public int Capacity
    {
        get { return capacity; }
        set
        {
            if (value <= 0)
                throw new ArgumentException(
                    string.Format(Rsc.MruListCapacityTooLow, value));
            if (value < capacity)
            {
                while (fileList.Count > capacity)
                    fileList.RemoveAt(fileList.Count - 1);
                if (menuItem != null)
                    while (menuItem.DropDownItems.Count > capacity)
                        DeleteItem(menuItem.DropDownItems.Count - 1);
                if (loaded)
                    SaveValuesToRegistry();
            }
            capacity = value;
        }
    }

    [Browsable(true)]
    public string KeyName
    {
        get { return keyName; }
        set
        {
            keyName = value;
            CheckIfLoaded();
        }
    }

    [Browsable(true)]
    public ToolStripMenuItem MenuItem
    {
        get { return menuItem; }
        set
        {
            if (loaded && menuItem != value)
            {
                if (menuItem != null)
                    menuItem.DropDownItems.Clear();
                fileList.Clear();
                loaded = false;
            }
            menuItem = value;
            CheckIfLoaded();
        }
    }

    [Browsable(true)]
    public string ValueName
    {
        get { return valueName; }
        set
        {
            valueName = value;
            CheckIfLoaded();
        }
    }

    /// <summary>
    /// Adds a new file name to the list.
    /// If the file exists, it's just moved to the first position.
    /// </summary>
    /// <param name="fileName">The name of the file being used.</param>
    public void Add(string fileName)
    {
        Add(fileName, null);
    }

    /// <summary>
    /// Adds a new file name to the list.
    /// If the file exists, it's just moved to the first position.
    /// </summary>
    /// <param name="fileName">The name of the file being used.</param>
    /// <param name="data">Extra information for the FileOpen event.</param>
    public void Add(string fileName, object data)
    {
        if (loaded)
        {
            OnFileOpen(fileName, data);
            MoveItemTop(fileName);
        }
    }

    protected virtual void OnFileOpen(string fileName, object data)
    {
        FileOpen?.Invoke(this, new MruFileListEventArgs(fileName, data));
    }

    public void Rename(string oldName, string newName)
    {
        if (!string.IsNullOrEmpty(oldName))
        {
            int idx = fileList.IndexOf(oldName);
            if (idx != -1)
            {
                fileList.RemoveAt(idx);
                DeleteItem(idx);
            }
        }
        MoveItemTop(newName);
    }

    private void MoveItemTop(string fileName)
    {
        ToolStripMenuItem item;
        int i = fileList.IndexOf(fileName);
        if (i == -1)
        {
            item = CreateItem(fileName);
            if (fileList.Count == capacity)
            {
                fileList.RemoveAt(capacity - 1);
                DeleteItem(capacity - 1);
            }
        }
        else
        {
            item = menuItem.DropDownItems[i] as ToolStripMenuItem;
            menuItem.DropDownItems.RemoveAt(i);
            fileList.RemoveAt(i);
        }
        menuItem.DropDownItems.Insert(0, item);
        fileList.Insert(0, fileName);
        menuItem.Enabled = true;
        SaveValuesToRegistry();
    }

    private void FileOpenHandler(object sender, EventArgs e)
    {
        ToolStripMenuItem item = sender as ToolStripMenuItem;
        string fileName = item.Text;
        fileList.Remove(fileName);
        menuItem.DropDownItems.Remove(item);
        if (System.IO.File.Exists(fileName))
        {
            OnFileOpen(fileName, null);
            fileList.Insert(0, fileName);
            menuItem.DropDownItems.Insert(0, item);
        }
        SaveValuesToRegistry();
    }

    private void SaveValuesToRegistry()
    {
        if (OperatingSystem.IsWindows())
            Microsoft.Win32.Registry.SetValue(
                keyName, valueName, fileList.ToArray());
    }

    private void CheckIfLoaded()
    {
        if (!loaded && !initializing && menuItem != null &&
            !string.IsNullOrEmpty(keyName) && !string.IsNullOrEmpty(valueName) &&
            !DesignMode)
        {
            if (OperatingSystem.IsWindows() &&
                Microsoft.Win32.Registry.GetValue(
                keyName, valueName, Array.Empty<string>()) is string[] values)
            {
                for (int i = 0; i < Math.Min(capacity, values.Length); i++)
                    fileList.Add(values[i]);
                menuItem.DropDownItems.Clear();
                foreach (string fileName in fileList)
                    menuItem.DropDownItems.Add(CreateItem(fileName));
                menuItem.Enabled = menuItem.DropDownItems.Count > 0;
            }
            loaded = true;
        }
    }

    protected virtual ToolStripMenuItem CreateItem(string fileName) =>
        new(fileName, null, FileOpenHandler);

    protected virtual void DeleteItem(int itemPosition)
    {
        ToolStripItem item = menuItem.DropDownItems[itemPosition];
        menuItem.DropDownItems.RemoveAt(itemPosition);
        item.Dispose();
    }

    #region Miembros de ISupportInitialize

    void ISupportInitialize.BeginInit()
    {
        this.initializing = true;
    }

    void ISupportInitialize.EndInit()
    {
        this.initializing = false;
        CheckIfLoaded();
    }

    #endregion
}

public delegate void MruFileListEventHandler(object sender, MruFileListEventArgs e);

public class MruFileListEventArgs : EventArgs
{
    public MruFileListEventArgs(string fileName, object data)
    {
        FileName = fileName;
        Data = data;
    }

    public string FileName { get; }
    public object Data { get; }
}
