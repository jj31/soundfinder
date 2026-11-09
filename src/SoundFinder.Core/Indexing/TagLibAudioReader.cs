using SoundFinder.Core.Models;

namespace SoundFinder.Core.Indexing;

public sealed class TagLibAudioReader : IAudioMetadataReader
{
    public AudioInfo Read(string path)
    {
        using var file = TagLib.File.Create(path);
        var properties = file.Properties;
        var tag = file.Tag;
        var comment = tag.Comment ?? string.Empty;
        var musicalKey = tag.InitialKey ?? string.Empty;
        var bpm = tag.BeatsPerMinute > 0 ? (double?)tag.BeatsPerMinute : null;
        var tags = string.Join(";", tag.Genres.Where(g => !string.IsNullOrWhiteSpace(g)).Distinct());
        return new AudioInfo
        {
            DurationSeconds = properties.Duration.TotalSeconds,
            SampleRate = properties.AudioSampleRate,
            Channels = properties.AudioChannels,
            BitsPerSample = properties.BitsPerSample,
            Format = file.MimeType,
            Title = tag.Title ?? string.Empty,
            Tags = tags,
            Bpm = bpm,
            MusicalKey = musicalKey,
            Comment = comment
        };
    }
}
