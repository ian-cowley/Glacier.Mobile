namespace Glacier.Mobile.UI.Layouts;

using System;
using System.Runtime.InteropServices;
using Glacier.Mobile.Input;
using SkiaSharp;

/// <summary>
/// Kinetic scrollable viewport delivering 120Hz flick scrolling with inertial physics via GestureKernels.
/// </summary>
public sealed class ScrollView : MobileView
{
    private readonly PanGestureRecognizer _panRecognizer;
    private float _scrollVelocityY;
    private float _scrollOffsetY;

    private MobileView? _content;
    public MobileView? Content
    {
        get => _content;
        set
        {
            if (_content != null) _content.Parent = null;
            _content = value;
            if (_content != null) _content.Parent = this;
        }
    }
    public float ScrollOffsetY => _scrollOffsetY;
    public float MaxScrollY { get; private set; }
    public float Friction { get; set; } = 0.92f; // Damping per 120Hz frame

    public ScrollView()
    {
        _panRecognizer = new PanGestureRecognizer();
        _panRecognizer.StateChanged += OnPanStateChanged;
        GestureRecognizers.Add(_panRecognizer);
    }

    private void OnPanStateChanged(PanGestureRecognizer pan)
    {
        if (pan.State == GestureState.Began)
        {
            _scrollVelocityY = 0f;
        }
        else if (pan.State == GestureState.Changed)
        {
            _scrollOffsetY -= pan.DeltaY;
            ClampScroll();
        }
        else if (pan.State == GestureState.Ended)
        {
            _scrollVelocityY = -pan.VelocityY;
        }
    }

    public override void Update(float dt)
    {
        UpdatePhysics(dt);
        Content?.Update(dt);
    }

    /// <summary>
    /// Updates kinetic inertial scrolling for a single discrete animation frame.
    /// </summary>
    public void UpdatePhysics(float dt)
    {
        if (dt <= 0f) return;

        if (MathF.Abs(_scrollVelocityY) > GestureKernels.VelocityEpsilon)
        {
            _scrollOffsetY += _scrollVelocityY * dt;
            ClampScroll();

            GestureKernels.ApplyInertiaDamping(
                MemoryMarshal.CreateSpan(ref _scrollVelocityY, 1),
                MathF.Pow(Friction, dt * 120.0f));
        }
    }

    /// <summary>
    /// Applies SIMD-accelerated inertial damping across a batch of scroll velocities.
    /// </summary>
    public static void UpdatePhysicsBatch(Span<float> velocities, float dt, float friction = 0.92f)
    {
        if (dt <= 0f || velocities.IsEmpty) return;
        GestureKernels.ApplyInertiaDamping(velocities, MathF.Pow(friction, dt * 120.0f));
    }

    private void ClampScroll()
    {
        _scrollOffsetY = Math.Clamp(_scrollOffsetY, 0f, MaxScrollY);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        base.Measure(availableWidth, availableHeight);

        if (Content != null)
        {
            Content.Measure(ActualWidth, float.PositiveInfinity);
            MaxScrollY = MathF.Max(0f, Content.ActualHeight - ActualHeight);
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (Content != null)
        {
            Content.Arrange(X, Y, ActualWidth, Content.ActualHeight);
            MaxScrollY = MathF.Max(0f, Content.ActualHeight - ActualHeight);
        }
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        base.Render(canvas);

        if (Content != null)
        {
            canvas.Save();
            var clipRect = new SKRect(X, Y, X + ActualWidth, Y + ActualHeight);
            canvas.ClipRect(clipRect);
            canvas.Translate(0f, -_scrollOffsetY);

            Content.Render(canvas);

            canvas.Restore();
        }
    }

    public override MobileView? HitTest(float px, float py)
    {
        if (!IsVisible || !IsEnabled) return null;
        if (px >= X && px <= X + ActualWidth && py >= Y && py <= Y + ActualHeight)
        {
            if (Content != null)
            {
                var hit = Content.HitTest(px, py + _scrollOffsetY);
                if (hit != null) return hit;
            }
            return this;
        }
        return null;
    }
}
