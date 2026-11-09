namespace SoundFinder.Core.Metadata;

public sealed class MetadataPatch
{
    public string? Tags { get; set; }
    public double? Bpm { get; set; }
    public bool ClearBpm { get; set; }
    public string? MusicalKey { get; set; }
    public string? Comment { get; set; }
    public string? NewFileName { get; set; }
}
