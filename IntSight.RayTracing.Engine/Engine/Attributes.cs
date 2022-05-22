namespace IntSight.RayTracing.Engine;

/// <summary>
/// Public classes marked with XSightAttribute will be automatically registered
/// for their use by the scene compiler.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class XSightAttribute : Attribute
{
    public XSightAttribute() => Alias = string.Empty;

    public string Alias { get; set; }
}

/// <summary>
/// Marks a selected implementation for the Scene Wizard.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
public sealed class PreferredAttribute : Attribute
{
    public PreferredAttribute() { }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ProposedAttribute : Attribute
{
    public ProposedAttribute(string defaultValue) => DefaultValue = defaultValue;

    public string DefaultValue { get; }
}

/// <summary>Specifies which properties must be saved in an XML dump.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PropertiesAttribute : Attribute
{
    public PropertiesAttribute(params string[] names) => Names = names ?? Array.Empty<string>();

    public string[] Names { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ChildrenAttribute : Attribute
{
    public ChildrenAttribute(params string[] names) => Names = names ?? Array.Empty<string>();

    public string[] Names { get; }
}
