![Glacier.Mobile Banner](assets/banner.jpg)

# 📱 Glacier.Mobile

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-Ready-brightgreen.svg)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![NuGet Version](https://img.shields.io/nuget/v/Glacier.Mobile.svg)](https://www.nuget.org/packages/Glacier.Mobile/)
[![Ecosystem](https://img.shields.io/badge/Glacier-Ecosystem-blue)](https://github.com/ian-cowley)
[![Website](https://img.shields.io/badge/Website-glaciersoftware.dev-66fcf1.svg)](https://glaciersoftware.dev/)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Support-orange.svg?logo=buy-me-a-coffee)](https://buymeacoffee.com/glaciersoftware)

> **Headless .NET 10 Native AOT UI, Layout, and Touch/Gesture Pipeline Core**

`Glacier.Mobile` is a `net10.0` headless mobile UI layout tree, touch-dispatch pipeline, and SkiaSharp composition core built for .NET 10 and Native AOT compatibility. It provides stack-allocated touch event payloads, a lock-free SPSC `TouchRingBuffer`, gesture recognizers (`Tap`, `Pan`, `Pinch`), SIMD inertial damping kernels, and a swapchain HAL (`IMobileSwapchain` / `ICompositor`) that ships in-repo with CPU `HeadlessSwapchain` and `HeadlessCompositor` for deterministic off-screen rendering, testing, and benchmarks.

> **Scope Note:** This package targets `net10.0` only. It does **not** ship `net10.0-android` / `net10.0-ios` target frameworks, platform windowing hosts, or `GRContext` Vulkan/Metal GPU backends; platform hosts plug in through `IMobileSwapchain` and `ICompositor`.

---

## 1. Core Architecture

- **Retained-Mode Layout Tree**: `MobileView`, `StackLayout` (`Column` / `Row`), `ScrollView`, `NavigationStack`, `MobilePage` (`Screen`), `Label`, `Header`, `Button`, `PlotView`, and `TelemetryView`.
- **120Hz Kinetic Physics**: `MobileApplication.Step(dt)` drains queued touch events, ticks `ScrollView.UpdatePhysics(dt)` via `GestureKernels.ApplyInertiaDamping` (portable `Vector512`/`Vector256`/`Vector128` SIMD for batch spans and scalar fast-paths for `N=1`/`N=2`), and renders the visual tree.
- **Zero-Allocation Touch Pipeline**: Stack-only `readonly ref struct TouchEvent` payloads, cache-line-isolated SPSC `TouchRingBuffer` with `CachedRemote` shadow cursors and batch `Span.CopyTo` draining, and `PointerCaptureTable`.

---

## 🖼️ Visual Gallery: Headless Rendered Snapshot

<div align="center">

| Glacier.Mobile System Monitor (Headless SkiaSharp Render) |
| :---: |
| <img src="docs/images/glacier_mobile_dashboard.png" width="393" alt="Glacier.Mobile System Monitor" /> |
| *Headless SkiaSharp render with Polaris DataFrame telemetry stream, diagnostics, and touch controls* |

</div>

---

## 2. Zero-Allocation Touch & Gesture Input Pipeline

```csharp
[StructLayout(LayoutKind.Sequential)]
public readonly ref struct TouchEvent
{
    public readonly long Timestamp;
    public readonly int PointerId;
    public readonly float X;
    public readonly float Y;
    public readonly TouchPhase Phase;
}
```
Touch events flow through `TouchRingBuffer`, `TouchDispatcher`, and `PointerCaptureTable` to `TapGestureRecognizer`, `PanGestureRecognizer` (with incremental `DeltaX`/`DeltaY`), and `PinchGestureRecognizer` (with incremental `DeltaScale` and surviving-finger tracking across single-finger lifts), with touch bubbling from child controls like `Button` to parent `ScrollView` containers.

---

## 3. Quickstart API

```csharp
using Glacier.Mobile.Core;
using Glacier.Mobile.Interop;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Mobile.UI.Navigation;
using Glacier.Polaris;
using Glacier.Polaris.Data;

public sealed class AppShell : MobileApplication
{
    protected override MobileView Build()
    {
        var telemetry = new TelemetryView("Real-Time Telemetry");
        var values = new Float32Series("Value", 32);
        for (int i = 0; i < 32; i++) values[i] = MathF.Sin(i * 0.25f) * 10f + 50f;
        telemetry.BindDataFrame(new DataFrame([values]), "Value");

        return new NavigationStack
        {
            new Screen("Dashboard")
            {
                Content = new ScrollView
                {
                    Content = new Column(spacing: 16f)
                    {
                        new Header("System Monitor"),
                        telemetry,
                        new Button("Export Snapshot", onClick: () => { })
                    }
                }
            }
        };
    }
}
```

---

## 4. Ecosystem Cross-References

- **[Glacier Ecosystem Website](https://glaciersoftware.dev/)**: Unified .NET 10 high-performance architecture overview.
- **[Glacier.Desktop](https://github.com/ian-cowley/Glacier.Desktop)**: Native desktop GUI engine and virtualized 1M-row data grids.
- **[Glacier.Plot](https://github.com/ian-cowley/Glacier.Plot)**: High-speed charts integrated via `PlotView`.
- **[Glacier.Polaris](https://github.com/ian-cowley/Glacier.Polaris)**: Embedded columnar analytics powering `TelemetryView`.

---

## 🆕 What's New in v1.1.1

- **Cached `PlotView` Framebuffer/Canvas & Straight Alpha** — reuses `LinearFramebuffer` and `CpuGraphicsCanvas` across same-size frames (`IDisposable`), eliminating per-frame 4 MB `GlyphAtlas` LOH allocations and switching pixel wrapping to `SKAlphaType.Unpremul`.
- **`[ThreadStatic]` Skia Scratch Pooling & `TelemetryView` Zero-Copy Binding** — pools `SKPaint`/`SKRoundRect` in `MobileView`/`Button`/`Label`, uses `col.AsFloatMemory()` in `TelemetryView.BindDataFrame`, and disposes replaced `Figure` instances.
- **Touch Bubbling & `PinchGestureRecognizer` Single-Finger Lift** — bubbles `Button` touches cleanly to parent `ScrollView` containers while suppressing parent bubbling during active `PlotView` pan/pinch, and preserves the surviving finger when one pinch pointer lifts.
- **Self-Contained `TouchRingBuffer` Shadow Cursors & Batch `Drain`** — adds 128-byte `PaddedCursor` (`CachedRemote`) isolation and 2-segment `Span.CopyTo` draining.
- **Aligned Dependencies (`Glacier.Polaris 1.2.6`, `Glacier.Plot 1.2.1`)** and platform-neutral core NuGet packaging (Linux Skia native assets live in sample/test/benchmark projects).

---

## Credits

Developed by Ian Cowley and Antigravity (Google DeepMind).

---

## License

Licensed under the [MIT License](LICENSE). Copyright (c) 2026 Ian Cowley.

---

## ❄️ Glacier Ecosystem & Support

Part of the **[Glacier Ecosystem](https://glaciersoftware.dev/)** — high-performance, zero-allocation developer tooling and data engines natively built for modern C# and .NET 10.

* 🌐 **Website & Documentation**: [glaciersoftware.dev](https://glaciersoftware.dev/)
* ☕ **Buy Me a Coffee**: [buymeacoffee.com/glaciersoftware](https://buymeacoffee.com/glaciersoftware)
* ❤️ **GitHub Sponsors**: [github.com/sponsors/ian-cowley](https://github.com/sponsors/ian-cowley)
* 🐙 **All Repositories**: [github.com/ian-cowley](https://github.com/ian-cowley)
