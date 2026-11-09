namespace SoundFinder.Core.Models;

public sealed class AudioInfo
{
    public double DurationSeconds { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public double? Bpm { get; set; }
    public string MusicalKey { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
