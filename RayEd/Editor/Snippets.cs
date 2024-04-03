using IntSight.Controls.CodeModel;
using IntSight.RayTracing.Language;

namespace RayEd;

internal class MemorySnippet : ICodeSnippet
{
    public MemorySnippet(string name, string text) =>
        (Name, Text) = (name, text);

    #region ICodeSnippet Members

    string ICodeSnippet.Description => Name;
    public string Name { get; }
    public string Text { get; }

    #endregion
}

internal class SnippetManager : ISnippetManager
{
    private readonly List<ICodeSnippet> snippets;
    private bool isSorted;

    public SnippetManager() => snippets = [];

    public void AddSnippet(string name, string text) =>
        snippets.Add(new MemorySnippet(name, text));

    #region ISnippetManager Members

    ICodeSnippet[] ISnippetManager.GetSnippets(string partialName)
    {
        if (!isSorted)
        {
            snippets.Sort((x, y) => x.Name.CompareTo(y.Name));
            isSorted = true;
        }
        partialName = partialName.ToLowerInvariant();
        List<ICodeSnippet> result = [];
        foreach (ICodeSnippet snippet in snippets)
            if (snippet.Name.StartsWith(partialName, StringComparison.InvariantCultureIgnoreCase))
                result.Add(snippet);
        return [.. result];
    }

    void ISnippetManager.Refresh() { }

    void ISnippetManager.Select(ICodeSnippet[] snippets, Control editor,
        int x, int y, bool isSurround, ICodeSnippetCallback callback) =>
        SnippetBox.Select(snippets, editor, x, y, isSurround, callback);

    #endregion
}

internal static class IntelliTips
{
    private static Dictionary<Type, string> tips;

    public static string GetTip(string className)
    {
        if (tips == null)
        {
            tips = [];
            foreach (Type t in XrtRegistry.Types)
                tips.Add(t, GetTips(t));
        }
        Type type = XrtRegistry.Find(className);
        if (type != null && tips.TryGetValue(type, out string result))
            return result;
        // Check if we have a color name.
        else if (XrtRegistry.IsColorName(className, out result))
            return result;
        else
            return null;
    }

    private static string GetTips(Type t)
    {
        StringBuilder sb = new();
        List<string> added = [];
        foreach (ConstructorInfo info in t.GetConstructors())
        {
            sb.Length = 0;
            foreach (ParameterInfo paramInfo in info.GetParameters())
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(paramInfo.Name);
            }
            string line = t.Name + "(" + sb.Append(')').ToString();
            if (!added.Contains(line))
                added.Add(line);
        }
        sb.Length = 0;
        foreach (string line in added)
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(line);
        }
        return sb.ToString();
    }
}
