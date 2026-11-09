using SoundFinder.Core.Models;

namespace SoundFinder.Core.Indexing;

public interface IAudioMetadataReader
{
    AudioInfo Read(string path);
}
