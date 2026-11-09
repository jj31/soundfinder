using SoundFinder.Core.Audio;

namespace SoundFinder.Core.Tests;

public class WaveformPeaksTests
{
    [Fact]
    public void ToMono_AveragesChannels()
    {
        var interleaved = new float[] { 1f, 3f, -1f, 1f };
        var mono = WaveformPeaks.ToMono(interleaved, 2);
        Assert.Equal(new float[] { 2f, 0f }, mono);
    }

    [Fact]
    public void ComputeMinMax_ReturnsInterleavedMinMax()
    {
        var mono = new float[] { 0f, 0.5f, -0.5f, 0.1f, -0.1f, 0.9f, -0.9f, 0f };
        var peaks = WaveformPeaks.ComputeMinMax(mono, 2);
        Assert.Equal(4, peaks.Length);
        Assert.Equal(-0.5f, peaks[0]);
        Assert.Equal(0.5f, peaks[1]);
        Assert.Equal(-0.9f, peaks[2]);
        Assert.Equal(0.9f, peaks[3]);
    }

    [Fact]
    public void ComputeMinMax_HandlesShortInput()
    {
        var peaks = WaveformPeaks.ComputeMinMax(new float[] { 0.3f }, 100);
        Assert.Equal(200, peaks.Length);
        Assert.Equal(0.3f, peaks[0]);
        Assert.Equal(0.3f, peaks[1]);
    }

    [Fact]
    public void Downsample_ReducesLength()
    {
        var mono = Enumerable.Range(0, 1000).Select(i => (float)i / 1000f).ToArray();
        var down = WaveformPeaks.Downsample(mono, 100);
        Assert.Equal(100, down.Length);
        Assert.All(down, v => Assert.InRange(v, 0f, 1f));
    }
}
