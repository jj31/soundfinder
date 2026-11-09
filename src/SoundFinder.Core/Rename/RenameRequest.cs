namespace SoundFinder.Core.Rename;

public sealed class RenameRequest
{
    public List<long> SampleIds { get; set; } = new();
    public string Pattern { get; set; } = "{name}.{ext}";
    public bool OnlyPreview { get; set; } = true;
}

public sealed class RenamePreviewItem
{
    public long SampleId { get; set; }
    public string OldPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
    public string NewFileName { get; set; } = string.Empty;
    public string? Error { get; set; }
}
