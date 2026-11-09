using SoundFinder.Core.Models;

namespace SoundFinder.Core.Sources;

public sealed class LocalFileSource : ISampleSource
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".aif", ".aiff", ".flac", ".mp3", ".ogg", ".opus", ".wma", ".m4a"
    };

    private readonly string _rootPath;

    public LocalFileSource(string rootPath) => _rootPath = rootPath;

    public SourceType Type => SourceType.Local;

    public static bool IsAudioFile(string path) => AudioExtensions.Contains(Path.GetExtension(path));

    public IEnumerable<SourceFile> EnumerateFiles()
    {
        if (!Directory.Exists(_rootPath))
        {
            yield break;
        }
        foreach (var file in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
        {
            if (!IsAudioFile(file))
            {
                continue;
            }
            var info = new FileInfo(file);
            yield return new SourceFile
            {
                Path = Path.GetRelativePath(_rootPath, file),
                Length = info.Length,
                LastWriteTimeUtc = info.LastWriteTimeUtc
            };
        }
    }

    public string GetFullPath(string relativePath) => Path.GetFullPath(Path.Combine(_rootPath, relativePath));

    public Stream OpenRead(string path) => File.OpenRead(GetFullPath(path));

    public bool FileExists(string path) => File.Exists(GetFullPath(path));
}
