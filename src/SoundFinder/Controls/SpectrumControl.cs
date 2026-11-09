using System.Windows;
using System.Windows.Media;

namespace SoundFinder.Controls;

public sealed class SpectrumControl : FrameworkElement
{
    public static readonly DependencyProperty MagnitudesProperty = DependencyProperty.Register(
        nameof(Magnitudes), typeof(double[]), typeof(SpectrumControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public double[]? Magnitudes
    {
        get => (double[]?)GetValue(MagnitudesProperty);
        set => SetValue(MagnitudesProperty, value);
    }

    private static readonly Brush BarBrush = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

    static SpectrumControl()
    {
        BarBrush.Freeze();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x12)), null, new Rect(0, 0, ActualWidth, ActualHeight));
        var magnitudes = Magnitudes;
        if (magnitudes is null || magnitudes.Length == 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }
        var barWidth = Math.Max(1.0, ActualWidth / magnitudes.Length);
        var max = magnitudes.Max();
        if (max <= 0)
        {
            return;
        }
        for (var i = 0; i < magnitudes.Length; i++)
        {
            var normalized = magnitudes[i] / max;
            var log = Math.Log10(1.0 + 9.0 * normalized);
            var height = log * ActualHeight;
            dc.DrawRectangle(BarBrush, null, new Rect(i * barWidth, ActualHeight - height, Math.Max(barWidth - 0.5, 0.5), height));
        }
    }
}
