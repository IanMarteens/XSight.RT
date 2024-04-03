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
public sealed class ProposedAttribute(string defaultValue) : Attribute
{
    public string DefaultValue { get; } = defaultValue;
}

/// <summary>Specifies which properties must be saved in an XML dump.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PropertiesAttribute(params string[] names) : Attribute
{
    public string[] Names { get; } = names ?? [];
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ChildrenAttribute(params string[] names) : Attribute
{
    public string[] Names { get; } = names ?? [];
}
