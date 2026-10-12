using System.Globalization;
using System.Windows;
using System.Windows.Media;
using FeizaiTracker.App.Models;

namespace FeizaiTracker.App.Controls;

public sealed class TrendChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(IEnumerable<ChartPoint>),
        typeof(TrendChart),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target),
        typeof(double),
        typeof(TrendChart),
        new FrameworkPropertyMetadata(
            0d,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<ChartPoint>? Items
    {
        get => (IEnumerable<ChartPoint>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public double Target
    {
        get => (double)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var points = Items?.ToList() ?? [];
        if (points.Count == 0 || ActualWidth < 100 || ActualHeight < 100)
        {
            return;
        }

        const double left = 45;
        const double right = 12;
        const double top = 18;
        const double bottom = 30;
        var plotWidth = ActualWidth - left - right;
        var plotHeight = ActualHeight - top - bottom;
        if (plotWidth <= 0 || plotHeight <= 0)
        {
            return;
        }

        var maximum = Math.Max(
            points.Max(point => (double)point.Value),
            Target);
        maximum = Math.Max(100, Math.Ceiling(maximum * 1.15 / 250) * 250);

        var gridBrush = new SolidColorBrush(Color.FromRgb(229, 234, 229));
        var labelBrush = new SolidColorBrush(Color.FromRgb(122, 133, 125));
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var index = 0; index <= 3; index++)
        {
            var y = top + plotHeight * index / 3;
            drawingContext.DrawLine(
                new Pen(gridBrush, 1),
                new Point(left, y),
                new Point(left + plotWidth, y));

            var labelValue = maximum * (3 - index) / 3;
            var label = CreateText($"{labelValue:0}", 10, labelBrush, dpi);
            drawingContext.DrawText(label, new Point(left - label.Width - 7, y - label.Height / 2));
        }

        if (Target > 0)
        {
            var targetY = top + plotHeight - Target / maximum * plotHeight;
            var targetPen = new Pen(
                new SolidColorBrush(Color.FromRgb(255, 179, 92)),
                1.5)
            {
                DashStyle = DashStyles.Dash
            };
            drawingContext.DrawLine(
                targetPen,
                new Point(left, targetY),
                new Point(left + plotWidth, targetY));
        }

        var slotWidth = plotWidth / points.Count;
        var barWidth = Math.Max(2, Math.Min(24, slotWidth * 0.58));
        var barBrush = new SolidColorBrush(Color.FromRgb(61, 170, 104));
        var todayBrush = new SolidColorBrush(Color.FromRgb(28, 111, 67));
        var labelEvery = points.Count <= 7 ? 1 : points.Count <= 31 ? 5 : 15;

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            var height = (double)point.Value / maximum * plotHeight;
            var x = left + index * slotWidth + (slotWidth - barWidth) / 2;
            var y = top + plotHeight - height;
            var rectangle = new Rect(x, y, barWidth, Math.Max(1, height));
            drawingContext.DrawRoundedRectangle(
                point.Date.Date == DateTime.Today ? todayBrush : barBrush,
                null,
                rectangle,
                Math.Min(4, barWidth / 2),
                Math.Min(4, barWidth / 2));

            if (index % labelEvery == 0 || index == points.Count - 1)
            {
                var labelText = points.Count <= 7
                    ? point.Date.ToString("ddd")[..1]
                    : point.Date.ToString("d MMM");
                var label = CreateText(labelText, 10, labelBrush, dpi);
                var labelX = left + index * slotWidth + slotWidth / 2 - label.Width / 2;
                drawingContext.DrawText(label, new Point(labelX, top + plotHeight + 8));
            }
        }
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
}
