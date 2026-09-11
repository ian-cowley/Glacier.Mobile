namespace Glacier.Mobile.Core;

using System;
using System.Diagnostics;
using Glacier.Mobile.Input;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;

/// <summary>
/// Root mobile application orchestrator managing lifecycle, 120Hz frame pump, and touch dispatch.
/// </summary>
public abstract class MobileApplication : IDisposable
{
    private bool _disposed;
    private bool _isRunning;

    public DisplayMetrics Metrics { get; }
    public ICompositor Compositor { get; private set; }
    public TouchDispatcher TouchDispatcher { get; } = new();
    public MobileView? RootView { get; protected set; }
    public bool IsRunning => _isRunning;
    public long FrameCount { get; private set; }

    public event Action? Started;
    public event Action? Paused;
    public event Action? Resumed;
    public event Action? Stopped;

    protected MobileApplication(DisplayMetrics? metrics = null, ICompositor? compositor = null)
    {
        Metrics = metrics ?? DisplayMetrics.DefaultPhone;
        Compositor = compositor ?? new HeadlessCompositor((int)Metrics.Width, (int)Metrics.Height);
    }

    /// <summary>
    /// Declarative factory method constructing the mobile visual tree hierarchy.
    /// </summary>
    protected abstract MobileView Build();

    /// <summary>
    /// Initializes and starts the application runtime.
    /// </summary>
    public virtual void Start()
    {
        RootView = Build();
        _isRunning = true;
        Started?.Invoke();
    }

    /// <summary>
    /// Dispatches a high-frequency native touch event to the visual tree and gesture recognizers.
    /// </summary>
    public void DispatchTouch(in TouchEvent touch)
    {
        TouchDispatcher.Dispatch(in touch);

        if (RootView != null)
        {
            var hit = RootView.HitTest(touch.X, touch.Y);
            hit?.ProcessTouch(in touch);
        }
    }

    /// <summary>
    /// Steps the application animation, kinetic physics, and compositing for a discrete frame.
    /// </summary>
    public void Step(float dt)
    {
        FrameCount++;
        RenderFrame();
    }

    /// <summary>
    /// Renders the current visual tree into the compositor.
    /// </summary>
    public void RenderFrame()
    {
        if (RootView == null) return;

        Compositor.BeginFrame();
        Compositor.RenderTree(RootView, Metrics.Width, Metrics.Height);
        Compositor.EndFrame();
    }

    public virtual void Pause()
    {
        _isRunning = false;
        Paused?.Invoke();
    }

    public virtual void Resume()
    {
        _isRunning = true;
        Resumed?.Invoke();
    }

    public virtual void Stop()
    {
        _isRunning = false;
        Stopped?.Invoke();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            Compositor.Dispose();
            _disposed = true;
        }
    }
}
