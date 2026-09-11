namespace Glacier.Mobile.UI.Layouts;

using System;
using Glacier.Mobile.Input;
using SkiaSharp;

/// <summary>
/// Kinetic scrollable viewport delivering locked 120Hz flick scrolling with SIMD inertial physics.
/// </summary>
public sealed class ScrollView : MobileView
{
    private readonly PanGestureRecognizer _panRecognizer;
    private float _scrollVelocityY;
    private float _scrollOffsetY;

    public MobileView? Content { get; set; }
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
        if (pan.State == GestureState.Changed)
        {
            _scrollOffsetY -= pan.VelocityY * 0.008f;
            ClampScroll();
        }
        else if (pan.State == GestureState.Ended)
        {
            _scrollVelocityY = -pan.VelocityY;
        }
    }

    /// <summary>
    /// Updates kinetic inertial scrolling for a single discrete animation frame.
    /// </summary>
    public void UpdatePhysics(float dt)
    {
        if (MathF.Abs(_scrollVelocityY) > GestureKernels.VelocityEpsilon)
        {
            _scrollOffsetY += _scrollVelocityY * dt;
            ClampScroll();

            Span<float> vel = stackalloc float[1] { _scrollVelocityY };
            GestureKernels.ApplyInertiaDamping(vel, MathF.Pow(Friction, dt * 120.0f));
            _scrollVelocityY = vel[0];
        }
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
