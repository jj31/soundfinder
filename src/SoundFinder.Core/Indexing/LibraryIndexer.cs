using SoundFinder.Core.Data;
using SoundFinder.Core.Models;
using SoundFinder.Core.Sources;

namespace SoundFinder.Core.Indexing;

public sealed class LibraryIndexer
{
    private readonly Database _database;
    private readonly IAudioMetadataReader _metadataReader;

    public LibraryIndexer(Database database, IAudioMetadataReader metadataReader)
    {
        _database = database;
        _metadataReader = metadataReader;
    }

    public IndexReport IndexLibrary(long libraryId, CancellationToken cancellationToken = default)
    {
        var report = new IndexReport { LibraryId = libraryId };
        var libraryRepository = new LibraryRepository(_database);
        var library = libraryRepository.GetById(libraryId)
            ?? throw new InvalidOperationException($"Library {libraryId} not found.");
        var sampleRepository = new SampleRepository(_database);
        var source = SampleSourceFactory.Create(library);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in source.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            seenPaths.Add(file.Path);
            report.Scanned++;

            AudioInfo info;
            string? tempFile = null;
            try
            {
                var fullPath = source is LocalFileSource local
                    ? local.GetFullPath(file.Path)
                    : tempFile = DownloadToTemp(source, file.Path);
                info = _metadataReader.Read(fullPath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                report.Errors++;
                report.ErrorMessages.Add($"{file.Path}: {ex.Message}");
                continue;
            }
            finally
            {
                if (tempFile is not null)
                {
                    TryDelete(tempFile);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                report.Errors++;
                report.ErrorMessages.Add($"{file.Path}: {ex.Message}");
                continue;
            }

            var sample = new SampleFile
            {
                LibraryId = libraryId,
                Path = file.Path,
                FileName = file.Path.Replace('\\', '/').Split('/').Last(),
                FileSize = file.Length,
                LastWriteTimeUtc = file.LastWriteTimeUtc,
                IndexedAtUtc = DateTime.UtcNow,
                DurationSeconds = info.DurationSeconds,
                SampleRate = info.SampleRate,
                Channels = info.Channels,
                BitsPerSample = info.BitsPerSample,
                Format = info.Format,
                Tags = info.Tags,
                Bpm = info.Bpm,
                MusicalKey = info.MusicalKey,
                Comment = info.Comment
            };
            var existing = GetByPath(libraryId, file.Path);
            sampleRepository.Upsert(sample);
            if (existing is null)
            {
                report.Added++;
            }
            else if (existing.LastWriteTimeUtc != file.LastWriteTimeUtc || existing.FileSize != file.Length)
            {
                report.Updated++;
            }
        }

        report.Removed = sampleRepository.DeleteMissing(libraryId, seenPaths);
        return report;
    }

    private SampleFile? GetByPath(long libraryId, string path)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Samples WHERE LibraryId = @lib AND Path = @path";
        command.Parameters.AddWithValue("@lib", libraryId);
        command.Parameters.AddWithValue("@path", path);
        using var reader = command.ExecuteReader();
        return reader.Read() ? SampleRepository.Map(reader) : null;
    }

    private static string DownloadToTemp(ISampleSource source, string path)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "soundfinder_" + Guid.NewGuid().ToString("N") + Path.GetExtension(path));
        using (var input = source.OpenRead(path))
        using (var output = File.Create(tempPath))
        {
            input.CopyTo(output);
        }
        return tempPath;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
