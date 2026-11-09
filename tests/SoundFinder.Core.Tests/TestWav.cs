namespace SoundFinder.Core.Tests;

public static class TestWav
{
    public static void Write(string path, double seconds = 0.1, int sampleRate = 8000, double frequency = 440.0)
    {
        var sampleCount = (int)(seconds * sampleRate);
        var dataSize = sampleCount * 2;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data".ToCharArray());
        writer.Write(dataSize);
        for (var i = 0; i < sampleCount; i++)
        {
            var value = (short)(short.MaxValue * 0.5 * Math.Sin(2.0 * Math.PI * frequency * i / sampleRate));
            writer.Write(value);
        }
    }
}
