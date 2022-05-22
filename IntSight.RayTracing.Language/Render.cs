using IntSight.Parser;
using IntSight.RayTracing.Engine;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using Rsc = IntSight.RayTracing.Language.Properties.Resources;

namespace IntSight.RayTracing.Language;

public sealed class RenderProgressChangedEventArgs : EventArgs
{
    public RenderProgressInfo ProgressInfo { get; internal set; }
    public int TotalFrames { get; internal set; }
    public int CurrentFrame { get; internal set; }
}

public enum TimeShape { Linear, Parabolic, SquareRoot, Sine, Ess }

public sealed class RenderCompletedEventArgs : AsyncCompletedEventArgs
{
    public RenderCompletedEventArgs(
        Exception error, bool cancelled, Errors errors, PixelMap pixelMap)
        : base(error, cancelled, null) =>
        (Errors, PixelMap) = (errors, pixelMap);

    /// <summary>Gets the list of parsing errors.</summary>
    public Errors Errors { get; }

    /// <summary>Gets the rendered pixel map.</summary>
    public PixelMap PixelMap { get; }
}

public sealed class RenderPreviewEventArgs : EventArgs
{
    public RenderPreviewEventArgs(PixelMap map, int from, int to) =>
        (Map, FromRow, ToRow) = (map, from, to);

    public PixelMap Map { get; }
    public int FromRow { get; }
    public int ToRow { get; }
}

/// <summary>Controls single scene, motion blurred and animated rendering.</summary>
public sealed class RenderProcessor : IRenderListener
{
    private readonly SendOrPostCallback reportProgress;
    private readonly SendOrPostCallback renderPreview;
    private readonly RenderProgressChangedEventArgs progressArgs = new();
    private AsyncOperation asyncOperation;
    private bool cancelled;

    public RenderProcessor()
    {
        // Hook the report progress event.
        reportProgress = state => RenderProgressChanged?.Invoke(this, progressArgs);
        // Hook the render preview event.
        renderPreview = state => RenderPreview?.Invoke(this, (RenderPreviewEventArgs)state);
    }

    /// <summary>Compiles and renders a scene from a SILLY script.</summary>
    /// <param name="document">Document containing the scene description.</param>
    /// <param name="clock">Time parameter.</param>
    /// <param name="mode">Render quality mode.</param>
    /// <param name="multithreading">True for parallel rendering.</param>
    /// <param name="rotateCamera">True for rotating the camera's location.</param>
    public async Task ExecuteAsync(
        IDocument document, double clock, RenderMode mode,
        bool multithreading, bool rotateCamera, bool keepCameraHeight)
    {
        if (IsBusy)
            throw new Exception(Rsc.RenderIsBusy);
        asyncOperation = AsyncOperationManager.CreateOperation(this);
        cancelled = false;
        Exception e = null;
        Errors errors = null;
        PixelMap map = null;
        try
        {
            (e, errors, map) = await RenderAsync(
                document: document, 
                clock: clock, 
                mode: mode, 
                multithreading: multithreading,
                rotateCamera: rotateCamera, 
                keepCameraHeight: keepCameraHeight);
        }
        finally
        {
            asyncOperation = null;
        }
        RenderCompleted?.Invoke(this, new(e, cancelled, errors, map));
    }

    /// <summary>Compiles and renders a scene with motion blur.</summary>
    /// <param name="document">Document containing the scene description.</param>
    /// <param name="clock">Time parameter.</param>
    /// <param name="mode">Render quality mode.</param>
    /// <param name="multithreading">True for parallel rendering.</param>
    /// <param name="rotateCamera">True for rotating the camera's location.</param>
    /// <param name="samples">Number of samples to render.</param>
    /// <param name="width">Time interval between the first and the last frame.</param>
    public async Task ExecuteAsync(
        IDocument document, double clock, RenderMode mode,
        bool multithreading, bool rotateCamera, bool keepCameraHeight,
        int samples, double width)
    {
        if (IsBusy)
            throw new Exception(Rsc.RenderIsBusy);
        asyncOperation = AsyncOperationManager.CreateOperation(this);
        cancelled = false;
        Exception e = null;
        Errors errors = null;
        PixelMap map = null;
        try
        {
            (e, errors, map) = await MotionBlurAsync(
                document, clock, mode, multithreading, rotateCamera, keepCameraHeight,
                samples, width);
        }
        finally
        {
            asyncOperation = null;
        }
        RenderCompleted?.Invoke(this, new(e, cancelled, errors, map));
    }

    /// <summary>Compiles and animates a scene.</summary>
    /// <param name="document">Document containing the scene description.</param>
    /// <param name="mode">Render quality mode.</param>
    /// <param name="multithreading">True for parallel rendering.</param>
    /// <param name="rotateCamera">True for rotating the camera's location.</param>
    public async Task ExecuteAsync(
        IDocument document, RenderMode mode,
        bool multithreading, bool rotateCamera, bool keepCameraHeight,
        TimeShape timeShape, int firstFrame, int lastFrame, int totalFrames,
        string directory, string fileName)
    {
        if (IsBusy)
            throw new Exception(Rsc.RenderIsBusy);
        asyncOperation = AsyncOperationManager.CreateOperation(this);
        cancelled = false;
        Exception e = null;
        Errors errors = null;
        PixelMap map = null;
        try
        {
            (e, errors, map) = await AnimateAsync(
                document, mode, multithreading, rotateCamera, keepCameraHeight,
                timeShape,
                firstFrame, lastFrame, totalFrames, directory, fileName);
        }
        finally
        {
            asyncOperation = null;
        }
        RenderCompleted?.Invoke(this, new(e, cancelled, errors, map));
    }

    [Browsable(false)]
    public bool IsBusy => asyncOperation != null;

    [Browsable(false)]
    public int CoolingPause { get; set; }

    /// <summary>Cancel the current operation.</summary>
    public void CancelRender() => cancelled = true;

    public event EventHandler<RenderProgressChangedEventArgs> RenderProgressChanged;
    public event EventHandler<RenderCompletedEventArgs> RenderCompleted;
    public event EventHandler<RenderPreviewEventArgs> RenderPreview;

    #region Implementation.

    private Task<(Exception, Errors, PixelMap)> RenderAsync(
        IDocument document, double clock, RenderMode mode,
        bool multithreading, bool rotateCamera, bool keepCameraHeight) =>
        Task.Run(() =>
        {
            Exception e = null;
            Errors errors = null;
            PixelMap map = null;
            try
            {
                progressArgs.TotalFrames = 0;
                progressArgs.CurrentFrame = 0;
                int gcCount = GC.CollectionCount(0);
                int start = Environment.TickCount;
                AstScene scene = AstBuilder.Parse(document.Open(), clock, out errors);
                if (scene != null && errors.Count == 0)
                {
                    int renderTime = Environment.TickCount;
                    int parsingTime = renderTime - start;
                    if (rotateCamera)
                        scene.RotateCamera(clock, keepCameraHeight);
                    map = scene.Evaluate(mode, multithreading, this);
                    if (map != null)
                    {
                        map.ParsingTime = parsingTime;
                        map.RenderTime = Environment.TickCount - renderTime;
                        map.CollectionCount = GC.CollectionCount(0) - gcCount;
                    }
                }
            }
            catch (Exception ex)
            {
                e = ex;
            }
            return (e, errors, map);
        });

    private Task<(Exception, Errors, PixelMap)> MotionBlurAsync(
        IDocument document, double clock, RenderMode mode,
        bool multithreading, bool rotateCamera, bool keepCameraHeight,
        int samples, double width) =>
        Task.Run(() =>
        {
            Exception e = null;
            Errors errors = null;
            PixelMap map = null;
            int gcCount = GC.CollectionCount(0);
            try
            {
                map = RenderWithMotionBlur(document,
                    clock, mode, multithreading, rotateCamera, keepCameraHeight,
                    samples, width, ref errors);
            }
            catch (Exception ex)
            {
                e = ex;
            }
            if (map != null)
                map.CollectionCount = GC.CollectionCount(0) - gcCount;
            return (e, errors, map);
        });

    private Task<(Exception, Errors, PixelMap)> AnimateAsync(
        IDocument document, RenderMode mode,
        bool multithreading, bool rotateCamera, bool keepCameraHeight,
        TimeShape timeShape, int firstFrame, int lastFrame, int totalFrames,
        string directory, string fileName) =>
        Task.Run(() =>
        {
            Exception e = null;
            Errors errors = null;
            PixelMap map = null;
            int gcCount = GC.CollectionCount(0);
            try
            {
                map = RenderAnimation(
                    document, mode, multithreading, rotateCamera, keepCameraHeight, 
                    timeShape,
                    firstFrame, lastFrame, totalFrames,
                    directory, fileName, ref errors);
            }
            catch (Exception ex)
            {
                e = ex;
            }
            if (map != null)
                map.CollectionCount = GC.CollectionCount(0) - gcCount;
            return (e, errors, map);
        });

    private PixelMap RenderWithMotionBlur(
        IDocument document,
        double clock, RenderMode mode, bool dualMode,
        bool rotateCamera, bool keepCameraHeight,
        int samples, double width,
        ref Errors errors)
    {
        int parsingTime = 0, superSamples = 0, renderTime = Environment.TickCount;
        double half = width / 2;
        if (clock < half && 1 - clock < half)
        {
            half = Math.Min(clock, 1 - clock);
            width = half + half;
        }
        if (clock < half)
            clock = half;
        if (1 - clock < half)
            clock = 1 - half;
        var times = new double[samples];
        var weights = new double[samples];
        double sumw = 0.0;
        for (int i = 0; i < samples; i++)
        {
            times[i] = clock - half + i * width / (samples - 1);
            weights[i] = 1.0 - 0.5 * Math.Abs(times[i] - clock) / half;
            sumw += weights[i];
        }
        for (int i = 0; i < samples; i++)
            weights[i] /= sumw;
        Pixel[] result = null;
        progressArgs.TotalFrames = samples;
        progressArgs.CurrentFrame = -1;
        IScene centralScene = null;
        while (++progressArgs.CurrentFrame < samples)
        {
            int t0 = Environment.TickCount;
            AstScene scene = AstBuilder.Parse(
                document.Open(), times[progressArgs.CurrentFrame], out errors);
            parsingTime += Environment.TickCount - t0;
            if (scene == null || errors.Count > 0)
                break;
            if (rotateCamera)
                scene.RotateCamera(times[progressArgs.CurrentFrame], keepCameraHeight);
            PixelMap m = scene.Evaluate(mode, dualMode, this);
            if (cancelled || m == null)
                break;
            if (progressArgs.CurrentFrame == samples / 2)
                centralScene = m.Scene;
            superSamples += m.SuperSamples;
            result = m.Add(result, weights[progressArgs.CurrentFrame]);
        }
        if (!cancelled && errors.Count == 0)
            return new(result, centralScene)
            {
                ParsingTime = parsingTime,
                RenderTime = Environment.TickCount - renderTime - parsingTime,
                SuperSamples = superSamples / samples
            };
        return null;
    }

    private PixelMap RenderAnimation(
        IDocument document,
        RenderMode mode, bool dualMode, bool rotateCamera, bool keepCameraHeight, 
        TimeShape timeShape,
        int firstFrame, int lastFrame, int totalFrames,
        string directory, string fileName,
        ref Errors errors)
    {
        progressArgs.TotalFrames = totalFrames;
        progressArgs.CurrentFrame = firstFrame - 1;
        int superSamples = 0, parsingTime = 0, renderTime = Environment.TickCount;
        PixelMap map = null;
        while (++progressArgs.CurrentFrame <= lastFrame)
        {
            // Get the clock value for this frame.
            double clock = (double)progressArgs.CurrentFrame / (totalFrames - 1);
            clock = timeShape switch
            {
                TimeShape.Parabolic => clock * clock,
                TimeShape.SquareRoot => Math.Sqrt(clock),
                TimeShape.Sine => Math.Sin(clock * Math.PI),
                TimeShape.Ess => 0.5 + 0.5 * Math.Sin(Math.PI * (clock - 0.5)),
                _ => clock
            };
            if (clock < 0.0)
                clock = 0.0;
            else if (clock > 1.0)
                clock = 1.0;
            // Compile with this clock value.
            int t0 = Environment.TickCount;
            AstScene scene = AstBuilder.Parse(document.Open(), clock, out errors);
            parsingTime += Environment.TickCount - t0;
            if (scene == null || errors.Count > 0)
                break;
            if (rotateCamera)
                scene.RotateCamera(clock, keepCameraHeight);
            map = scene.Evaluate(mode, dualMode, this);
            if (cancelled || map == null)
                break;
            superSamples += map.SuperSamples;
            // Save this frame to file.
            map.ToBitmap().Save(
                GetFullPath(directory, fileName,
                    progressArgs.CurrentFrame, totalFrames),
                ImageFormat.Png);
        }
        if (map == null)
            return null;
        map.ParsingTime = parsingTime;
        map.RenderTime = Environment.TickCount - renderTime - parsingTime;
        map.SuperSamples = superSamples / (lastFrame - firstFrame + 1);
        return map;
    }

    private static string GetFullPath(
        string directory, string fileName,
        int currentFrame, int totalFrames)
    {
        int fmtLen = 1;
        while (totalFrames >= 10)
        {
            totalFrames /= 10;
            fmtLen++;
        }
        return System.IO.Path.Combine(directory,
            fileName + currentFrame.ToString(new string('0', fmtLen)) + ".png");
    }

    #endregion

    #region IRenderListener members.

    /// <summary>This method is called by the sampler to report progress.</summary>
    /// <param name="info">Information about the render operation.</param>
    void IRenderListener.Progress(RenderProgressInfo info)
    {
        progressArgs.ProgressInfo = info;
        if (cancelled)
            info.Cancel();
        else
        {
            asyncOperation.Post(reportProgress, null);
            if (CoolingPause > 0)
                Thread.Sleep(CoolingPause);
        }
    }

    void IRenderListener.Preview(PixelMap map, int from, int to) =>
        asyncOperation.Post(renderPreview, new RenderPreviewEventArgs(map, from, to));

    #endregion
}