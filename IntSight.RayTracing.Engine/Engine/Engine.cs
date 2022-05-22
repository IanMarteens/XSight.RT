using System.Xml;

namespace IntSight.RayTracing.Engine;

/// <summary>A very simple container for shapes, lights, sampler and camera.</summary>
/// <remarks>
/// There's only one implementation for <c>IScene</c>: the <c>Scene</c> class.
/// <c>IScene.Render</c> is the root method for rendering.
/// </remarks>
public interface IScene
{
    /// <summary>Gets the scene title.</summary>
    string Title { get; }
    /// <summary>Gets the sampler that will render and collect light rays.</summary>
    ISampler Sampler { get; }
    /// <summary>Gets the camera that maps pixels to light rays.</summary>
    ICamera Camera { get; }
    /// <summary>Gets the array of lights in the scene.</summary>
    ILight[] Lights { get; }
    /// <summary>Gets the omnidirectional ambient light.</summary>
    IAmbient Ambient { get; }
    /// <summary>Gets the scene background.</summary>
    IBackground Background { get; }
    /// <summary>Gets the optional atmospheric media.</summary>
    IMedia Media { get; }
    /// <summary>Gets the optional photon map.</summary>
    PhotonMap Photons { get; }
    /// <summary>Gets the root shape in the scene.</summary>
    IShape Root { get; }

    /// <summary>Renders the scene using the contained components.</summary>
    /// <param name="mode">Render mode: optimal, draft, sonar, etc.</param>
    /// <param name="multithread">If true, strips are rendered in parallel.</param>
    /// <param name="listener">Event listener for progress information.</param>
    /// <returns>The generated image.</returns>
    PixelMap Render(RenderMode mode, bool multithread, IRenderListener listener);

    /// <summary>Gets the time expended in optimization.</summary>
    int OptimizationTime { get; }

    /// <summary>Saves a scene description to an XML writer.</summary>
    /// <param name="writer">XML writer.</param>
    void Write(XmlWriter writer);
    /// <summary>Saves a scene description to an XML file.</summary>
    /// <param name="outputFileName">Output file path.</param>
    void Write(string outputFileName);
}

/// <summary>Special sampling modes.</summary>
public enum RenderMode
{
    /// <summary>A simplified rendering mapping depth to colors.</summary>
    Sonar,
    /// <summary>No reflections, no oversampling, and draft materials are used.</summary>
    Draft,
    /// <summary>No reflections, no oversampling, but materials are honored.</summary>
    TexturedDraft,
    /// <summary>Simple rendering with no oversampling and up to two bounces.</summary>
    Basic,
    /// <summary>Simple antialias rendering with up to three bounces.</summary>
    GoodEnough,
    /// <summary>The sampler specified for the scene is used.</summary>
    Normal
}

/// <summary>A callback interface for reporting progress to the caller.</summary>
public interface IRenderListener
{
    /// <summary>Informs the listener about rendering progress.</summary>
    /// <param name="info">Contains information about rendering.</param>
    void Progress(RenderProgressInfo info);

    /// <summary>Shows a preview of the rendered image for adaptive sampling.</summary>
    /// <param name="map">Partially rendered image.</param>
    /// <param name="from">Initial row.</param>
    /// <param name="to">Last row.</param>
    void Preview(PixelMap map, int from, int to);
}

/// <summary>High level controller for the ray tracing algorithm.</summary>
/// <remarks>
/// A sampler controls how many rays are cast for each pixel in the image,
/// and how they must be combined for the final result.
/// </remarks>
public interface ISampler
{
    /// <summary>Generates a image for the initialized scene.</summary>
    /// <param name="strip">Band of the pixel map to render.</param>
    void Render(PixelStrip strip);

    /// <summary>Initializes the sampler before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void Initialize(IScene scene);

    /// <summary>Gets the estimated number of rays by pixel.</summary>
    /// <remarks>
    /// This number it's only an estimation. Spheric lights use this value
    /// to adjust the number of samples they must cast for shadow tests.
    /// </remarks>
    int Oversampling { get; }

    /// <summary>Get the lens aperture, for focal samplers.</summary>
    double Aperture { get; }

    /// <summary>Create an independent copy of the sampler.</summary>
    /// <returns>The new sampler.</returns>
    ISampler Clone();
}

/// <summary>Maps points in the 2D generated image to rays in the 3D space.</summary>
public interface ICamera
{
    /// <summary>Gets the primary ray used by the camera.</summary>
    Ray PrimaryRay { get; }

    /// <summary>Initializes the camera ray for a given row and column.</summary>
    /// <param name="row">Row for the camera ray.</param>
    /// <param name="column">Column for the camera ray.</param>
    void Focus(int row, int column);
    /// <summary>Sets the row for the camera ray.</summary>
    /// <param name="row">Zero based row number.</param>
    void FocusRow(int row);
    /// <summary>Sets the column for the camera ray.</summary>
    /// <param name="column">Zero based column number.</param>
    void FocusColumn(int column);
    /// <summary>Initializes the camera ray given a small target deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    void GetRay(double dY, double dX);
    /// <summary>Initializes the camera ray given a target and an origin deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    /// <param name="odY">Vertical origin deviation.</param>
    /// <param name="odX">Horizontal origin deviation.</param>
    void GetRay(double dY, double dX, double odY, double odX);

    /// <summary>Scales jitter according to the pixel size in each dimension.</summary>
    /// <param name="jitter">Array with jitter data.</param>
    /// <returns>Echoes the jitter array.</returns>
    double[] InitJitter(double[] jitter);
    /// <summary>Given a ray, find the pixel it intersects.</summary>
    /// <param name="ray">A camera ray.</param>
    /// <param name="row">Row where the pixel belongs.</param>
    /// <param name="column">Column where the pixel belongs.</param>
    void GetRayCoordinates(Ray ray, out int row, out int column);

    /// <summary>Selects the best camera for a given scene.</summary>
    /// <param name="sampler">Sampler to be used.</param>
    /// <returns>The selected camera.</returns>
    ICamera Simplify(ISampler sampler);
    /// <summary>Removes bounds from the root shape, when no needed.</summary>
    /// <param name="root">The root shape in the scene.</param>
    /// <returns>The new root shape.</returns>
    IShape CheckRoot(IShape root);

    /// <summary>Gets the common origin for all camera rays.</summary>
    Vector Location { get; }
    /// <summary>Gets the point the camera looks at.</summary>
    Vector Target { get; }
    /// <summary>Gets the sky reference, for tilting the camera.</summary>
    Vector Up { get; }

    /// <summary>Gets the sampling height in pixels.</summary>
    int Height { get; }
    /// <summary>Gets the sampling width in pixels.</summary>
    int Width { get; }

    /// <summary>Creates an independent copy of the camera.</summary>
    /// <returns>The new camera.</returns>
    ICamera Clone();

    /// <summary>Creates a copy of the camera with a rotated location.</summary>
    /// <param name="rotationAngle">Rotation angle, in degrees.</param>
    /// <returns>The new camera.</returns>
    ICamera Rotate(double rotationAngle, bool keepCameraHeight);
}

/// <summary>Light sources.</summary>
public interface ILight
{
    /// <summary>Initializes a light source before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void Initialize(IScene scene);

    /// <summary>Gets the light color.</summary>
    Pixel Color { get; }

    /// <summary>Gets the light's location.</summary>
    Vector Location { get; }

    /// <summary>Light intensity at a point, with partial and total occlusion.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <param name="isPrimary">Are we sampling an intersection from a primary ray?</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float Intensity(in Vector hitPoint, bool isPrimary);

    /// <summary>Computes the direction the light comes from at a given location.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <returns>Light's direction.</returns>
    Vector GetDirection(in Vector hitPoint);

    /// <summary>Gets a quick estimation of light's intensity.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float DraftIntensity(in Vector hitPoint);

    /// <summary>Creates an independent copy of the light source.</summary>
    /// <returns>The new light object.</returns>
    ILight Clone();

    /// <summary>Optimizes the order of a shape list.</summary>
    /// <param name="shapeList">List of shapes to sort.</param>
    /// <returns>The same list, or a new one with a different order.</returns>
    IShape[] Sort(IShape[] shapeList);
}

/// <summary>Omnidirectional ambient light source.</summary>
public interface IAmbient
{
    /// <summary>Initializes an ambient light before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void Initialize(IScene scene);

    /// <summary>Gets ambient intensity at a given location.</summary>
    /// <param name="location">Location for computing ambient light.</param>
    /// <param name="normal">Normal vector at the hit location.</param>
    /// <returns>Light intensity at the given location.</returns>
    Pixel this[in Vector location, in Vector normal] { get; }

    /// <summary>Creates an independent copy of the ambient light.</summary>
    /// <returns>The new ambient light.</returns>
    IAmbient Clone();
}

/// <summary>Scene background.</summary>
public interface IBackground
{
    /// <summary>Selects the best performing background for a given task.</summary>
    /// <returns>The selected background.</returns>
    IBackground Simplify();

    /// <summary>Initializes the background before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void Initialize(IScene scene);

    /// <summary>Gets the background color in a given direction.</summary>
    /// <param name="ray">The sampling ray.</param>
    /// <returns>The color at the given direction.</returns>
    Pixel this[Ray ray] { get; }

    /// <summary>Gets a quick estimation of the background color in a given direction.</summary>
    /// <param name="ray">The sampling ray.</param>
    /// <returns>The color at the given direction.</returns>
    Pixel DraftColor(Ray ray);

    /// <summary>Creates an independent copy of the background.</summary>
    /// <returns>The new background.</returns>
    IBackground Clone();
}

/// <summary>Defines a solid color mapping, from a 3D location to a color.</summary>
public interface IPigment
{
    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void GetColor(out Pixel color, in Vector location);

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel DraftColor { get; }

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment Clone(bool force);

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    IPigment Translate(in Vector translation);

    /// <summary>Rotates the pigment.</summary>
    /// <param name="rotation">The rotation matrix.</param>
    /// <returns>The rotated pigment.</returns>
    IPigment Rotate(in Matrix rotation);

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    IPigment Scale(in Vector factor);
}

/// <summary>Physical surface behavior.</summary>
public interface IMaterial
{
    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    bool GetColor(out Pixel color, in Vector location);

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel DraftColor { get; }

    /// <summary>Gets the index of refraction (IOR) of the material.</summary>
    double IndexOfRefraction { get; }

    /// <summary>Gets the reflection and refraction coefficients at a given angle.</summary>
    /// <param name="cosine">The cosine of the incidence angle.</param>
    /// <param name="filter">Transmition coefficient.</param>
    /// <param name="refraction">Refraction coefficient.</param>
    /// <returns>The reflection coefficient.</returns>
    float Reflection(double cosine, out Pixel filter, out float refraction);

    /// <summary>Computes the amount of diffused light from a source at a point.</summary>
    /// <param name="location">Incidence point.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="reflection">The direction of the reflected ray.</param>
    /// <param name="light">The light source.</param>
    /// <param name="color">The color of the hit point.</param>
    /// <returns>The contributed radiance.</returns>
    Pixel Shade(
        in Vector location, in Vector normal, in Vector reflection,
        ILight light, in Pixel color);

    /// <summary>Does this material have a rough surface?</summary>
    bool HasRoughness { get; }

    /// <summary>Does this material feature exponential light attenuation?</summary>
    bool HasAttenuation { get; }

    /// <summary>The exponential attenuation filter.</summary>
    Pixel AttenuationFactor { get; }

    /// <summary>Perturbates a normal vector given a location.</summary>
    /// <param name="normal">Original normal vector.</param>
    /// <param name="hitPoint">Intersection point.</param>
    void Bump(ref Vector normal, in Vector hitPoint);

    /// <summary>Distorts a reflection vector in a small amount.</summary>
    /// <param name="reflection">Original reflection direction.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="sign">The sign of the cosine factor.</param>
    /// <returns>New reflection vector.</returns>
    Vector Perturbate(in Vector reflection, in Vector normal, bool sign);

    /// <summary>Splits a reflection vector into two distorted directions.</summary>
    /// <param name="reflection">Original reflection vector.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="r1">A ray for the first reflection vector.</param>
    /// <param name="r2">Second reflection vector.</param>
    /// <returns>The relative weight of the first reflection vector.</returns>
    float Perturbate(
        in Vector reflection, in Vector normal,
        Ray r1, out Vector r2);

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    IMaterial Clone(bool force);

    /// <summary>Translates the pigment of the material.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated material.</returns>
    IMaterial Translate(in Vector translation);

    /// <summary>Rotates the pigment of the material.</summary>
    /// <param name="rotation">Rotation matrix.</param>
    /// <returns>The rotated material.</returns>
    IMaterial Rotate(in Matrix rotation);

    /// <summary>Scales the pigment of the material.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled material.</returns>
    IMaterial Scale(in Vector factor);
}

/// <summary>An algorithm to perturbate normals at the hit points.</summary>
public interface IPerturbator
{
    /// <summary>Modifies a normal vector given a location.</summary>
    /// <param name="normal">Normal to perturbate.</param>
    /// <param name="hitPoint">Intersection point.</param>
    void Perturbate(ref Vector normal, in Vector hitPoint);

    /// <summary>Creates an independent copy of this object.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new perturbator.</returns>
    IPerturbator Clone(bool force);
}

/// <summary>Bounded Euclidean shapes.</summary>
public interface ITransformable
{
    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    int MaxHits { get; }

    /// <summary>Estimated complexity.</summary>
    ShapeCost Cost { get; }

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape Clone(bool force);

    /// <summary>Invert normals for the right operand in a difference.</summary>
    void Negate();

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    IShape Simplify();

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    IShape Substitute();

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    void Initialize(IScene scene, bool inCsg, bool inTransform);

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    TransformationCost CanRotate(in Matrix rotation);

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    TransformationCost CanScale(in Vector factor);

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    void ApplyTranslation(in Vector translation);

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    void ApplyRotation(in Matrix rotation);

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    void ApplyScale(in Vector factor);

    /// <summary>Changes the material this shape is made of.</summary>
    /// <param name="newMaterial">The new material definition.</param>
    void ChangeDress(IMaterial newMaterial);

    /// <summary>Notifies the shape that its container is checking spheric bounds.</summary>
    /// <param name="centroid">Centroid of parent's spheric bounds.</param>
    /// <param name="squaredRadius">Square radius of parent's spheric bounds.</param>
    /// <remarks>
    /// If the shape is performing the same bounds check, it may decide to omit its own test.
    /// That's already happening with tori and spheric unions.
    /// </remarks>
    void NotifySphericBounds(in Vector centroid, double squaredRadius);

    /// <summary>Checks whether the entity can be statically rotated or not.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <param name="problemCount">Accumulator for counting conflictive shapes.</param>
    /// <returns>True if rotation is possible.</returns>
    public bool CheckRotation(in Matrix rotation, ref int problemCount)
    {
        switch (CanRotate(rotation))
        {
            case TransformationCost.Depends:
                problemCount++;
                return true;
            case TransformationCost.Nope:
                if (this is Scale)
                {
                    problemCount++;
                    return true;
                }
                return false;
            default:
                return true;
        }
    }

    /// <summary>Checks whether the entity can be statically scaled or not.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <param name="problemCount">Accumulator for counting conflictive shapes.</param>
    /// <returns>True if scaling is possible.</returns>
    public bool CheckScale(in Vector factor, ref int problemCount)
    {
        switch (CanScale(factor))
        {
            case TransformationCost.Depends:
                problemCount++;
                return true;
            case TransformationCost.Nope:
                if (this is Rotate)
                {
                    problemCount++;
                    return true;
                }
                return false;
            default:
                return true;
        }
    }
}

/// <summary>Bounded entities.</summary>
public interface IBounded
{
    /// <summary>A box that encloses this entity.</summary>
    Bounds Bounds { get; }
    /// <summary>The center of the bounding sphere.</summary>
    Vector Centroid { get; }
    /// <summary>The square radius of the bounding sphere.</summary>
    double SquaredRadius { get; }
}

/// <summary>Supported shapes: solids, surfaces, transformations and CSG.</summary>
/// <remarks>
/// There are two complementary methods sets for ray/shape intersection.
/// <c>ShadowTest</c>/<c>HitTest</c> are the most efficient, but don't support CSG.
/// <c>GetHits</c>/<c>GetNormal</c> support CSG, but are slower.
/// </remarks>
public interface IShape : ITransformable, IBounded
{
    #region Intersection tests.

    /// <summary>
    /// Checks whether there's an intersection between the shape and the ray.
    /// </summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool ShadowTest(Ray ray);

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool HitTest(Ray ray, double maxt, ref HitInfo info);

    #endregion

    #region CSG support.

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int GetHits(Ray ray, Hit[] hits);

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at the given point.</returns>
    Vector GetNormal(in Vector location);

    #endregion

    /// <summary>Compares two shapes by their projected solid angle.</summary>
    /// <param name="another">Second shape to compare.</param>
    /// <param name="location">Projection origin.</param>
    /// <returns>The inverted result of the comparison.</returns>
    public int CompareTo(IShape another, in Vector location)
    {
        if (Bounds.IsInfinite)
            return another.Bounds.IsInfinite ? 0 : +1;
        if (another.Bounds.IsInfinite)
            return -1;
        double d1 = Math.Abs(Centroid.Distance(location) -
            Math.Sqrt(SquaredRadius));
        double d2 = Math.Abs(another.Centroid.Distance(location) -
            Math.Sqrt(another.SquaredRadius));
        return (another.SquaredRadius / (d2 * d2)).CompareTo(SquaredRadius / (d1 * d1));
    }
}

/// <summary>A shape with optional bounds checking.</summary>
public interface IBoundsChecker
{
    /// <summary>Is this union checking its bounds?</summary>
    bool IsChecking { get; set; }

    /// <summary>Can we turn off bounds checking for this union?</summary>
    bool IsCheckModifiable { get; }
}

/// <summary>A list of shapes with optional bounds checking.</summary>
public interface IUnion : IBoundsChecker
{
    /// <summary>Gets the list of shapes grouped inside this union.</summary>
    IShape[] Shapes { get; }
}

/// <summary>A shape transformed by any means.</summary>
public interface ITransform
{
    /// <summary>Gets the original shape.</summary>
    IShape Original { get; }
}

/// <summary>The five coefficients of a fourth-degree algebraic equation.</summary>
public struct QuarticCoefficients
{
    public double c0, c1, c2, c3, c4;
}

/// <summary>Field sources inside a blob.</summary>
public interface IBlobItem : IBounded
{
    /// <summary>Computes the intersection interval with the area of influence.</summary>
    /// <param name="ray">Ray to test.</param>
    /// <param name="t1">Time for entering the AOI.</param>
    /// <param name="t2">Time for leaving the AOI.</param>
    /// <returns>True, if the ray ever enters the AOI.</returns>
    bool Hits(Ray ray, ref double t1, ref double t2);
    /// <summary>Gets this item's contribution to the field's gradient.</summary>
    /// <param name="location">Point where the gradient will be evaluated.</param>
    /// <param name="normal">An accumulator for the total gradient.</param>
    void GetNormal(in Vector location, ref Blob.Normal normal);
    /// <summary>Adds this item's contribution to the total potential field.</summary>
    /// <param name="coefficients">Fourth-degree equation's coefficients.</param>
    void AddCoefficients(ref QuarticCoefficients coefficients);
    /// <summary>Removes this item's contribution from the total potential field.</summary>
    /// <param name="coefficients">Fourth-degree equation's coefficients.</param>
    void RemoveCoefficients(ref QuarticCoefficients coefficients);

    /// <summary>Can we rotate this blob item?</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>True when possible.</returns>
    bool CanRotate(in Matrix rotation);
    /// <summary>Can we scale this blob item?</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>True when possible.</returns>
    bool CanScale(in Vector factor);

    /// <summary>Translates this blob item.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated blob item.</returns>
    IBlobItem ApplyTranslation(in Vector translation);
    /// <summary>Rotates this blob item.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The rotated blob item.</returns>
    IBlobItem ApplyRotation(in Matrix rotation);
    /// <summary>Scales this blob item.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled blob item.</returns>
    IBlobItem ApplyScale(in Vector factor);

    /// <summary>Adds this blob item, or an equivalent, to the parent's item list.</summary>
    /// <param name="accumulator">The item list of a blob shape.</param>
    void Simplify(ICollection<IBlobItem> accumulator);
    /// <summary>Substitutes this blob item by a more efficient one.</summary>
    /// <returns>An equivalent blob item.</returns>
    IBlobItem Substitute();
    /// <summary>Creates an independent copy of this blob item.</summary>
    /// <returns>A copy of this blob item.</returns>
    IBlobItem Clone();

    /// <summary>Gets the X coordinate of the center of the bounding sphere.</summary>
    double CentroidX { get; }
    /// <summary>Gets the Y coordinate of the center of the bounding sphere.</summary>
    double CentroidY { get; }
    /// <summary>Gets the Z coordinate of the center of the bounding sphere.</summary>
    double CentroidZ { get; }
}

/// <summary>Algorithms for media interaction.</summary>
public interface IMedia
{
    /// <summary>Initializes the media object before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void Initialize(IScene scene);

    /// <summary>Modifies color for a finite ray.</summary>
    /// <param name="ray">Primary or secondary ray.</param>
    /// <param name="time">Time to intersection.</param>
    /// <param name="color">Color, before and after media interaction.</param>
    void Modify(Ray ray, double time, ref Pixel color);

    /// <summary>Modifies color for an infinite ray.</summary>
    /// <param name="ray">Primary or secondary ray.</param>
    /// <param name="color">Original color, before media interaction.</param>
    /// <returns>The modified color.</returns>
    Pixel Modify(Ray ray, in Pixel color);

    /// <summary>Creates an independent copy of the media.</summary>
    /// <returns>The new media.</returns>
    IMedia Clone();
}

/// <summary>Intersection information for supporting CSG operations.</summary>
public struct Hit
{
    /// <summary>Intersection time, relative to the global frame.</summary>
    public double Time;
    /// <summary>Object that intersects with the ray.</summary>
    public IShape Shape;
}

/// <summary>Intersection information for regular (non-CSG) ray tests.</summary>
public struct HitInfo
{
    /// <summary>Intersection point, in object's local coordinates.</summary>
    public Vector HitPoint;
    /// <summary>Normal vector, in global coordinates.</summary>
    public Vector Normal;
    /// <summary>Hit object's material.</summary>
    public IMaterial Material;
    /// <summary>Intersection time, relative to the global frame.</summary>
    public double Time;
}

/// <summary>Euclidean axes plus an unknown wildcard.</summary>
public enum Axis : byte { Unknown, X, Y, Z }

/// <summary>Estimated cost of rendering a shape.</summary>
public enum ShapeCost
{
    /// <summary>Most simple shapes, such as spheres, boxes and planes.</summary>
    EasyPeasy = 0,
    /// <summary>More complicated shapes, such as tori, cylinders and cones.</summary>
    NoPainNoGain = 1,
    /// <summary>Most complicated shapes.</summary>
    PainInTheNeck = 2
}

/// <summary>Estimated cost of transforming a shape.</summary>
public enum TransformationCost
{
    /// <summary>Statical transformation is possible and desirable.</summary>
    Ok = 0,
    /// <summary>Statical transformation doesn't save too much time.</summary>
    Depends = 1,
    /// <summary>The shape must not be statically transformed.</summary>
    Nope = 2
}
