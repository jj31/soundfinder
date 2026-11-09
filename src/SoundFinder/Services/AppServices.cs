using System.IO;
using SoundFinder.Core.Data;
using SoundFinder.Core.Indexing;
using SoundFinder.Core.Metadata;
using SoundFinder.Core.Models;
using SoundFinder.Core.Rename;
using SoundFinder.Core.Search;
using SoundFinder.Core.Sources;

namespace SoundFinder.Services;

public static class AppServices
{
    public static Database Database { get; }
    public static LibraryRepository Libraries { get; }
    public static SampleRepository Samples { get; }
    public static SearchService Search { get; }
    public static MetadataService Metadata { get; }
    public static BulkRenameService BulkRename { get; }
    public static LibraryIndexer Indexer { get; }

    static AppServices()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SoundFinder",
            "soundfinder.db");
        Database = new Database(dbPath);
        Libraries = new LibraryRepository(Database);
        Samples = new SampleRepository(Database);
        Search = new SearchService(Database);
        Metadata = new MetadataService(Database, TagLibFileWriter.Write);
        BulkRename = new BulkRenameService(Database);
        Indexer = new LibraryIndexer(Database, new TagLibAudioReader());
    }

    public static string ResolveFullPath(SampleFile sample)
    {
        var library = Libraries.GetById(sample.LibraryId)
            ?? throw new InvalidOperationException("Library not found.");
        if (library.SourceType == SourceType.Local)
        {
            return new LocalFileSource(library.RootPath).GetFullPath(sample.Path);
        }
        var source = SampleSourceFactory.Create(library);
        var tempPath = Path.Combine(Path.GetTempPath(), "soundfinder_" + Guid.NewGuid().ToString("N") + Path.GetExtension(sample.Path));
        using (var input = source.OpenRead(sample.Path))
        using (var output = File.Create(tempPath))
        {
            input.CopyTo(output);
        }
        return tempPath;
    }
}
