using SoundFinder.Core.Audio;

namespace SoundFinder.Core.Tests;

public class FftTests
{
    [Fact]
    public void SineWave_PeaksAtExpectedBin()
    {
        const int fftSize = 1024;
        const int sampleRate = 8192;
        const double frequency = 1000.0;
        var samples = new float[fftSize];
        for (var i = 0; i < fftSize; i++)
        {
            samples[i] = (float)Math.Sin(2.0 * Math.PI * frequency * i / sampleRate);
        }

        var magnitudes = Fft.Magnitudes(samples, fftSize);

        var peakBin = 0;
        for (var i = 1; i < magnitudes.Length; i++)
        {
            if (magnitudes[i] > magnitudes[peakBin])
            {
                peakBin = i;
            }
        }
        var expectedBin = (int)Math.Round(frequency * fftSize / sampleRate);
        Assert.Equal(expectedBin, peakBin);
        Assert.True(magnitudes[peakBin] > 0.4);
    }

    [Fact]
    public void ComputeRadix2_RejectsNonPowerOfTwo()
    {
        Assert.Throws<ArgumentException>(() => Fft.ComputeRadix2(new double[100], new double[100]));
    }

    [Fact]
    public void HannWindow_IsSymmetricAndBounded()
    {
        var window = Fft.HannWindow(256);
        Assert.Equal(256, window.Length);
        Assert.All(window, w => Assert.InRange(w, 0.0, 1.0));
        for (var i = 0; i < 128; i++)
        {
            Assert.Equal(window[i], window[255 - i], 6);
        }
    }
}
