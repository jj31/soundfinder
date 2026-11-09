namespace SoundFinder.Core.Indexing;

public sealed class IndexReport
{
    public long LibraryId { get; set; }
    public int Scanned { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Removed { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; } = new();
}
