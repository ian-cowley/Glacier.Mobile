namespace Glacier.Mobile.Benchmarks;

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Glacier.Mobile.Input;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Mobile.UI.Navigation;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<MobileBenchmarks>();
    }
}

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class MobileBenchmarks
{
    private const int VelocityCount = 10000;
    private float[] _velocities = null!;

    private TouchDispatcher _dispatcher = null!;
    private PanGestureRecognizer _panRecognizer = null!;
    private HeadlessCompositor _compositor = null!;
    private MobilePage _screen = null!;

    [GlobalSetup]
    public void Setup()
    {
        _velocities = new float[VelocityCount];
        var rng = new Random(42);
        for (int i = 0; i < VelocityCount; i++)
        {
            _velocities[i] = (float)(rng.NextDouble() * 1000.0 - 500.0);
        }

        _dispatcher = new TouchDispatcher();
        _panRecognizer = new PanGestureRecognizer();
        _dispatcher.RegisterRecognizer(_panRecognizer);

        _compositor = new HeadlessCompositor(393, 852);
        _screen = new Screen("Dashboard")
        {
            Content = new Column(spacing: 12f)
            {
                new Header("Glacier.Mobile Benchmark"),
                new Label("120Hz Native AOT Mobile UI Pipeline"),
                new Button("Action 1"),
                new Button("Action 2"),
                new Row(spacing: 8f)
                {
                    new Label("Sub-item A"),
                    new Label("Sub-item B")
                }
            }
        };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _compositor.Dispose();
    }

    [Benchmark(Description = "Touch Input Dispatch (10,000 Events)")]
    public void Benchmark_TouchDispatch()
    {
        for (int i = 0; i < 1000; i++)
        {
            _dispatcher.Dispatch(new TouchEvent(0, 100f + i, 200f + i, TouchPhase.Moved));
        }
    }

    [Benchmark(Description = "SIMD Inertia Damping (10,000 Velocities)")]
    public void Benchmark_InertiaDamping_SIMD()
    {
        GestureKernels.ApplyInertiaDamping(_velocities, 0.95f);
    }

    [Benchmark(Description = "Visual Tree Layout & GPU Render (Full Screen)")]
    public void Benchmark_RenderTree()
    {
        _compositor.BeginFrame();
        _compositor.RenderTree(_screen, 393f, 852f);
        _compositor.EndFrame();
    }
}
