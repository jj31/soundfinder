using SoundFinder.Core.Models;

namespace SoundFinder.Core.Sources;

public sealed class CloudSampleSource : ISampleSource
{
    private readonly string _providerName;

    public CloudSampleSource(string providerName) => _providerName = providerName;

    public SourceType Type => SourceType.Cloud;

    public IEnumerable<SourceFile> EnumerateFiles() =>
        throw new NotSupportedException(
            $"Cloud source '{_providerName}' is not configured yet. Plug a provider (Dropbox, Google Drive, OneDrive) by implementing ISampleSource.");

    public Stream OpenRead(string path) =>
        throw new NotSupportedException(
            $"Cloud source '{_providerName}' is not configured yet. Plug a provider (Dropbox, Google Drive, OneDrive) by implementing ISampleSource.");

    public bool FileExists(string path) => false;
}
