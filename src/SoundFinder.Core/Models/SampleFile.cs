namespace SoundFinder.Core.Models;

public sealed class SampleFile
{
    public long Id { get; set; }
    public long LibraryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public DateTime IndexedAtUtc { get; set; }
    public double DurationSeconds { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public double? Bpm { get; set; }
    public string MusicalKey { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    public string Extension =>
        Path.Length > 0 && Path.Contains('.') ? Path[(Path.LastIndexOf('.') + 1)..].ToLowerInvariant() : string.Empty;
}
