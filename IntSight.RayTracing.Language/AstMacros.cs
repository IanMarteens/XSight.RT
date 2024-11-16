using IntSight.RayTracing.Engine;
using System.Globalization;

namespace IntSight.RayTracing.Language;

public static class XrtRegistry
{
    public sealed class Macro
    {
        private readonly List<string> formalParameters;
        private readonly IAstValue body;

        public Macro(List<string> formalParameters, IAstValue body) =>
            (this.formalParameters, this.body) = (formalParameters, body);

        public Macro Clone() => new(formalParameters, body.Clone());

        public IAstValue Expand(IAstValue[] actualParameters)
        {
            if (formalParameters.Count != actualParameters.Length)
                return null;
            Dictionary<string, IAstValue> bindings = new(
                formalParameters.Count, StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < formalParameters.Count; i++)
                bindings.Add(formalParameters[i], actualParameters[i]);
            return body.ExpandMacro(bindings);
        }
    }

    private static readonly List<Type> types = [];
    private static readonly Dictionary<string, Type> alias;
    private static readonly Dictionary<string, Macro> macros;
    private static readonly Dictionary<string, Color> colors;
    private static readonly Dictionary<string, AstUnary.Operation> functions;

    static XrtRegistry()
    {
        IEqualityComparer<string> comparer = StringComparer.InvariantCultureIgnoreCase;
        alias = new(comparer);
        macros = new(comparer);
        colors = new(comparer);
        functions = new(comparer);
        // Initialize the color table.
        foreach (KnownColor kc in Enum.GetValues<KnownColor>())
        {
            string knownName = kc.ToString();
            if (typeof(Color).GetProperty(knownName) != null)
                colors.Add(knownName, Color.FromKnownColor(kc));
        }
        // Initialize the function table.
        foreach (AstUnary.Operation value in Enum.GetValues<AstUnary.Operation>())
            if (value != AstUnary.Operation.Plus &&
                value != AstUnary.Operation.Minus)
                functions.Add(value.ToString(), value);
    }

    public static IEnumerable<Type> Types => types;

    internal static bool IsFunctionName(string functionName, out AstUnary.Operation operation) =>
        functions.TryGetValue(functionName, out operation);

    public static bool IsColorName(string colorName, out Color color) =>
        colors.TryGetValue(colorName, out color);

    public static bool IsColorName(string colorName, out string description)
    {
        if (colors.TryGetValue(colorName, out Color color))
        {
            description = string.Format(CultureInfo.InvariantCulture,
                "rgb({0:F3}, {1:F3}, {2:F3})",
                color.R  * (1F / 255F), color.G * (1F / 255F), color.B * (1F / 255F));
            return true;
        }
        description = string.Empty;
        return false;
    }

    public static bool IsColorName(string colorName) => colors.ContainsKey(colorName);

    public static void Register(Assembly assembly)
    {
        foreach (Type t in assembly.GetExportedTypes())
        {
            object[] attrs = t.GetCustomAttributes(typeof(XSightAttribute), false);
            if (attrs?.Length == 1)
            {
                types.Add(t);
                alias[t.Name] = t;
                string newAlias = ((XSightAttribute)attrs[0]).Alias;
                if (!string.IsNullOrEmpty(newAlias))
                    alias[newAlias] = t;
            }
        }
    }

    public static void Register(params Type[] typeArray)
    {
        types.AddRange(typeArray);
        foreach (Type type in typeArray)
            alias[type.Name] = type;
    }

    public static void Register(Type type, string alias)
    {
        if (!types.Contains(type))
            types.Add(type);
        XrtRegistry.alias[alias] = type;
    }

    public static Type Find(string className) =>
        alias.TryGetValue(className, out Type result) ? result : null;

    public static void AddMacro(string macro, List<string> formalParameters, IAstValue body) =>
        macros[macro] = new Macro(formalParameters, body);

    public static Macro FindMacro(string macro) =>
        macros.TryGetValue(macro, out Macro result) ? result.Clone() : null;

    public static void ClearMacros() => macros.Clear();
}