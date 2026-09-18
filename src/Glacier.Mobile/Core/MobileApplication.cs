namespace Glacier.Mobile.Core;

using System;
using System.Diagnostics;
using Glacier.Mobile.Input;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;

/// <summary>
/// Root mobile application orchestrator managing lifecycle, 120Hz frame pump, and touch dispatch.
/// Integrates a lock-free SPSC touch ring buffer and a pointer capture table for high-frequency input routing.
/// </summary>
public abstract class MobileApplication : IDisposable
{
    private bool _disposed;
    private bool _isRunning;

    public DisplayMetrics Metrics { get; }
    public ICompositor Compositor { get; private set; }
    public TouchDispatcher TouchDispatcher { get; } = new();
    public PointerCaptureTable PointerCaptures { get; } = new();
    public TouchRingBuffer TouchQueue { get; } = new(1024);
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
    /// Asynchronously enqueues a touch snapshot into the lock-free SPSC ring buffer.
    /// </summary>
    public bool EnqueueTouch(in TouchSnapshot snapshot)
    {
        return TouchQueue.TryEnqueue(snapshot);
    }

    /// <summary>
    /// Asynchronously enqueues a touch event into the lock-free SPSC ring buffer.
    /// </summary>
    public bool EnqueueTouch(int pointerId, float x, float y, TouchPhase phase)
    {
        return TouchQueue.TryEnqueue(new TouchSnapshot(Stopwatch.GetTimestamp(), pointerId, x, y, phase));
    }

    /// <summary>
    /// Drains and processes all queued touch events from the lock-free SPSC ring buffer.
    /// </summary>
    public void ProcessQueuedTouches()
    {
        Span<TouchSnapshot> batch = stackalloc TouchSnapshot[64];
        int read;
        while ((read = TouchQueue.Drain(batch)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                ref readonly var s = ref batch[i];
                var evt = new TouchEvent(s.Timestamp, s.PointerId, s.X, s.Y, s.Phase);
                DispatchTouch(in evt);
            }
        }
    }

    /// <summary>
    /// Dispatches a high-frequency native touch event to the visual tree and gesture recognizers,
    /// binding pointers to views via the pointer capture table across gesture lifecycles.
    /// </summary>
    public void DispatchTouch(in TouchEvent touch)
    {
        TouchDispatcher.Dispatch(in touch);

        if (RootView != null)
        {
            MobileView? target = null;
            if (touch.Phase == TouchPhase.Began)
            {
                target = RootView.HitTest(touch.X, touch.Y);
                if (target != null)
                {
                    PointerCaptures.Capture(touch.PointerId, target);
                }
            }
            else
            {
                target = PointerCaptures.GetCaptured(touch.PointerId) ?? RootView.HitTest(touch.X, touch.Y);
            }

            target?.ProcessTouch(in touch);

            if (touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Cancelled)
            {
                PointerCaptures.Release(touch.PointerId);
            }
        }
    }

    /// <summary>
    /// Steps the application animation, kinetic physics, and compositing for a discrete frame.
    /// </summary>
    public void Step(float dt)
    {
        ProcessQueuedTouches();
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
            PointerCaptures.Clear();
            TouchQueue.Clear();
            Compositor.Dispose();
            _disposed = true;
        }
    }
}
