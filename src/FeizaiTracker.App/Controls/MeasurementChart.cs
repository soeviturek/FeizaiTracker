using System.Globalization;
using System.Windows;
using System.Windows.Media;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Controls;

public sealed class MeasurementChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(IEnumerable<ChartPoint>),
        typeof(MeasurementChart),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ChartTypeProperty = DependencyProperty.Register(
        nameof(ChartType),
        typeof(MeasurementChartType),
        typeof(MeasurementChart),
        new FrameworkPropertyMetadata(
            MeasurementChartType.Line,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit),
        typeof(string),
        typeof(MeasurementChart),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<ChartPoint>? Items
    {
        get => (IEnumerable<ChartPoint>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public MeasurementChartType ChartType
    {
        get => (MeasurementChartType)GetValue(ChartTypeProperty);
        set => SetValue(ChartTypeProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var points = Items?
            .OrderBy(point => point.Date)
            .ToList() ?? [];
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (points.Count == 0)
        {
            DrawEmptyState(drawingContext, dpi);
            return;
        }

        if (ActualWidth < 140 || ActualHeight < 120)
        {
            return;
        }

        const double left = 58;
        const double right = 42;
        const double top = 24;
        const double bottom = 34;
        var plotWidth = ActualWidth - left - right;
        var plotHeight = ActualHeight - top - bottom;
        if (plotWidth <= 0 || plotHeight <= 0)
        {
            return;
        }

        var values = points.Select(point => (double)point.Value).ToList();
        var (minimum, maximum) = GetBounds(values, ChartType);
        var gridBrush = new SolidColorBrush(Color.FromRgb(229, 234, 229));
        var labelBrush = new SolidColorBrush(Color.FromRgb(105, 119, 110));
        var greenBrush = new SolidColorBrush(Color.FromRgb(61, 170, 104));
        var darkGreenBrush = new SolidColorBrush(Color.FromRgb(28, 111, 67));
        var areaBrush = new SolidColorBrush(Color.FromArgb(46, 61, 170, 104));

        if (!string.IsNullOrWhiteSpace(Unit))
        {
            var unitLabel = CreateText(Unit, 10, labelBrush, dpi);
            drawingContext.DrawText(unitLabel, new Point(4, 2));
        }

        for (var index = 0; index <= 4; index++)
        {
            var y = top + plotHeight * index / 4;
            drawingContext.DrawLine(
                new Pen(gridBrush, 1),
                new Point(left, y),
                new Point(left + plotWidth, y));

            var value = maximum - (maximum - minimum) * index / 4;
            var label = CreateText(FormatValue(value), 10, labelBrush, dpi);
            drawingContext.DrawText(
                label,
                new Point(left - label.Width - 8, y - label.Height / 2));
        }

        var renderedPoints = points
            .Select((point, index) => new RenderedPoint(
                point,
                GetX(index, points.Count, left, plotWidth, ChartType),
                GetY((double)point.Value, minimum, maximum, top, plotHeight)))
            .ToList();

        switch (ChartType)
        {
            case MeasurementChartType.Area:
                DrawArea(
                    drawingContext,
                    renderedPoints,
                    top + plotHeight,
                    areaBrush,
                    darkGreenBrush);
                break;
            case MeasurementChartType.Bar:
                DrawBars(
                    drawingContext,
                    renderedPoints,
                    minimum,
                    maximum,
                    top,
                    plotHeight,
                    plotWidth,
                    greenBrush,
                    darkGreenBrush);
                break;
            default:
                DrawLine(drawingContext, renderedPoints, darkGreenBrush);
                break;
        }

        DrawDateLabels(drawingContext, renderedPoints, top + plotHeight + 9, labelBrush, dpi);
    }

    private void DrawEmptyState(DrawingContext drawingContext, double dpi)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var brush = new SolidColorBrush(Color.FromRgb(105, 119, 110));
        var text = CreateText("Add measurements to see your chart.", 12, brush, dpi);
        drawingContext.DrawText(
            text,
            new Point(
                Math.Max(0, (ActualWidth - text.Width) / 2),
                Math.Max(0, (ActualHeight - text.Height) / 2)));
    }

    private static void DrawLine(
        DrawingContext drawingContext,
        IReadOnlyList<RenderedPoint> points,
        Brush brush)
    {
        var pen = new Pen(brush, 2.5)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        for (var index = 1; index < points.Count; index++)
        {
            drawingContext.DrawLine(
                pen,
                new Point(points[index - 1].X, points[index - 1].Y),
                new Point(points[index].X, points[index].Y));
        }

        foreach (var point in points)
        {
            drawingContext.DrawEllipse(
                Brushes.White,
                new Pen(brush, 2),
                new Point(point.X, point.Y),
                4,
                4);
        }
    }

    private static void DrawArea(
        DrawingContext drawingContext,
        IReadOnlyList<RenderedPoint> points,
        double baseline,
        Brush fill,
        Brush line)
    {
        var area = new StreamGeometry();
        using (var context = area.Open())
        {
            context.BeginFigure(
                new Point(points[0].X, baseline),
                isFilled: true,
                isClosed: true);
            foreach (var point in points)
            {
                context.LineTo(new Point(point.X, point.Y), isStroked: true, isSmoothJoin: true);
            }

            context.LineTo(
                new Point(points[^1].X, baseline),
                isStroked: true,
                isSmoothJoin: true);
        }

        area.Freeze();
        drawingContext.DrawGeometry(fill, null, area);
        DrawLine(drawingContext, points, line);
    }

    private static void DrawBars(
        DrawingContext drawingContext,
        IReadOnlyList<RenderedPoint> points,
        double minimum,
        double maximum,
        double top,
        double plotHeight,
        double plotWidth,
        Brush brush,
        Brush latestBrush)
    {
        var baseline = GetY(0, minimum, maximum, top, plotHeight);
        var slotWidth = plotWidth / points.Count;
        var barWidth = Math.Max(3, Math.Min(36, slotWidth * 0.58));

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            var y = Math.Min(point.Y, baseline);
            var height = Math.Max(1, Math.Abs(point.Y - baseline));
            var rectangle = new Rect(point.X - barWidth / 2, y, barWidth, height);
            var cornerRadius = Math.Min(4, barWidth / 2);
            drawingContext.DrawRoundedRectangle(
                index == points.Count - 1 ? latestBrush : brush,
                null,
                rectangle,
                cornerRadius,
                cornerRadius);
        }
    }

    private static void DrawDateLabels(
        DrawingContext drawingContext,
        IReadOnlyList<RenderedPoint> points,
        double y,
        Brush brush,
        double dpi)
    {
        var labelEvery = Math.Max(1, (int)Math.Ceiling(points.Count / 6d));
        for (var index = 0; index < points.Count; index++)
        {
            if (index % labelEvery != 0 && index != points.Count - 1)
            {
                continue;
            }

            var label = CreateText(points[index].Point.Date.ToString("d MMM"), 10, brush, dpi);
            drawingContext.DrawText(
                label,
                new Point(points[index].X - label.Width / 2, y));
        }
    }

    private static (double Minimum, double Maximum) GetBounds(
        IReadOnlyList<double> values,
        MeasurementChartType chartType)
    {
        var dataMinimum = values.Min();
        var dataMaximum = values.Max();

        if (chartType == MeasurementChartType.Bar)
        {
            var minimum = Math.Min(0, dataMinimum);
            var maximum = Math.Max(0, dataMaximum);
            var span = maximum - minimum;
            if (span <= 0)
            {
                span = 1;
            }

            if (maximum > 0)
            {
                maximum += span * 0.1;
            }

            if (minimum < 0)
            {
                minimum -= span * 0.1;
            }

            return (minimum, maximum);
        }

        var range = dataMaximum - dataMinimum;
        var padding = range > 0
            ? range * 0.15
            : Math.Max(1, Math.Abs(dataMaximum) * 0.05);
        return (dataMinimum - padding, dataMaximum + padding);
    }

    private static double GetX(
        int index,
        int count,
        double left,
        double plotWidth,
        MeasurementChartType chartType)
    {
        if (chartType == MeasurementChartType.Bar)
        {
            return left + (index + 0.5) * plotWidth / count;
        }

        return count == 1
            ? left + plotWidth / 2
            : left + index * plotWidth / (count - 1);
    }

    private static double GetY(
        double value,
        double minimum,
        double maximum,
        double top,
        double plotHeight) =>
        top + (maximum - value) / (maximum - minimum) * plotHeight;

    private static string FormatValue(double value)
    {
        var absolute = Math.Abs(value);
        return absolute >= 1000
            ? value.ToString("N0", CultureInfo.CurrentCulture)
            : absolute >= 100
                ? value.ToString("0.#", CultureInfo.CurrentCulture)
                : value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static FormattedText CreateText(
        string value,
        double size,
        Brush brush,
        double pixelsPerDip) =>
        new(
            value,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Variable Text"),
            size,
            brush,
            pixelsPerDip);

    private sealed record RenderedPoint(ChartPoint Point, double X, double Y);
}
