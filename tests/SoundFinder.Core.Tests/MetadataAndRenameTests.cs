using SoundFinder.Core.Data;
using SoundFinder.Core.Metadata;
using SoundFinder.Core.Models;
using SoundFinder.Core.Rename;
using SoundFinder.Core.Search;

namespace SoundFinder.Core.Tests;

public class MetadataAndRenameTests : IDisposable
{
    private readonly Database _database = Database.CreateInMemory(Guid.NewGuid().ToString("N"));

    public void Dispose() => _database.Dispose();

    private long SeedLibrary(string name = "Lib") =>
        new LibraryRepository(_database).Add(new Library
        {
            Name = name,
            RootPath = "/tmp",
            SourceType = SourceType.Local
        });

    private SampleFile Seed(string fileName, string tags = "", double? bpm = null, string key = "") =>
        Seed(SeedLibrary(), fileName, tags, bpm, key);

    private SampleFile Seed(long libraryId, string fileName, string tags = "", double? bpm = null, string key = "")
    {
        var sample = new SampleFile
        {
            LibraryId = libraryId,
            Path = "sub/" + fileName,
            FileName = fileName,
            LastWriteTimeUtc = DateTime.UtcNow,
            IndexedAtUtc = DateTime.UtcNow,
            Tags = tags,
            Bpm = bpm,
            MusicalKey = key
        };
        new SampleRepository(_database).Upsert(sample);
        return sample;
    }

    [Fact]
    public void Metadata_UpdatePersistsAllFields()
    {
        var sample = Seed("a.wav");
        var service = new MetadataService(_database);
        var updated = service.Update(sample.Id, new MetadataPatch
        {
            Tags = "drum;loop",
            Bpm = 128,
            MusicalKey = "Am",
            Comment = "punchy"
        });

        Assert.NotNull(updated);
        Assert.Equal("drum;loop", updated!.Tags);
        Assert.Equal(128, updated.Bpm);
        Assert.Equal("Am", updated.MusicalKey);
        Assert.Equal("punchy", updated.Comment);

        var reloaded = new SampleRepository(_database).GetById(sample.Id);
        Assert.Equal("drum;loop", reloaded!.Tags);
    }

    [Fact]
    public void Metadata_SearchFindsUpdatedTags()
    {
        var sample = Seed("x.wav", tags: "ambient");
        var service = new MetadataService(_database);
        service.Update(sample.Id, new MetadataPatch { Tags = "ambient pad" });

        var result = new SearchService(_database).Search(new SearchFilter { Query = "pad" });
        Assert.Single(result.Items);
    }

    [Fact]
    public void Rename_ExpandsTokensAndSanitizes()
    {
        var sample = Seed("raw name.wav", tags: "drum:hard", bpm: 90, key: "C#m");
        var name = BulkRenameService.ExpandPattern("{tags}_{bpm}bpm_{key}_{name}.{ext}", sample);
        Assert.Equal("drum_hard_90bpm_C#m_raw name.wav", name);
    }

    [Fact]
    public void Rename_PreviewDetectsCollision()
    {
        var libraryId = SeedLibrary();
        Seed(libraryId, "one.wav");
        var two = Seed(libraryId, "two.wav");
        var service = new BulkRenameService(_database);
        var preview = service.Preview(new RenameRequest
        {
            SampleIds = new List<long> { two.Id },
            Pattern = "one.{ext}"
        });
        Assert.Single(preview);
        Assert.NotNull(preview[0].Error);
    }

    [Fact]
    public void Rename_ApplyMovesFileAndUpdatesRow()
    {
        var sample = Seed("old.wav");
        var service = new BulkRenameService(_database);
        var applied = service.Apply(
            new RenameRequest { SampleIds = new List<long> { sample.Id }, Pattern = "new_{name}.{ext}" },
            (s, newPath) => true);

        Assert.Null(applied[0].Error);
        var reloaded = new SampleRepository(_database).GetById(sample.Id);
        Assert.Equal("new_old.wav", reloaded!.FileName);
        Assert.Equal("sub/new_old.wav", reloaded.Path);
    }
}
