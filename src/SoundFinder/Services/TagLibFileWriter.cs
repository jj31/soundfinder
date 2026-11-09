using SoundFinder.Core.Metadata;
using SoundFinder.Core.Models;
using SoundFinder.Core.Sources;

namespace SoundFinder.Services;

public static class TagLibFileWriter
{
    public static void Write(SampleFile sample, MetadataPatch patch)
    {
        var library = AppServices.Libraries.GetById(sample.LibraryId);
        if (library is null || library.SourceType != SourceType.Local)
        {
            return;
        }
        var fullPath = new LocalFileSource(library.RootPath).GetFullPath(sample.Path);
        if (!File.Exists(fullPath))
        {
            return;
        }
        using var file = TagLib.File.Create(fullPath);
        if (patch.Tags is not null)
        {
            file.Tag.Genres = patch.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        if (patch.ClearBpm)
        {
            file.Tag.BeatsPerMinute = 0;
        }
        else if (patch.Bpm is { } bpm)
        {
            file.Tag.BeatsPerMinute = (uint)Math.Round(bpm);
        }
        if (patch.MusicalKey is not null)
        {
            file.Tag.InitialKey = patch.MusicalKey;
        }
        if (patch.Comment is not null)
        {
            file.Tag.Comment = patch.Comment;
        }
        file.Save();
    }
}
