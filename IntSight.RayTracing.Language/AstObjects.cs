using IntSight.Parser;
using IntSight.RayTracing.Engine;
using Rsc = IntSight.RayTracing.Language.Properties.Resources;

namespace IntSight.RayTracing.Language;

internal class AstObject : AstValue, IAstValue
{
    protected readonly string classname;
    protected readonly Type classtype;
    protected Type verifiedAs;
    private ConstructorInfo ctor;
    protected List<IAstValue> parameters;

    /// <summary>Initializes an object given it's class descriptor.</summary>
    /// <param name="errors">Error list.</param>
    /// <param name="position">Position in source.</param>
    /// <param name="classtype">Class descriptor.</param>
    /// <param name="parameters">The parameter list.</param>
    protected AstObject(Errors errors, SourceRange position,
        Type classtype, object parameters)
    {
        classname = classtype.Name;
        this.classtype = classtype;
        Position = position;
        this.parameters = (List<IAstValue>)parameters;
        // There cannot be any positional parameters after a named parameter
        bool positional = true;
        for (int i = 0; i < this.parameters.Count; i++)
            if (!string.IsNullOrEmpty(this.parameters[i].Name))
                positional = false;
            else if (!positional)
            {
                errors.Add(position, Rsc.ParserInvalidPositional, classname);
                break;
            }
    }

    public AstObject(SourceRange position, string classname,
        params IAstValue[] parameters)
    {
        this.classname = classname;
        classtype = XrtRegistry.Find(this.classname);
        Position = position;
        this.parameters = new List<IAstValue>(parameters);
    }

    private int GetNamedParameter(int from, string name)
    {
        while (from < parameters.Count)
            if (string.Compare(name, parameters[from].Name, true) == 0)
                return from;
            else
                from++;
        return -1;
    }

    private bool ConstructorMatchs(ParameterInfo[] paramsInfo)
    {
        for (int i = 0; i < paramsInfo.Length; i++)
        {
            if (!string.IsNullOrEmpty(parameters[i].Name))
            {
                int j = GetNamedParameter(i, paramsInfo[i].Name);
                if (j < 0) return false;
                (parameters[j], parameters[i]) = (parameters[i], parameters[j]);
            }
            if (!parameters[i].IsA(paramsInfo[i].ParameterType))
                return false;
        }
        return true;
    }

    #region IAstValue members

    IAstValue IAstValue.Clone()
    {
        AstObject other = (AstObject)MemberwiseClone();
        other.parameters = [];
        foreach (IAstValue p in parameters)
            other.parameters.Add(p.Clone());
        return other;
    }

    bool IAstValue.IsA(Type type) =>
        classtype != null &&
        (type.IsAssignableFrom(classtype) ||
        typeof(IPigment).IsAssignableFrom(classtype) && type == typeof(IMaterial));

    public virtual object Value
    {
        get
        {
            object[] arguments = new object[parameters.Count];
            for (int i = 0; i < arguments.Length; i++)
                arguments[i] = parameters[i].Value;
            if (verifiedAs == typeof(IBlobItem))
                if (classtype == typeof(Rotate))
                {
                    Vector rotation = (Vector)arguments[1];
                    return ((IBlobItem)arguments[0]).ApplyRotation(
                        Matrix.Rotation(rotation.X, rotation.Y, rotation.Z));
                }
                else if (classtype == typeof(Translate))
                    return ((IBlobItem)arguments[0]).ApplyTranslation(
                        (Vector)arguments[1]);
                else if (classtype == typeof(Repeater))
                    return new BlobRepeater(
                        (IBlobItem)arguments[2],
                        Convert.ToInt32(arguments[0]),
                        (Vector)arguments[1], Vector.Null);
            object result = ctor.Invoke(arguments);
            if (verifiedAs == typeof(IMaterial) &&
                !verifiedAs.IsAssignableFrom(classtype))
                return new DefaultPigment((IPigment)result);
            else
                return result;
        }
    }

    public virtual bool Verify(Type type, Errors errors)
    {
        if (classtype == null)
        {
            errors.Add(Position, Rsc.ParserClassNotFound, classname);
            return false;
        }
        if (!((IAstValue)this).IsA(type))
            if (type == typeof(IBlobItem) &&
                (classtype == typeof(Rotate) || classtype == typeof(Translate)))
            {
                if (parameters.Count != 2 ||
                    !parameters[0].Verify(typeof(IBlobItem), errors) ||
                    !parameters[1].Verify(typeof(Vector), errors))
                {
                    errors.Add(Position, Rsc.ParserConstructorNotFound, classname);
                    return false;
                }
                verifiedAs = type;
                return true;
            }
            else if (type == typeof(IBlobItem) && classtype == typeof(Repeater))
            {
                if (parameters.Count != 3 ||
                    !parameters[0].Verify(typeof(int), errors) ||
                    !parameters[1].Verify(typeof(Vector), errors) ||
                    !parameters[2].Verify(typeof(IBlobItem), errors))
                {
                    errors.Add(Position, Rsc.ParserConstructorNotFound, classname);
                    return false;
                }
                verifiedAs = type;
                return true;
            }
            else
            {
                errors.Add(Position, Rsc.ParserNotImplemented, classname, type.Name);
                return false;
            }
        verifiedAs = type;
        foreach (ConstructorInfo ci in classtype.GetConstructors())
        {
            ParameterInfo[] paramsInfo = ci.GetParameters();
            if (paramsInfo.Length == parameters.Count && ConstructorMatchs(paramsInfo))
            {
                for (int i = 0; i < paramsInfo.Length; i++)
                    if (!parameters[i].Verify(paramsInfo[i].ParameterType, errors))
                        return false;
                ctor = ci;
                return true;
            }
        }
        if (classtype.GetConstructors().Length == 1)
            errors.Add(Position, Rsc.ParserParamMismatch, classname);
        else
            errors.Add(Position, Rsc.ParserConstructorNotFound, classname);
        return false;
    }

    protected IAstValue ExpandParameters(Dictionary<string, IAstValue> bindings)
    {
        for (int i = 0; i < parameters.Count; i++)
            parameters[i] = parameters[i].ExpandMacro(bindings);
        return this;
    }

    public virtual IAstValue ExpandMacro(Dictionary<string, IAstValue> bindings) =>
        parameters.Count == 0 ? bindings.TryGetValue(classname, out IAstValue result) ?
            result : this : ExpandParameters(bindings);

    public override string ToString()
    {
        if (parameters.Count == 0)
            return classname;
        var sb = new StringBuilder(classname).Append('(');
        for (int i = 0; i < parameters.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(parameters[i].ToString());
        }
        return sb.Append(')').ToString();
    }

    #endregion
}

internal sealed class AstUnion(Errors errors, SourceRange position, object parameters)
    : AstObject(errors, position, typeof(Union), parameters), IAstValue
{
    #region IAstValue members

    public override object Value
    {
        get
        {
            if (parameters.Count == 1)
                return (IShape)parameters[0].Value;
            IShape[] arguments = new IShape[parameters.Count];
            for (int i = 0; i < arguments.Length; i++)
                arguments[i] = (IShape)parameters[i].Value;
            return new Union(arguments);
        }
    }

    public override bool Verify(Type type, Errors errors)
    {
        if (!type.IsAssignableFrom(classtype))
        {
            errors.Add(Position, Rsc.ParserIncompatibleUnion, type.Name);
            return false;
        }
        foreach (IAstValue p in parameters)
            if (!p.Verify(typeof(IShape), errors))
                return false;
        return true;
    }

    public override IAstValue ExpandMacro(Dictionary<string, IAstValue> bindings) =>
        ExpandParameters(bindings);

    #endregion
}

internal sealed class AstBlob(Errors errors, SourceRange position, object parameters)
    : AstObject(errors, position, typeof(Blob), parameters), IAstValue
{
    #region IAstValue members.

    public override object Value
    {
        get
        {
            IBlobItem[] items = new IBlobItem[parameters.Count - 2];
            for (int i = 0; i < items.Length; i++)
                items[i] = (IBlobItem)parameters[i].Value;
            return new Blob(items,
                Convert.ToDouble(parameters[^2].Value),
                (IMaterial)parameters[^1].Value);
        }
    }

    public override bool Verify(Type type, Errors errors)
    {
        if (!type.IsAssignableFrom(classtype))
        {
            errors.Add(Position, Rsc.ParserIncompatibleBlob, type.Name);
            return false;
        }
        if (parameters.Count < 3)
        {
            errors.Add(Position, Rsc.ParserBlobInsufficientParams);
            return false;
        }
        for (int i = 0; i < parameters.Count - 2; i++)
            if (!parameters[i].Verify(typeof(IBlobItem), errors))
                return false;
        if (!parameters[^2].Verify(typeof(double), errors))
        {
            errors.Add(Position, Rsc.ParserInvalidBlobThreshold);
            return false;
        }
        if (!parameters[^1].Verify(typeof(IMaterial), errors))
        {
            errors.Add(Position, Rsc.ParserInvalidBlobMaterial);
            return false;
        }
        verifiedAs = type;
        return true;
    }

    public override IAstValue ExpandMacro(Dictionary<string, IAstValue> bindings) =>
        ExpandParameters(bindings);

    #endregion
}

internal abstract class AstCsg(Errors errors, SourceRange position,
    Type classtype, object parameters) : AstObject(errors, position, classtype, parameters)
{
    protected bool CheckParameters(Errors errors)
    {
        int start, end;
        IAstValue material;
        if (parameters[0].IsA(typeof(IMaterial)))
        {
            start = 1; end = parameters.Count;
            material = parameters[0];
        }
        else if (parameters[^1].IsA(typeof(IMaterial)))
        {
            start = 0; end = parameters.Count - 1;
            material = parameters[end];
        }
        else
        {
            start = 0; end = parameters.Count;
            material = null;
        }
        for (int i = start; i < end; i++)
            if (!parameters[i].Verify(typeof(IShape), errors))
                return false;
        if (material == null)
        {
            if (!parameters[0].IsA(typeof(MaterialShape)))
            {
                errors.Add(Position, Rsc.ParserMissingMaterial);
                return false;
            }
        }
        else if (!material.Verify(typeof(IMaterial), errors))
            return false;
        return true;
    }

    public override IAstValue ExpandMacro(Dictionary<string, IAstValue> bindings) =>
        ExpandParameters(bindings);

    public override object Value
    {
        get
        {
            if (parameters.Count == 1)
                return (IShape)parameters[0].Value;
            int start, end;
            IMaterial material;
            if (parameters[0].IsA(typeof(IMaterial)))
            {
                start = 1; end = parameters.Count;
                material = (IMaterial)parameters[0].Value;
            }
            else if (parameters[^1].IsA(typeof(IMaterial)))
            {
                start = 0; end = parameters.Count - 1;
                material = (IMaterial)parameters[end].Value;
            }
            else
            {
                start = 0; end = parameters.Count;
                material = null;
            }
            IShape[] arguments = new IShape[end - start];
            for (int i = 0; i < arguments.Length; i++)
                arguments[i] = (IShape)parameters[i + start].Value;
            material ??= ((MaterialShape)arguments[0]).Material;
            return classtype
                .GetConstructor([typeof(IShape[]), typeof(IMaterial)])
                .Invoke([arguments, material]);
        }
    }
}

internal sealed class AstIntersection(Errors errors, SourceRange position, object parameters)
    : AstCsg(errors, position, typeof(Intersection), parameters), IAstValue
{
    #region IAstValue members

    public override bool Verify(Type type, Errors errors)
    {
        if (!type.IsAssignableFrom(classtype))
        {
            errors.Add(Position, Rsc.ParserIncompatibleIntersection, type.Name);
            return false;
        }
        return CheckParameters(errors);
    }

    #endregion
}

internal sealed class AstDifference(Errors errors, SourceRange position, object parameters)
    : AstCsg(errors, position, typeof(Difference), parameters), IAstValue
{
    #region IAstValue members

    public override bool Verify(Type type, Errors errors)
    {
        if (!type.IsAssignableFrom(classtype))
        {
            errors.Add(Position, Rsc.ParserIncompatibleDifference, type.Name);
            return false;
        }
        return CheckParameters(errors);
    }

    #endregion
}
