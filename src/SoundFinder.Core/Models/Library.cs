namespace SoundFinder.Core.Models;

public enum SourceType
{
    Local = 0,
    Ftp = 1,
    Cloud = 2
}

public sealed class Library
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public SourceType SourceType { get; set; } = SourceType.Local;
    public string SourceUri { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
