using SoundFinder.Core.Models;

namespace SoundFinder.Core.Sources;

public sealed class SourceFile
{
    public string Path { get; set; } = string.Empty;
    public long Length { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
}

public interface ISampleSource
{
    SourceType Type { get; }
    IEnumerable<SourceFile> EnumerateFiles();
    Stream OpenRead(string path);
    bool FileExists(string path);
}
