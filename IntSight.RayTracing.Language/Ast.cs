using IntSight.Parser;
using IntSight.RayTracing.Engine;
using System.Globalization;
using Rsc = IntSight.RayTracing.Language.Properties.Resources;

namespace IntSight.RayTracing.Language;

#region Interface types for the Abstract Syntax Tree

public interface IAstValue : IAstNode
{
    IAstValue Clone();
    bool IsA(Type type);
    string Name { get; set; }
    object Value { get; }
    bool Verify(Type type, Errors errors);
    IAstValue ExpandMacro(Dictionary<string, IAstValue> bindings);
}

public interface ITimeProvider
{
    double Time { get; }
}

#endregion

public abstract class AstValue : IAstValue
{
    public static double Parse(object s) => double.Parse((string)s, CultureInfo.InvariantCulture);

    public static IAstValue SetName(object astValue, object name)
    {
        IAstValue result = (IAstValue)astValue;
        result.Name = (string)name;
        return result;
    }

    #region IAstNode Members

    public SourceRange Position { get; set; }

    #endregion

    #region IAstValue Members

    IAstValue IAstValue.Clone() => (IAstValue)MemberwiseClone();

    bool IAstValue.IsA(Type type) => throw new NotImplementedException();

    string IAstValue.Name { get; set; } = string.Empty;

    object IAstValue.Value => throw new NotImplementedException();

    bool IAstValue.Verify(Type type, Errors errors) => true;

    IAstValue IAstValue.ExpandMacro(Dictionary<string, IAstValue> bindings) => this;

    #endregion
}

internal sealed class AstNumber : AstValue, IAstValue
{
    private readonly double value;
    private bool isInteger;

    public AstNumber(object value) => this.value = Parse(value);

    public AstNumber(double value) => this.value = value;

    #region IAstValue Members

    bool IAstValue.IsA(Type type)
    {
        if (type == typeof(double))
            return true;
        else if (type == typeof(int) && value == Math.Round(value))
        {
            isInteger = true;
            return true;
        }
        else
            return false;
    }

    object IAstValue.Value => isInteger ? (int)value : (object)value;

    public override string ToString() => value.ToString();

    #endregion
}

internal sealed class AstString : AstValue, IAstValue
{
    private readonly string value;

    public AstString(object value) => this.value = StripString(value.ToString());

    public static string StripString(string s)
    {
        StringBuilder sb = new(s.Length - 2);
        s += "\u0000";
        int idx = 0;
        char ch;
    START:
        switch (s[idx++])
        {
            case '\'':
                ch = s[idx++];
                if (ch == '\'')
                {
                    ch = s[idx];
                    if (ch != '\'')
                        goto START;
                    idx++;
                }
                sb.Append(ch);
                goto case '\'';
        }
        return sb.ToString();
    }

    #region IAstValue Members

    bool IAstValue.IsA(Type type) => type == typeof(string);

    object IAstValue.Value => value;

    #endregion
}

[DebuggerDisplay("VECT({x}, {y}, {z})")]
internal sealed class AstVector : AstValue, IAstValue
{
    private IAstValue x, y, z;

    public AstVector(IAstValue x, IAstValue y, IAstValue z) =>
        (this.x, this.y, this.z) = (x, y, z);

    #region IAstValue Members

    bool IAstValue.IsA(Type type) => type == typeof(Vector);

    bool IAstValue.Verify(Type type, Errors errors) =>
        x.Verify(typeof(double), errors) &&
        y.Verify(typeof(double), errors) &&
        z.Verify(typeof(double), errors);

    object IAstValue.Value =>
        new Vector(
            Convert.ToDouble(x.Value),
            Convert.ToDouble(y.Value),
            Convert.ToDouble(z.Value));

    IAstValue IAstValue.ExpandMacro(Dictionary<string, IAstValue> bindings)
    {
        x = x.ExpandMacro(bindings);
        y = y.ExpandMacro(bindings);
        z = z.ExpandMacro(bindings);
        return this;
    }

    #endregion
}

internal sealed class AstVectorConstant : AstValue, IAstValue
{
    private Vector value;

    public AstVectorConstant(double x, double y, double z) => value = new(x, y, z);

    public AstVectorConstant Multiply(double factor)
    {
        value *= factor;
        return this;
    }

    #region IAstValue Members

    bool IAstValue.IsA(Type type) => type == typeof(Vector);

    object IAstValue.Value => value;

    #endregion
}

internal sealed class AstBinary : AstValue, IAstValue
{
    private IAstValue v1, v2;
    private readonly char op;

    public AstBinary(IAstValue v1, IAstValue v2, char op) =>
        (this.v1, this.v2, this.op) = (v1, v2, op);

    #region IAstValue Members

    IAstValue IAstValue.Clone() => new AstBinary(v1.Clone(), v2.Clone(), op);

    bool IAstValue.IsA(Type type)
    {
        switch (op)
        {
            case '+':
                if (v1.IsA(typeof(Vector)))
                    return type == typeof(Vector);
                else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                    return type == typeof(int) || type == typeof(double);
                else if (v1.IsA(typeof(double)))
                    return type == typeof(double);
                else if (v1.IsA(typeof(IShape)))
                    return type == typeof(Union) || type == typeof(IShape);
                else
                    return false;
            case '-':
                if (v1.IsA(typeof(Vector)))
                    return type == typeof(Vector);
                else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                    return type == typeof(int) || type == typeof(double);
                else if (v1.IsA(typeof(double)))
                    return type == typeof(double);
                else if (v1.IsA(typeof(IShape)))
                    return type == typeof(Difference) || type == typeof(IShape);
                else
                    return false;
            case '*':
                if (v1.IsA(typeof(Vector)))
                    if (v2.IsA(typeof(Vector)))
                        return type == typeof(double);
                    else
                        return type == typeof(Vector);
                else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                    return type == typeof(int) || type == typeof(double);
                else if (v1.IsA(typeof(double)))
                    if (v2.IsA(typeof(Vector)))
                        return type == typeof(Vector);
                    else
                        return type == typeof(double);
                else if (v1.IsA(typeof(IShape)))
                    return type == typeof(Intersection) || type == typeof(IShape);
                else
                    return false;
            case '/':
                if (v1.IsA(typeof(Vector)))
                    return type == typeof(Vector);
                else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                    return type == typeof(int) || type == typeof(double);
                else if (v1.IsA(typeof(double)))
                    return type == typeof(double);
                else
                    return false;
            default:
                return false;
        }
    }

    object IAstValue.Value
    {
        get
        {
            object obj1 = v1.Value, obj2 = v2.Value;
            switch (op)
            {
                case '+':
                    if (v1.IsA(typeof(Vector)))
                        return (Vector)obj1 + (Vector)obj2;
                    else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                        return Convert.ToInt32(obj1) + Convert.ToInt32(obj2);
                    else if (v1.IsA(typeof(double)))
                        return Convert.ToDouble(obj1) + Convert.ToDouble(obj2);
                    else
                        return new Union((IShape)obj1, (IShape)obj2);
                case '-':
                    if (v1.IsA(typeof(Vector)))
                        return (Vector)obj1 - (Vector)obj2;
                    else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                        return Convert.ToInt32(obj1) - Convert.ToInt32(obj2);
                    else if (v1.IsA(typeof(double)))
                        return Convert.ToDouble(obj1) - Convert.ToDouble(obj2);
                    else
                        return new Difference((IShape)obj1, (IShape)obj2);
                case '*':
                    if (v1.IsA(typeof(Vector)))
                        if (v2.IsA(typeof(Vector)))
                            return (Vector)obj1 * (Vector)obj2;
                        else
                            return (Vector)obj1 * Convert.ToDouble(obj2);
                    else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                        return Convert.ToInt32(obj1) * Convert.ToInt32(obj2);
                    else if (v1.IsA(typeof(double)))
                        if (v2.IsA(typeof(Vector)))
                            return Convert.ToDouble(obj1) * (Vector)obj2;
                        else
                            return Convert.ToDouble(obj1) * Convert.ToDouble(obj2);
                    else
                        return new Intersection((IShape)obj1, (IShape)obj2);
                case '/':
                    if (v1.IsA(typeof(Vector)))
                        return (Vector)obj1 / Convert.ToDouble(obj2);
                    else if (v1.IsA(typeof(int)) && v2.IsA(typeof(int)))
                        return Convert.ToInt32(obj1) / Convert.ToInt32(obj2);
                    else
                        return Convert.ToDouble(obj1) / Convert.ToDouble(obj2);
            }
            throw new NotImplementedException();
        }
    }

    bool IAstValue.Verify(Type type, Errors errors)
    {
        switch (op)
        {
            case '+':
            case '-':
                if (v1.IsA(typeof(Vector)))
                    return type == typeof(Vector) &&
                        v1.Verify(typeof(Vector), errors) &&
                        v2.Verify(typeof(Vector), errors);
                if (v1.IsA(typeof(int)))
                    if (v2.IsA(typeof(int)))
                        return (type == typeof(int) || type == typeof(double)) &&
                            v1.Verify(typeof(int), errors) &&
                            v2.Verify(typeof(int), errors);
                    else if (v2.IsA(typeof(double)))
                        return type == typeof(double) &&
                            v1.Verify(typeof(double), errors) &&
                            v2.Verify(typeof(double), errors);
                if (v1.IsA(typeof(double)))
                    return type == typeof(double) &&
                        v1.Verify(typeof(double), errors) &&
                        v2.Verify(typeof(double), errors);
                if (v1.IsA(typeof(IShape)))
                    return type == typeof(IShape) &&
                        v1.Verify(typeof(IShape), errors) &&
                        v2.Verify(typeof(IShape), errors);
                if (op == '+')
                    errors.Add(Position, Rsc.ParserInvalidAdd);
                else
                    errors.Add(Position, Rsc.ParserInvalidSub);
                return false;
            case '*':
                if (v1.IsA(typeof(Vector)))
                {
                    if (v2.IsA(typeof(Vector)))
                        return type == typeof(double) &&
                            v1.Verify(typeof(Vector), errors) &&
                            v2.Verify(typeof(Vector), errors);
                    else if (v2.IsA(typeof(double)))
                        return type == typeof(Vector) &&
                            v1.Verify(typeof(Vector), errors) &&
                            v2.Verify(typeof(double), errors);
                }
                else if (v1.IsA(typeof(int)))
                {
                    if (v2.IsA(typeof(int)))
                        return (type == typeof(double) || type == typeof(int)) &&
                            v1.Verify(typeof(int), errors) &&
                            v2.Verify(typeof(int), errors);
                    else if (v2.IsA(typeof(double)))
                        return (type == typeof(double)) &&
                            v1.Verify(typeof(double), errors) &&
                            v2.Verify(typeof(double), errors);
                    else if (v2.IsA(typeof(Vector)))
                        return type == typeof(Vector) &&
                            v1.Verify(typeof(double), errors) &&
                            v2.Verify(typeof(Vector), errors);
                }
                else if (v1.IsA(typeof(double)))
                {
                    if (v2.IsA(typeof(double)))
                        return type == typeof(double) &&
                            v1.Verify(typeof(double), errors) &&
                            v2.Verify(typeof(double), errors);
                    else if (v2.IsA(typeof(Vector)))
                        return type == typeof(Vector) &&
                            v1.Verify(typeof(double), errors) &&
                            v2.Verify(typeof(Vector), errors);
                }
                else if (v1.IsA(typeof(IShape)))
                    return type == typeof(IShape) &&
                        v1.Verify(typeof(IShape), errors) &&
                        v2.Verify(typeof(IShape), errors);
                errors.Add(Position, Rsc.ParserInvalidMult);
                return false;
            case '/':
                if (v1.IsA(typeof(Vector)))
                    return type == typeof(Vector) &&
                        v1.Verify(typeof(Vector), errors) &&
                        v2.Verify(typeof(double), errors);
                if (v1.IsA(typeof(int)))
                    if (v2.IsA(typeof(int)))
                        return (type == typeof(int) || type == typeof(double)) &&
                            v1.Verify(typeof(int), errors) &&
                            v2.Verify(typeof(int), errors);
                    else if (v2.IsA(typeof(double)))
                        return type == typeof(double) &&
                        v1.Verify(typeof(double), errors) &&
                        v2.Verify(typeof(double), errors);
                if (v1.IsA(typeof(double)))
                    return type == typeof(double) &&
                        v1.Verify(typeof(double), errors) &&
                        v2.Verify(typeof(double), errors);
                errors.Add(Position, Rsc.ParserInvalidDiv);
                return false;
        }
        return true;
    }

    IAstValue IAstValue.ExpandMacro(Dictionary<string, IAstValue> bindings)
    {
        v1 = v1.ExpandMacro(bindings);
        v2 = v2.ExpandMacro(bindings);
        return this;
    }

    public override string ToString() => $"({v1} {op} {v2})";

    #endregion
}

internal sealed class AstUnary : AstValue, IAstValue
{
    public enum Operation
    {
        Plus, Minus,
        Sqrt, Sin, Cos, Tan, Abs, Asin, Acos, Atan, Exp, Log
    };

    private IAstValue v;
    private readonly Operation op;

    public AstUnary(IAstValue v, Operation op) =>
        (this.v, this.op) = (v, op);

    public AstUnary(Operation op, object parameters)
    {
        List<IAstValue> p = (List<IAstValue>)parameters;
        if (p.Count != 1)
            throw new Exception(Rsc.ParserInvalidFunctParamCount);
        this.op = op;
        v = p[0];
    }

    #region IAstValue Members

    IAstValue IAstValue.Clone() => new AstUnary(v.Clone(), op);

    bool IAstValue.IsA(Type type) =>
        v.IsA(typeof(double))
        ? type == typeof(double)
        : v.IsA(typeof(Vector)) && type == typeof(Vector);

    private double ArgValue => Convert.ToDouble(v.Value);

    object IAstValue.Value => op switch
    {
        Operation.Minus => v.IsA(typeof(Vector)) ? -(Vector)v.Value : (object)-ArgValue,
        Operation.Sin => Math.Sin(ArgValue),
        Operation.Cos => Math.Cos(ArgValue),
        Operation.Tan => Math.Tan(ArgValue),
        Operation.Abs => Math.Abs(ArgValue),
        Operation.Exp => Math.Exp(ArgValue),
        Operation.Log => Math.Log(ArgValue),
        Operation.Asin => Math.Asin(ArgValue),
        Operation.Acos => Math.Acos(ArgValue),
        Operation.Atan => Math.Atan(ArgValue),
        Operation.Sqrt => Math.Sqrt(ArgValue),
        _ => v.Value,
    };

    bool IAstValue.Verify(Type type, Errors errors)
    {
        if (v.IsA(typeof(double)))
            return type == typeof(double) && v.Verify(type, errors);
        else if (v.IsA(typeof(Vector)))
        {
            if (op == Operation.Plus || op == Operation.Minus)
                return type == typeof(Vector) && v.Verify(type, errors);
            errors.Add(Position, Rsc.ParserInvalidFunctVectorParam);
            return false;
        }
        errors.Add(Position, Rsc.ParserInvalidUnary);
        return false;
    }

    IAstValue IAstValue.ExpandMacro(Dictionary<string, IAstValue> bindings)
    {
        v = v.ExpandMacro(bindings);
        return this;
    }

    public override string ToString() => $"({op} {v})";

    #endregion
}

internal sealed class AstColor : AstValue, IAstValue
{
    private readonly Pixel value;

    public AstColor() { }

    public AstColor(IAstValue r, IAstValue g, IAstValue b) =>
        value = !r.IsA(typeof(double)) || !g.IsA(typeof(double)) || !b.IsA(typeof(double))
            ? throw new ParsingException(Position, Rsc.ParserInvalidColorComp)
            : new(
                Convert.ToDouble(r.Value),
                Convert.ToDouble(g.Value),
                Convert.ToDouble(b.Value));

    public AstColor(IAstValue brightness) =>
        value = !brightness.IsA(typeof(double))
            ? throw new ParsingException(Position, Rsc.ParserInvalidColorBright)
            : new(Convert.ToDouble(brightness.Value));

    public AstColor(string colorName)
    {
        if (XrtRegistry.IsColorName(colorName, out Color clr))
            value = new(clr.R * (1F / 255F), clr.G * (1F / 255F), clr.B * (1F / 255F));
    }

    #region IAstValue Members

    object IAstValue.Value => value;

    bool IAstValue.IsA(Type type) => type == typeof(Pixel);

    #endregion
}

internal sealed class AstSplinePoint : IAstNode
{
    public readonly IAstValue Time;
    public IAstValue Result;

    public AstSplinePoint(object result) => Result = (IAstValue)result;

    public AstSplinePoint(object time, object result) =>
        (Time, Result) = ((IAstValue)time, (IAstValue)result);

    public AstSplinePoint Clone() =>
        new(Time?.Clone(), Result.Clone()) { Position = Position };

    #region Miembros de IAstNode

    public SourceRange Position { get; set; }

    #endregion
}

internal sealed class AstSpline : AstValue, IAstValue
{
    private List<AstSplinePoint> parameters;
    private Type baseType;
    private readonly ITimeProvider timeProvider;
    private bool hasTime;

    public AstSpline(object parameters, ITimeProvider timeProvider)
    {
        this.parameters = (List<AstSplinePoint>)parameters;
        this.timeProvider = timeProvider;
    }

    #region IAstValue members

    IAstValue IAstValue.Clone()
    {
        AstSpline other = (AstSpline)MemberwiseClone();
        other.parameters = new List<AstSplinePoint>();
        foreach (AstSplinePoint p in parameters)
            other.parameters.Add(p.Clone());
        return other;
    }

    bool IAstValue.IsA(Type type) => parameters.Count > 0 && parameters[0].Result.IsA(type);

    bool IAstValue.Verify(Type type, Errors errors)
    {
        if (parameters.Count < 2)
        {
            errors.Add(Position, Rsc.ParserRangeInsufficientParams);
            return false;
        }
        AstSplinePoint point0 = parameters[0];
        if (point0.Result.IsA(typeof(double)))
            baseType = typeof(double);
        else if (point0.Result.IsA(typeof(Vector)))
            baseType = typeof(Vector);
        else if (point0.Result.IsA(typeof(Pixel)))
            baseType = typeof(Pixel);
        else
        {
            errors.Add(Position, Rsc.ParserRangeInvalidParamType);
            return false;
        }
        if (!point0.Result.Verify(baseType, errors))
            return false;
        for (int i = 1; i < parameters.Count; i++)
        {
            IAstValue v = parameters[i].Result;
            if (!v.IsA(baseType))
            {
                errors.Add(v.Position, Rsc.ParserRangeInvalidParamType);
                return false;
            }
            else if (!v.Verify(baseType, errors))
                return false;
            if (parameters[i].Time != null)
                hasTime = true;
        }
        if (hasTime)
            for (int i = 0; i < parameters.Count; i++)
            {
                AstSplinePoint p = parameters[i];
                if (p.Time == null && i != 0 && i != parameters.Count - 1)
                {
                    errors.Add(Position, Rsc.ParserIncompleteInterpolationHints);
                    return false;
                }
                if (p.Time != null)
                    if (!p.Time.IsA(typeof(double)))
                    {
                        errors.Add(Position, Rsc.ParserRangeTimeNotReal);
                        return false;
                    }
            }
        return true;
    }

    object IAstValue.Value
    {
        get
        {
            double[] timeHints = new double[parameters.Count];
            for (int i = 0; i < timeHints.Length; i++)
                timeHints[i] = (double)i / (timeHints.Length - 1);
            if (hasTime)
                for (int i = 0; i < timeHints.Length; i++)
                {
                    IAstValue hint = parameters[i].Time;
                    if (hint != null)
                        timeHints[i] = Convert.ToDouble(hint.Value);
                }
            if (timeHints[0] != 0.0)
                throw new Exception(Rsc.ParserInvalidInterpolationFirst);
            if (timeHints[^1] != 1.0)
                throw new Exception(Rsc.ParserInvalidInterpolationLast);
            for (int i = 1; i < timeHints.Length; i++)
                if (timeHints[i] <= timeHints[i - 1])
                    throw new Exception(Rsc.ParserInterpolationNotAscending);
            double time = timeProvider == null ? 0.0 : timeProvider.Time;
            if (time <= 0.0)
                return parameters[0].Result.Value;
            if (time >= 1.0)
                return parameters[^1].Result.Value;
            int segment = 0;
            while (timeHints[segment + 1] < time)
                segment++;
            double lo = timeHints[segment];
            double hi = timeHints[segment + 1];
            time = (time - lo) / (hi - lo);
            if (time > 1.0)
                time = 1.0;
            else if (time < 0.0)
                time = 0.0;
            if (baseType == typeof(double))
            {
                double v0 = Convert.ToDouble(parameters[segment].Result.Value);
                double v1 = Convert.ToDouble(parameters[segment + 1].Result.Value);
                return v0 + (v1 - v0) * time;
            }
            else if (baseType == typeof(Vector))
            {
                Vector v0 = (Vector)parameters[segment].Result.Value;
                Vector v1 = (Vector)parameters[segment + 1].Result.Value;
                return new Vector(
                    v0.X + (v1.X - v0.X) * time,
                    v0.Y + (v1.Y - v0.Y) * time,
                    v0.Z + (v1.Z - v0.Z) * time);
            }
            else if (baseType == typeof(Pixel))
            {
                Pixel v0 = (Pixel)parameters[segment].Result.Value;
                Pixel v1 = (Pixel)parameters[segment + 1].Result.Value;
                return new Pixel(
                    v0.Red + (v1.Red - v0.Red) * time,
                    v0.Green + (v1.Green - v0.Green) * time,
                    v0.Blue + (v1.Blue - v0.Blue) * time);
            }
            throw new Exception(Rsc.ParserRangeUnsupportedType);
        }
    }

    IAstValue IAstValue.ExpandMacro(Dictionary<string, IAstValue> bindings)
    {
        for (int i = 0; i < parameters.Count; i++)
            parameters[i].Result = parameters[i].Result.ExpandMacro(bindings);
        return this;
    }

    #endregion
}