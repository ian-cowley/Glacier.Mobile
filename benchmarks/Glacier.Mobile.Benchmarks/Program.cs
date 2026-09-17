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
        if (args.Length > 0 && args[0] == "--standalone")
        {
            RunStandaloneBenchmark();
            return;
        }

        BenchmarkRunner.Run<MobileBenchmarks>();
    }

    private static void RunStandaloneBenchmark()
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("  GLACIER.MOBILE PHYSICAL HARDWARE BENCHMARK SUITE");
        Console.WriteLine("===============================================================================\n");

        // 1. Low-pointer inertia damping (N=1, N=2, N=4)
        Console.WriteLine("--- 1. Low-Pointer Inertia Damping Fast-Paths ---");
        const int lowPointerIterations = 10_000_000;

        float[] v1 = [ 250.0f ];
        float[] v2 = [ 250.0f, -180.0f ];
        float[] v4 = [ 250.0f, -180.0f, 150.0f, -90.0f ];

        // Warmup
        for (int w = 0; w < 100_000; w++)
        {
            GestureKernels.ApplyInertiaDamping(v1.AsSpan(), 0.95f);
            GestureKernels.ApplyInertiaDamping(v2.AsSpan(), 0.95f);
            GestureKernels.ApplyInertiaDamping(v4.AsSpan(), 0.95f);
        }

        // N=1 (Single 1D Touch Velocity)
        v1[0] = 250.0f;
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        for (int it = 0; it < lowPointerIterations; it++)
        {
            GestureKernels.ApplyInertiaDamping(v1.AsSpan(), 0.95f);
            v1[0] += 0.001f;
        }
        sw1.Stop();
        double nsPerCall1 = (sw1.Elapsed.TotalMilliseconds * 1_000_000.0) / lowPointerIterations;
        double opsPerSec1 = lowPointerIterations / sw1.Elapsed.TotalSeconds;
        Console.WriteLine($"[N=1 Single 1D Touch Velocity]:  {sw1.Elapsed.TotalMilliseconds:F2} ms for {lowPointerIterations:N0} ops | Latency: {nsPerCall1:F3} ns/op | Throughput: {opsPerSec1 / 1e6:F2} M ops/s");

        // N=2 (Single 2D Touch Velocity X/Y)
        v2[0] = 250.0f; v2[1] = -180.0f;
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (int it = 0; it < lowPointerIterations; it++)
        {
            GestureKernels.ApplyInertiaDamping(v2.AsSpan(), 0.95f);
            v2[0] += 0.001f;
        }
        sw2.Stop();
        double nsPerCall2 = (sw2.Elapsed.TotalMilliseconds * 1_000_000.0) / lowPointerIterations;
        double opsPerSec2 = lowPointerIterations / sw2.Elapsed.TotalSeconds;
        Console.WriteLine($"[N=2 Single 2D Touch Velocity]:  {sw2.Elapsed.TotalMilliseconds:F2} ms for {lowPointerIterations:N0} ops | Latency: {nsPerCall2:F3} ns/op | Throughput: {opsPerSec2 / 1e6:F2} M ops/s");

        // N=4 (Dual-Touch Pinch/Rotate 2D)
        v4[0] = 250.0f; v4[1] = -180.0f; v4[2] = 150.0f; v4[3] = -90.0f;
        var sw4 = System.Diagnostics.Stopwatch.StartNew();
        for (int it = 0; it < lowPointerIterations; it++)
        {
            GestureKernels.ApplyInertiaDamping(v4.AsSpan(), 0.95f);
            v4[0] += 0.001f;
        }
        sw4.Stop();
        double nsPerCall4 = (sw4.Elapsed.TotalMilliseconds * 1_000_000.0) / lowPointerIterations;
        double opsPerSec4 = lowPointerIterations / sw4.Elapsed.TotalSeconds;
        Console.WriteLine($"[N=4 Dual-Touch Velocity]:       {sw4.Elapsed.TotalMilliseconds:F2} ms for {lowPointerIterations:N0} ops | Latency: {nsPerCall4:F3} ns/op | Throughput: {opsPerSec4 / 1e6:F2} M ops/s");

        // 2. Batch SIMD Inertia Damping (N=10,000)
        Console.WriteLine("\n--- 2. Batch SIMD Inertia Damping (10,000 Velocities) ---");
        const int batchSize = 10_000;
        const int batchIterations = 50_000;
        float[] batchVel = new float[batchSize];
        var rng = new Random(42);
        for (int i = 0; i < batchSize; i++) batchVel[i] = (float)(rng.NextDouble() * 1000.0 - 500.0);

        for (int w = 0; w < 500; w++)
            GestureKernels.ApplyInertiaDamping(batchVel.AsSpan(), 0.95f);

        var swBatch = System.Diagnostics.Stopwatch.StartNew();
        for (int it = 0; it < batchIterations; it++)
        {
            GestureKernels.ApplyInertiaDamping(batchVel.AsSpan(), 0.95f);
        }
        swBatch.Stop();
        double usPerBatch = (swBatch.Elapsed.TotalMilliseconds * 1000.0) / batchIterations;
        double itemsPerSec = (double)batchSize * batchIterations / swBatch.Elapsed.TotalSeconds;
        Console.WriteLine($"[10,000 Vector512 SIMD Damping]: {swBatch.Elapsed.TotalMilliseconds:F2} ms for {batchIterations:N0} runs | Latency: {usPerBatch:F3} μs/batch | Throughput: {itemsPerSec / 1e6:F2} M items/s");

        // 3. Touch Input Dispatch
        Console.WriteLine("\n--- 3. Touch Input Dispatch (1,000,000 Events) ---");
        var dispatcher = new TouchDispatcher();
        var panRecognizer = new PanGestureRecognizer();
        dispatcher.RegisterRecognizer(panRecognizer);

        const int eventCount = 1_000_000;
        for (int w = 0; w < 10_000; w++)
            dispatcher.Dispatch(new TouchEvent(0, 100f + w, 200f + w, TouchPhase.Moved));

        var swDisp = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < eventCount; i++)
        {
            dispatcher.Dispatch(new TouchEvent(0, 100f + (i % 500), 200f + (i % 500), TouchPhase.Moved));
        }
        swDisp.Stop();
        double nsPerEvent = (swDisp.Elapsed.TotalMilliseconds * 1_000_000.0) / eventCount;
        double eventsPerSec = eventCount / swDisp.Elapsed.TotalSeconds;
        Console.WriteLine($"[Touch Dispatcher]:             {swDisp.Elapsed.TotalMilliseconds:F2} ms for {eventCount:N0} events | Latency: {nsPerEvent:F3} ns/event | Throughput: {eventsPerSec / 1e6:F2} M events/s");

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("  GLACIER.MOBILE PHYSICAL BENCHMARK COMPLETE");
        Console.WriteLine("===============================================================================\n");
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
