using SoundFinder.Core.Models;

namespace SoundFinder.Core.Sources;

public static class SampleSourceFactory
{
    public static ISampleSource Create(Library library) => library.SourceType switch
    {
        SourceType.Local => new LocalFileSource(library.RootPath),
        SourceType.Ftp => new FtpSampleSource(library.SourceUri, library.UserName, Environment.GetEnvironmentVariable("SOUNDFINDER_FTP_PASSWORD") ?? string.Empty),
        SourceType.Cloud => new CloudSampleSource(library.SourceUri),
        _ => throw new NotSupportedException($"Source type {library.SourceType} is not supported.")
    };
}
