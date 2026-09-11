namespace Glacier.Mobile.Tests;

using System;
using Glacier.Mobile.Interop;
using Glacier.Mobile.Rendering;
using Glacier.Plot.Figures;
using Glacier.Polaris;
using Glacier.Polaris.Data;
using Xunit;

public class InteropTests
{
    [Fact]
    public void PlotView_IntegratesWithGlacierPlot()
    {
        using var compositor = new HeadlessCompositor(400, 300);

        var figure = new Figure { Title = "Mobile Plot" };
        var yData = new float[] { 10f, 25f, 15f, 40f, 30f };
        figure.PlotSignal(yData, label: "Telemetry");

        var plotView = new PlotView(figure) { Width = 380f, Height = 250f };

        compositor.BeginFrame();
        compositor.RenderTree(plotView, 400f, 300f);
        compositor.EndFrame();

        Assert.Equal(1, compositor.RenderedFrameCount);
    }

    [Fact]
    public void TelemetryView_BindsPolarisDataFrame()
    {
        using var compositor = new HeadlessCompositor(400, 400);

        var valSeries = new Float32Series("Value", 5);
        valSeries[0] = 1.0f;
        valSeries[1] = 3.5f;
        valSeries[2] = 2.0f;
        valSeries[3] = 4.5f;
        valSeries[4] = 6.0f;

        var df = new DataFrame([valSeries]);

        var telemetryView = new TelemetryView("Sensor Monitor");
        telemetryView.BindDataFrame(df, "Value");

        compositor.BeginFrame();
        compositor.RenderTree(telemetryView, 400f, 400f);
        compositor.EndFrame();

        Assert.Equal(1, compositor.RenderedFrameCount);
    }
}
