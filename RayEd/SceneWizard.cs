using IntSight.RayTracing.Engine;
using IntSight.RayTracing.Language;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace RayEd;

public partial class SceneWizard : Form
{
    private readonly AstScene sceneTree;

    public SceneWizard()
    {
        InitializeComponent();
        foreach (Type t in XrtRegistry.Types)
        {
            if (t.GetInterface(typeof(ISampler).FullName) != null)
                cbSampler.Items.Add(new TypeItem(t));
            else if (t.GetInterface(typeof(ICamera).FullName) != null)
                cbCamera.Items.Add(new TypeItem(t));
            else if (t.GetInterface(typeof(IBackground).FullName) != null)
                cbBackground.Items.Add(new TypeItem(t));
            else if (t.GetInterface(typeof(IAmbient).FullName) != null)
                cbAmbient.Items.Add(new TypeItem(t));
            else if (t.GetInterface(typeof(ILight).FullName) != null)
                cbLight.Items.Add(new TypeItem(t));
        }
        cbSampler.SelectedIndex = 0;
        cbCamera.SelectedIndex = 0;
        cbBackground.SelectedIndex = 0;
        cbAmbient.SelectedIndex = 0;
        cbLight.SelectedIndex = 0;
    }

    private SceneWizard(AstScene sceneTree)
        : this()
    {
        this.sceneTree = sceneTree;
        if (sceneTree != null)
        {
            SelectType(cbSampler, sceneTree.Sampler);
            SelectType(cbCamera, sceneTree.Camera);
            SelectType(cbAmbient, sceneTree.Ambients[0]);
            SelectType(cbBackground, sceneTree.Background);
        }
    }

    private static void SelectType(ComboBox combo, IAstValue obj)
    {
        if (obj != null)
        {
            Type t = obj.Value.GetType();
            for (int i = 1; i < combo.Items.Count; i++)
                if (((TypeItem)combo.Items[i]).Type == t)
                {
                    combo.SelectedIndex = i;
                    return;
                }
        }
    }

    internal class TypeItem
    {
        public TypeItem(Type type) => Type = type;

        public Type Type { get; }

        public override string ToString() => Type.Name;
    }

    public static bool Run(Form parent, AstScene sceneTree, out string scene)
    {
        using SceneWizard form = new(sceneTree);
        if (form.ShowDialog(parent) == DialogResult.OK)
        {
            scene = form.GetScene(form.bxNamedParameters.Checked);
            return true;
        }
        else
        {
            scene = string.Empty;
            return false;
        }
    }

    private static ConstructorInfo Select(ConstructorInfo[] constructors)
    {
        foreach (ConstructorInfo info in constructors)
            if (info.GetCustomAttributes(typeof(PreferredAttribute), false).Length > 0)
                return info;
        return constructors[0];
    }

    private static bool GetValueFromPrototype(ParameterInfo pInfo,
        IAstValue prototype, StringBuilder sb)
    {
        if (prototype == null)
            return false;
        object instance = prototype.Value;
        if (instance == null)
            return false;
        PropertyInfo property = instance.GetType().GetProperty(pInfo.Name,
            BindingFlags.Public | BindingFlags.Static |
            BindingFlags.Instance | BindingFlags.IgnoreCase,
            null, pInfo.ParameterType, Type.EmptyTypes, null);
        if (property != null)
            if (pInfo.ParameterType == typeof(Vector))
            {
                Vector v = (Vector)property.GetValue(instance, null);
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "[{0:F2},{1:F2},{2:F2}]", v.X, v.Y, v.Z);
                return true;
            }
            else if (pInfo.ParameterType == typeof(double))
            {
                double d = Convert.ToDouble(property.GetValue(instance, null));
                sb.Append(d.ToString("F2", CultureInfo.InvariantCulture));
                return true;
            }
            else if (pInfo.ParameterType == typeof(int))
            {
                int i = Convert.ToInt32(property.GetValue(instance, null));
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                return true;
            }
            else if (pInfo.ParameterType == typeof(Pixel))
            {
                Pixel p = (Pixel)property.GetValue(instance, null);
                Color c = p;
                foreach (KnownColor kc in Enum.GetValues(typeof(KnownColor)))
                    if (typeof(Color).GetProperty(kc.ToString()) != null)
                        if (Color.FromKnownColor(kc) == c)
                        {
                            sb.Append(kc.ToString());
                            return true;
                        }
                if (p.Red == p.Green && p.Green == p.Blue)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "rgb {0:F3}", p.Red);
                else
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "rgb({0:F3},{1:F3},{2:F3})", p.Red, p.Green, p.Blue);
                return true;
            }
        return false;
    }

    private static void GenerateConstructor(
        StringBuilder sb, string section, bool namedParameters,
        ComboBox combo, IAstValue prototype)
    {
        if (combo.SelectedItem is not TypeItem item)
            return;
        Type t = item.Type;
        sb.AppendLine(section);
        sb.Append("    ");
        if (t == typeof(ConstantAmbient))
            if (prototype == null)
            {
                sb.AppendLine("0.10;\r\n");
                return;
            }
            else
            {
                if (prototype.Value is ConstantAmbient amb)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        "{0:F3};\r\n\r\n", amb.Intensity);
                    return;
                }
            }
        sb.Append(t.Name).Append('(');
        ConstructorInfo[] ci = t.GetConstructors();
        if (ci.Length > 0)
        {
            int i = 0;
            foreach (ParameterInfo pi in Select(ci).GetParameters())
            {
                if (i++ > 0)
                    if (!pi.ParameterType.IsArray)
                        sb.Append(", ");
                if (namedParameters)
                {
                    sb.Append(pi.Name);
                    sb.Append(": ");
                }
                if (!GetValueFromPrototype(pi, prototype, sb))
                {
                    object[] attrs =
                        pi.GetCustomAttributes(typeof(ProposedAttribute), false);
                    if (attrs.Length > 0)
                        sb.Append(((ProposedAttribute)attrs[0]).DefaultValue);
                    else if (pi.ParameterType == typeof(Vector))
                        sb.Append("^0");
                    else if (pi.ParameterType == typeof(Pixel))
                        sb.Append("White");
                    else if (pi.ParameterType == typeof(double))
                        sb.Append("0.0");
                    else if (pi.ParameterType == typeof(int))
                        sb.Append('0');
                    else if (pi.ParameterType == typeof(string))
                        sb.Append("''");
                    else if (!pi.ParameterType.IsArray)
                        sb.Append("???");
                }
            }
        }
        sb.AppendLine(");\r\n");
    }

    private string GetScene(bool namedParameters)
    {
        StringBuilder sb = new(1024);
        IAstValue prototype = sceneTree?.Sampler;
        GenerateConstructor(sb, "sampler", namedParameters, cbSampler, prototype);
        prototype = sceneTree?.Camera;
        GenerateConstructor(sb, "camera", namedParameters, cbCamera, prototype);
        prototype = sceneTree?.Background;
        GenerateConstructor(sb, "background", namedParameters, cbBackground, prototype);
        prototype = sceneTree?.Ambients[0];
        GenerateConstructor(sb, "ambient", namedParameters, cbAmbient, prototype);
        GenerateConstructor(sb, "lights", namedParameters, cbLight, null);
        sb.Append("objects\r\n\r\nend.\r\n");
        return sb.ToString();
    }
}