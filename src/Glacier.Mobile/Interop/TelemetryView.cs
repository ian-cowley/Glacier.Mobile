namespace Glacier.Mobile.Interop;

using System;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Plot.Interop;
using Glacier.Polaris;
using Glacier.Polaris.Data;

/// <summary>
/// Real-time dashboard view consuming Glacier.Polaris DataFrames.
/// Displays tabular summaries and streaming telemetry statistics.
/// </summary>
public sealed class TelemetryView : Column, IDisposable
{
    private readonly Label _titleLabel;
    private readonly Label _statsLabel;
    private readonly PlotView _plotView;
    private bool _disposed;

    public TelemetryView(string title = "Live Telemetry") : base(spacing: 12f)
    {
        BackgroundColor = Color4.CardBackground;
        Padding = new Thickness(16f);
        Margin = new Thickness(0f, 8f);

        _titleLabel = new Header(title);
        _statsLabel = new Label("Awaiting data stream...") { FontSize = 14f, TextColor = new Color4(180, 190, 205, 255) };
        _plotView = new PlotView { Height = 220f };

        Add(_titleLabel);
        Add(_statsLabel);
        Add(_plotView);
    }

    /// <summary>
    /// Binds a Glacier.Polaris DataFrame to update telemetry statistics and charts.
    /// </summary>
    public void BindDataFrame(DataFrame df, string metricColumnName = "Value")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(df);

        int rowCount = df.RowCount;
        var col = df.Columns.Find(c => c.Name == metricColumnName);

        if (col != null && rowCount > 0)
        {
            ReadOnlyMemory<float> values = col.AsFloatMemory();
            if (values.Length > rowCount)
            {
                values = values.Slice(0, rowCount);
            }

            var nextFigure = new Glacier.Plot.Figures.Figure();
            nextFigure.PlotSignal(values, label: metricColumnName);
            _plotView.SetOwnedFigure(nextFigure);

            float lastVal = values.IsEmpty ? 0f : values.Span[^1];
            _statsLabel.Text = $"Rows: {rowCount:N0} | Current: {lastVal:F2} | Stream: Active";
        }
        else
        {
            _statsLabel.Text = $"DataFrame: {df.RowCount:N0} rows, {df.Columns.Count} columns";
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _plotView.Dispose();
            _disposed = true;
        }
    }
}
