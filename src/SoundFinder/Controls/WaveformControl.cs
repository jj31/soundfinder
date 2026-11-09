using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SoundFinder.Controls;

public sealed class WaveformControl : FrameworkElement
{
    public static readonly DependencyProperty PeaksProperty = DependencyProperty.Register(
        nameof(Peaks), typeof(float[]), typeof(WaveformControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PlayheadFractionProperty = DependencyProperty.Register(
        nameof(PlayheadFraction), typeof(double), typeof(WaveformControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public float[]? Peaks
    {
        get => (float[]?)GetValue(PeaksProperty);
        set => SetValue(PeaksProperty, value);
    }

    public double PlayheadFraction
    {
        get => (double)GetValue(PlayheadFractionProperty);
        set => SetValue(PlayheadFractionProperty, value);
    }

    public event Action<double>? SeekRequested;

    private static readonly Brush WaveBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x86, 0xC1));
    private static readonly Brush PlayheadBrush = new SolidColorBrush(Colors.OrangeRed);

    static WaveformControl()
    {
        WaveBrush.Freeze();
        PlayheadBrush.Freeze();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x1B, 0x1B, 0x1B)), null, new Rect(0, 0, ActualWidth, ActualHeight));
        var peaks = Peaks;
        if (peaks is null || peaks.Length < 2 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }
        var midY = ActualHeight / 2.0;
        var peakCount = peaks.Length / 2;
        var xStep = ActualWidth / peakCount;
        var pen = new Pen(WaveBrush, Math.Max(1.0, xStep * 0.8));
        for (var i = 0; i < peakCount; i++)
        {
            var min = peaks[i * 2];
            var max = peaks[i * 2 + 1];
            var y1 = midY - max * midY;
            var y2 = midY - min * midY;
            dc.DrawLine(pen, new Point(i * xStep, y1), new Point(i * xStep, Math.Max(y1 + 1, y2)));
        }
        if (PlayheadFraction is >= 0 and <= 1)
        {
            var x = PlayheadFraction * ActualWidth;
            dc.DrawLine(new Pen(PlayheadBrush, 1.5), new Point(x, 0), new Point(x, ActualHeight));
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (ActualWidth > 0)
        {
            SeekRequested?.Invoke(Math.Clamp(e.GetPosition(this).X / ActualWidth, 0.0, 1.0));
        }
    }
}
