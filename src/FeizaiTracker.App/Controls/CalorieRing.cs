using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FeizaiTracker.App.Controls;

public sealed class CalorieRing : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(CalorieRing),
        new FrameworkPropertyMetadata(
            0d,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(CalorieRing),
        new FrameworkPropertyMetadata(
            2000d,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
        {
            return;
        }

        var centre = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = Math.Max(0, size / 2 - 11);
        var backgroundPen = new Pen(new SolidColorBrush(Color.FromRgb(225, 232, 225)), 13)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        drawingContext.DrawEllipse(null, backgroundPen, centre, radius, radius);

        var progress = Maximum <= 0 ? 0 : Math.Clamp(Value / Maximum, 0, 1);
        var progressPen = new Pen(new SolidColorBrush(Color.FromRgb(61, 170, 104)), 13)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        if (progress >= 0.999)
        {
            drawingContext.DrawEllipse(null, progressPen, centre, radius, radius);
        }
        else if (progress > 0)
        {
            DrawArc(drawingContext, centre, radius, progress * 360, progressPen);
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var valueText = CreateText(
            $"{Value:0}",
            30,
            FontWeights.SemiBold,
            new SolidColorBrush(Color.FromRgb(23, 37, 29)),
            dpi);
        drawingContext.DrawText(
            valueText,
            new Point(centre.X - valueText.Width / 2, centre.Y - valueText.Height));

        var unitText = CreateText(
            "kcal consumed",
            12,
            FontWeights.Normal,
            new SolidColorBrush(Color.FromRgb(105, 119, 110)),
            dpi);
        drawingContext.DrawText(
            unitText,
            new Point(centre.X - unitText.Width / 2, centre.Y + 7));
    }

    private static void DrawArc(
        DrawingContext drawingContext,
        Point centre,
        double radius,
        double angle,
        Pen pen)
    {
        var start = new Point(centre.X, centre.Y - radius);
        var radians = (angle - 90) * Math.PI / 180;
        var end = new Point(
            centre.X + radius * Math.Cos(radians),
            centre.Y + radius * Math.Sin(radians));

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(
                end,
                new Size(radius, radius),
                0,
                angle > 180,
                SweepDirection.Clockwise,
                true,
                false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);
    }

    private static FormattedText CreateText(
        string value,
        double size,
        FontWeight weight,
        Brush brush,
        double pixelsPerDip) =>
        new(
            value,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI Variable Display"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            pixelsPerDip);
}
