using IntSight.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using Rsc = IntSight.RayTracing.Language.Properties.Resources;

namespace IntSight.RayTracing.Language
{
    public sealed class AstBuilder : BaseParser, ITimeProvider
    {
        private List<string> formalParams;
        private double currentTime;

        public static AstScene Parse(ISource source, double currentTime, out Errors errors)
        {
            AstBuilder parser = new(currentTime);
            errors = new();
            return parser.Execute(source, errors, out object result) ? (AstScene)result : null;
        }

        private AstBuilder(double currentTime)
        {
            XrtRegistry.ClearMacros();
            XrtRegistry.AddMacro("clock", new(),
                new AstSpline(new List<AstSplinePoint>
                {
                    new(new AstNumber(0.0)),
                    new(new AstNumber(1.0))
                },
                this));
            XrtRegistry.AddMacro("pi", new(), new AstNumber(Math.PI));
            Time = currentTime;
        }

        #region ITimeProvider Members

        public double Time
        {
            get => currentTime;
            private set => currentTime = value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
        }

        #endregion

        protected sealed override object OnReduce(int rule, object[] e)
        {
            switch ((RNum)rule)
            {
                case RNum.Additive_Additive_minus_Term:
                    return new AstBinary((IAstValue)e[0], (IAstValue)e[2], '-');
                case RNum.Additive_Additive_plus_Term:
                    return new AstBinary((IAstValue)e[0], (IAstValue)e[2], '+');
                case RNum.Additive_minus_Term:
                    return new AstUnary((IAstValue)e[1], AstUnary.Operation.Minus);
                case RNum.Additive_plus_Term:
                    return new AstUnary((IAstValue)e[1], AstUnary.Operation.Plus);
                case RNum.Additive_Term:
                    return e[0];

                case RNum.Ambient_AMBIENT_Color_OptSCol:
                    return CreateList<IAstValue>(e[1]);
                case RNum.Ambient_AMBIENT_number_OptSCol:
                    return CreateList<IAstValue>(new AstNumber(e[1]));
                case RNum.Ambient_AMBIENT_number_atsign_number_OptSCol:
                    return CreateList<IAstValue>(
                        ObjectCall(errors, ResultRange, "AmbientOccluder",
                        new AstNumber(e[1]), new AstNumber(e[3])));
                case RNum.Ambient_AMBIENT_ObjectList:
                    return e[1];

                case RNum.Background_BACKGROUND_Expression_OptSCol:
                    return e[1];

                case RNum.Camera_CAMERA_ObjectCall_OptSCol:
                    return e[1];

                case RNum.Color_RGB_lpar_Expression_cma_Expression_cma_Expression_rpar:
                    return new AstColor((IAstValue)e[2], (IAstValue)e[4], (IAstValue)e[6]);
                case RNum.Color_RGB_number:
                    return new AstColor(new AstNumber(e[1]));

                case RNum.Expression_Additive:
                    return e[0];
                case RNum.Expression_Expression_LOOP_Additive_BY_Additive:
                    return ObjectCall(errors, ResultRange, "Repeater",
                        (IAstValue)e[2], (IAstValue)e[4], (IAstValue)e[0]);
                case RNum.Expression_Expression_LOOP_Additive_cma_Additive_BY_Additive:
                    throw new Exception("Loop");
                case RNum.Expression_Expression_MOVE_Additive:
                    return ObjectCall(errors, ResultRange, "Translate",
                        (IAstValue)e[0], (IAstValue)e[2]);
                case RNum.Expression_Expression_SIZE_Additive:
                    return ObjectCall(errors, ResultRange, "Scale",
                        (IAstValue)e[0], (IAstValue)e[2]);
                case RNum.Expression_Expression_SPIN_Additive:
                    return ObjectCall(errors, ResultRange, "Rotate",
                        (IAstValue)e[0], (IAstValue)e[2]);
                case RNum.Expression_Expression_SPIN_Additive_AROUND_Additive:
                    return
                        ObjectCall(errors, ResultRange, "Translate",
                            ObjectCall(errors, ResultRange, "Rotate",
                                ObjectCall(errors, ResultRange, "Translate",
                                    (IAstValue)e[0],
                                    new AstUnary((IAstValue)e[4], AstUnary.Operation.Minus)),
                                (IAstValue)e[2]),
                            (IAstValue)e[4]);
                case RNum.Expression_Expression_SHEAR_identifier_BY_Additive:
                    {
                        string axes = ((string)e[2]).ToLowerInvariant();
                        switch (axes)
                        {
                            case "xy":
                            case "xz":
                            case "yx":
                            case "yz":
                            case "zx":
                            case "zy":
                                break;
                            default:
                                errors.Add(ranges[2], Rsc.ParserInvalidShear, axes);
                                break;
                        }
                        return e[0];
                    }

                case RNum.ExpressionList_Expression:
                    return CreateList<IAstValue>(e[0]);
                case RNum.ExpressionList_ExpressionList_cma_Expression:
                    return AddToList<IAstValue>(e[0], e[2]);

                case RNum.Factor_Color:
                    return e[0];
                case RNum.Factor_identifier_dot_identifier:
                case RNum.Factor_literal_dot_identifier:
                    throw new Exception("Object properties are not supported in this version.");
                case RNum.Factor_lpar_Expression_rpar:
                    return e[1];
                case RNum.Factor_number:
                    return new AstNumber(e[0]);
                case RNum.Factor_ObjectCall:
                    return e[0];
                case RNum.Factor_string:
                    return new AstString(e[0]);
                case RNum.Factor_VAR_lpar_Points_rpar:
                    return new AstSpline(e[2], this);
                case RNum.Factor_Vector:
                    return e[0];

                case RNum.FormalParameters:
                    return new List<string>();
                case RNum.FormalParameters_lpar_IdentifierList_rpar:
                    formalParams = (List<string>)e[1];
                    return formalParams;

                case RNum.IdentifierList_identifier:
                    return CreateList<string>(e[0]);
                case RNum.IdentifierList_IdentifierList_cma_identifier:
                    return AddToList<string>(e[0], e[2]);

                case RNum.Lights_LIGHTS_ObjectList:
                    return e[1];

                case RNum.Media_MEDIA_ObjectCall_OptSCol:
                    return e[1];

                case RNum.ObjectCall_identifier:
                    {
                        string id = (string)e[0];
                        if (XrtRegistry.FindMacro(id) != null || XrtRegistry.Find(id) != null)
                            return ObjectCall(errors, ResultRange, id);
                        else if (formalParams != null && formalParams.Contains(id))
                            return ObjectCall(errors, ResultRange, id);
                        else if (XrtRegistry.IsColorName(id))
                            return new AstColor(id);
                        else
                        {
                            errors.Add(ranges[0], Rsc.ParserUnknownID, id);
                            return new AstColor();
                        }
                    }
                case RNum.ObjectCall_identifier_lpar_Parameters_rpar:
                    {
                        string s = e[0].ToString();
                        if (string.Compare(s, "Diff", true) == 0 ||
                            string.Compare(s, "Difference", true) == 0)
                            return new AstDifference(errors, ResultRange, e[2]);
                        else if (string.Compare(s, "Union", true) == 0)
                            return new AstUnion(errors, ResultRange, e[2]);
                        else if (string.Compare(s, "Intersection", true) == 0 ||
                            string.Compare(s, "Inter", true) == 0)
                            return new AstIntersection(errors, ResultRange, e[2]);
                        else if (string.Compare(s, "Blob", true) == 0)
                            return new AstBlob(errors, ResultRange, e[2]);
                        else if (XrtRegistry.IsFunctionName(s, out var op))
                            return new AstUnary(op, e[2]);
                        else
                            return ObjectCall(errors, ResultRange, s,
                                ((List<IAstValue>)e[2]).ToArray());
                    }

                case RNum.ObjectList:
                    return new List<IAstValue>();
                case RNum.ObjectList_ObjectList_ObjectCall_OptSCol:
                    return AddToList<IAstValue>(e[0], e[1]);

                case RNum.Objects_Objects_scln_Statement:
                    return AddToList<IAstValue>(e[0], e[2]);
                case RNum.Objects_Statement:
                    return CreateList<IAstValue>(e[0]);

                case RNum.Parameter_Expression:
                    return e[0];
                case RNum.Parameter_identifier_cln_Expression:
                    return AstValue.SetName(e[2], e[0]);

                case RNum.Parameters:
                    return new List<IAstValue>();
                case RNum.Parameters_ParamList:
                    return e[0];

                case RNum.ParamList_Parameter:
                    return CreateList<IAstValue>(e[0]);
                case RNum.ParamList_ParamList_cma_Parameter:
                    return AddToList<IAstValue>(e[0], e[2]);

                case RNum.Point_Expression:
                    return new AstSplinePoint(e[0]);
                case RNum.Point_Expression_cln_Expression:
                    return new AstSplinePoint(e[0], e[2]);
                case RNum.Points_Point:
                    return CreateList<AstSplinePoint>(e[0]);
                case RNum.Points_Points_cma_Point:
                    return AddToList<AstSplinePoint>(e[0], e[2]);

                case RNum.Sampler_SAMPLER_ObjectCall_OptSCol:
                    return e[1];

                case RNum.Scene_SceneSections_END_OptDot:
                    return ((AstScene)e[0]).Verify(errors);

                case RNum.SceneSections:
                    return new AstScene();
                case RNum.SceneSections_SceneSections_Ambient:
                    return AstScene.SetAmbient(e[0], (List<IAstValue>)e[1], errors);
                case RNum.SceneSections_SceneSections_Background:
                    return AstScene.SetBackground(e[0], (IAstValue)e[1], errors);
                case RNum.SceneSections_SceneSections_Camera:
                    return AstScene.SetCamera(e[0], (IAstValue)e[1], errors);
                case RNum.SceneSections_SceneSections_Lights:
                    return AstScene.SetLights(e[0], e[1]);
                case RNum.SceneSections_SceneSections_Media:
                    return AstScene.SetMedia(e[0], (IAstValue)e[1]);
                case RNum.SceneSections_SceneSections_Sampler:
                    return AstScene.SetSampler(e[0], (IAstValue)e[1], errors);
                case RNum.SceneSections_SceneSections_Shapes:
                    return AstScene.SetObjects(e[0], e[1]);
                case RNum.SceneSections_SceneSections_Title:
                    return AstScene.SetTitle(e[0], e[1], ranges[1], errors);

                case RNum.Shapes_OBJECTS_Objects:
                    return e[1];

                case RNum.Statement_ExpressionList:
                    return e[0];
                case RNum.Statement_SET_identifier_FormalParameters_eq_Expression:
                    XrtRegistry.AddMacro((string)e[1], (List<string>)e[2], (IAstValue)e[4]);
                    formalParams = null;
                    return null;

                case RNum.Term_Factor:
                    return e[0];
                case RNum.Term_Term_slash_Factor:
                    return new AstBinary((IAstValue)e[0], (IAstValue)e[2], '/');
                case RNum.Term_Term_times_Factor:
                    return new AstBinary((IAstValue)e[0], (IAstValue)e[2], '*');

                case RNum.Title_SCENE_identifier_OptSCol:
                    return e[1];
                case RNum.Title_SCENE_string_OptSCol:
                    return AstString.StripString(e[1].ToString());

                case RNum.Vector_lbra_Expression_cma_Expression_cma_Expression_rbra:
                    return new AstVector((IAstValue)e[1], (IAstValue)e[3], (IAstValue)e[5]);
                case RNum.Vector_number_VectorConstant:
                    return ((AstVectorConstant)e[1]).Multiply(
                        Convert.ToDouble(e[0].ToString(), CultureInfo.InvariantCulture));
                case RNum.Vector_VectorConstant:
                    return e[0];

                case RNum.VectorConstant_vector0:
                    return new AstVectorConstant(0.0, 0.0, 0.0);
                case RNum.VectorConstant_vector1:
                    return new AstVectorConstant(1.0, 1.0, 1.0);
                case RNum.VectorConstant_vectorX:
                    return new AstVectorConstant(1.0, 0.0, 0.0);
                case RNum.VectorConstant_vectorY:
                    return new AstVectorConstant(0.0, 1.0, 0.0);
                case RNum.VectorConstant_vectorZ:
                    return new AstVectorConstant(0.0, 0.0, 1.0);

                default:
                    return null;
            }
        }

        protected override void SyntaxError(ref bool retry, ref ushort state, int retryTimes) =>
            errors.Add(input.Position, Rsc.ParserSyntaxError);

        private static IAstValue ObjectCall(Errors errors, SourceRange position,
            string identifier, params IAstValue[] parameters)
        {
            XrtRegistry.Macro macro = XrtRegistry.FindMacro(identifier);
            if (macro == null)
                return new AstObject(position, identifier, parameters);
            else
            {
                IAstValue result = macro.Expand(parameters);
                if (result == null)
                    errors.Add(position, Rsc.ParserMacroParamsMismatch, identifier);
                return result;
            }
        }

        private static List<X> CreateList<X>(object item) where X : class
        {
            if (item is not List<X> list)
            {
                list = new();
                if (item is X value)
                    list.Add(value);
            }
            return list;
        }

        private static List<X> AddToList<X>(object collection, object item) where X : class
        {
            List<X> result = (List<X>)collection;
            if (item is X value)
                result.Add(value);
            else if (item is List<X> list)
                result.AddRange(list);
            return result;
        }
    }
}