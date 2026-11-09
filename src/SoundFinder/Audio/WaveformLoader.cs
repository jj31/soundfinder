using NAudio.Wave;
using SoundFinder.Core.Audio;

namespace SoundFinder.Audio;

public static class WaveformLoader
{
    public static float[] LoadPeaks(string path, int peakCount)
    {
        using var reader = new AudioFileReader(path);
        var channels = reader.WaveFormat.Channels;
        var totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
        var maxSamples = 8_000_000;
        var buffer = new float[Math.Min(totalSamples, maxSamples)];
        var read = reader.Read(buffer, 0, buffer.Length);
        if (read < buffer.Length)
        {
            Array.Resize(ref buffer, read);
        }
        var mono = WaveformPeaks.ToMono(buffer, channels);
        if (mono.Length > maxSamples / 4)
        {
            mono = WaveformPeaks.Downsample(mono, maxSamples / 4);
        }
        return WaveformPeaks.ComputeMinMax(mono, peakCount);
    }
}
