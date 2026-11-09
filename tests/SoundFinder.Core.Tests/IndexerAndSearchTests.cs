using SoundFinder.Core.Data;
using SoundFinder.Core.Indexing;
using SoundFinder.Core.Models;
using SoundFinder.Core.Search;

namespace SoundFinder.Core.Tests;

public class IndexerAndSearchTests : IDisposable
{
    private readonly Database _database = Database.CreateInMemory(Guid.NewGuid().ToString("N"));
    private readonly string _root;

    public IndexerAndSearchTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "soundfinder_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        _database.Dispose();
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
        }
    }

    private long AddLibrary(string name = "TestLib")
    {
        var repo = new LibraryRepository(_database);
        return repo.Add(new Library { Name = name, RootPath = _root, SourceType = SourceType.Local });
    }

    [Fact]
    public void Indexer_IndexesWavFiles()
    {
        TestWav.Write(Path.Combine(_root, "kick.wav"), 0.25, 8000, 100.0);
        TestWav.Write(Path.Combine(_root, "snare.wav"), 0.1, 8000, 200.0);
        File.WriteAllText(Path.Combine(_root, "notes.txt"), "not audio");

        var libraryId = AddLibrary();
        var indexer = new LibraryIndexer(_database, new TagLibAudioReader());
        var report = indexer.IndexLibrary(libraryId);

        Assert.Equal(2, report.Scanned);
        Assert.Equal(2, report.Added);
        Assert.Equal(0, report.Removed);
        Assert.Equal(0, report.Errors);

        var search = new SearchService(_database);
        var result = search.Search(new SearchFilter { LibraryId = libraryId });
        Assert.Equal(2, result.Total);
        var kick = result.Items.Single(s => s.FileName == "kick.wav");
        Assert.Equal(8000, kick.SampleRate);
        Assert.InRange(kick.DurationSeconds, 0.2, 0.3);
    }

    [Fact]
    public void Indexer_RemovesDeletedFilesOnReindex()
    {
        var file = Path.Combine(_root, "hat.wav");
        TestWav.Write(file);
        var libraryId = AddLibrary();
        var indexer = new LibraryIndexer(_database, new TagLibAudioReader());
        indexer.IndexLibrary(libraryId);

        File.Delete(file);
        var report = indexer.IndexLibrary(libraryId);

        Assert.Equal(1, report.Removed);
        var result = new SearchService(_database).Search(new SearchFilter { LibraryId = libraryId });
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Search_FullTextFindsByFileName()
    {
        TestWav.Write(Path.Combine(_root, "808 kick.wav"));
        TestWav.Write(Path.Combine(_root, "vinyl crackle.wav"));
        var libraryId = AddLibrary();
        new LibraryIndexer(_database, new TagLibAudioReader()).IndexLibrary(libraryId);

        var search = new SearchService(_database);
        var result = search.Search(new SearchFilter { Query = "kick" });
        Assert.Single(result.Items);
        Assert.Equal("808 kick.wav", result.Items[0].FileName);
    }

    [Fact]
    public void Search_FiltersByDuration()
    {
        TestWav.Write(Path.Combine(_root, "short.wav"), 0.05, 8000);
        TestWav.Write(Path.Combine(_root, "long.wav"), 0.5, 8000);
        var libraryId = AddLibrary();
        new LibraryIndexer(_database, new TagLibAudioReader()).IndexLibrary(libraryId);

        var search = new SearchService(_database);
        var result = search.Search(new SearchFilter { LibraryId = libraryId, MinDurationSeconds = 0.3 });
        Assert.Single(result.Items);
        Assert.Equal("long.wav", result.Items[0].FileName);
    }
}
