using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>
/// Provides data for the <see cref="IRenderListener"/> callback interface.
/// </summary>
public sealed class RenderProgressInfo
{
    private readonly int start;

    internal RenderProgressInfo(PixelMap pixels)
    {
        Pixels = pixels;
        Rows = pixels.Height;
        start = Environment.TickCount;
    }

    /// <summary>Gets the total number of rows to render.</summary>
    public int Rows { get; }
    /// <summary>Gets the output pixel map.</summary>
    public PixelMap Pixels { get; }

    /// <summary>Cancel current rendering.</summary>
    public void Cancel() => CancellationPending = true;

    /// <summary>Has the user cancelled the execution?</summary>
    internal bool CancellationPending { get; private set; }

    /// <summary>Gets the total estimated time for rendering.</summary>
    public int Expected
    {
        get
        {
            int completed = Pixels.Completed;
            if (completed == 0)
                return int.MaxValue;
            else
            {
                int elapsed = Environment.TickCount - start;
                return elapsed * Rows / completed - elapsed;
            }
        }
    }

    /// <summary>Gets the render speed, in rows by second.</summary>
    public string Speed
    {
        get
        {
            int elapsed = Environment.TickCount - start;
            return string.Format(Rsc.MsgRowsBySecond,
                elapsed > 0 ? (Pixels.Completed * 1000.0) / elapsed : 0.0);
        }
    }

    /// <summary>Gets the number of already rendered rows.</summary>
    public int Completed => Pixels.Completed;
}

public sealed class RenderException : Exception
{
    internal RenderException(string message)
        : base(message) { }

    internal RenderException(string message, params object[] args)
        : base(string.Format(message, args)) { }

    internal RenderException(string message, Exception innerException)
        : base(message, innerException) { }
}
