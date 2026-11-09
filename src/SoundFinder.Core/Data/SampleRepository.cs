using Microsoft.Data.Sqlite;
using SoundFinder.Core.Models;

namespace SoundFinder.Core.Data;

public sealed class SampleRepository
{
    private readonly Database _database;

    public SampleRepository(Database database) => _database = database;

    public SampleFile? GetById(long id)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Samples WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public List<SampleFile> GetByIds(IEnumerable<long> ids)
    {
        var idList = ids.Distinct().ToList();
        var result = new List<SampleFile>();
        if (idList.Count == 0)
        {
            return result;
        }
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM Samples WHERE Id IN ({string.Join(",", idList.Select((_, i) => $"@id{i}"))})";
        for (var i = 0; i < idList.Count; i++)
        {
            command.Parameters.AddWithValue($"@id{i}", idList[i]);
        }
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(Map(reader));
        }
        return result;
    }

    public List<string> GetPathsByLibrary(long libraryId)
    {
        var paths = new List<string>();
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Path FROM Samples WHERE LibraryId = @lib";
        command.Parameters.AddWithValue("@lib", libraryId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            paths.Add(reader.GetString(0));
        }
        return paths;
    }

    public long Upsert(SampleFile sample)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Samples (LibraryId, Path, FileName, FileSize, LastWriteTimeUtc, IndexedAtUtc,
                DurationSeconds, SampleRate, Channels, BitsPerSample, Format, Tags, Bpm, MusicalKey, Comment)
            VALUES (@lib, @path, @name, @size, @modified, @indexed, @dur, @rate, @ch, @bits, @fmt, @tags, @bpm, @key, @comment)
            ON CONFLICT(LibraryId, Path) DO UPDATE SET
                FileName = excluded.FileName,
                FileSize = excluded.FileSize,
                LastWriteTimeUtc = excluded.LastWriteTimeUtc,
                IndexedAtUtc = excluded.IndexedAtUtc,
                DurationSeconds = excluded.DurationSeconds,
                SampleRate = excluded.SampleRate,
                Channels = excluded.Channels,
                BitsPerSample = excluded.BitsPerSample,
                Format = excluded.Format,
                Tags = excluded.Tags,
                Bpm = excluded.Bpm,
                MusicalKey = excluded.MusicalKey,
                Comment = excluded.Comment;
            SELECT Id FROM Samples WHERE LibraryId = @lib AND Path = @path;
            """;
        AddParameters(command, sample);
        sample.Id = (long)(command.ExecuteScalar() ?? 0L);
        return sample.Id;
    }

    public int DeleteMissing(long libraryId, ISet<string> existingPaths)
    {
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = "SELECT Id, Path FROM Samples WHERE LibraryId = @lib";
        select.Parameters.AddWithValue("@lib", libraryId);
        var toDelete = new List<long>();
        using (var reader = select.ExecuteReader())
        {
            while (reader.Read())
            {
                if (!existingPaths.Contains(reader.GetString(1)))
                {
                    toDelete.Add(reader.GetInt64(0));
                }
            }
        }
        if (toDelete.Count > 0)
        {
            using var delete = connection.CreateCommand();
            delete.Transaction = transaction;
            delete.CommandText = $"DELETE FROM Samples WHERE Id IN ({string.Join(",", toDelete.Select((_, i) => $"@d{i}"))})";
            for (var i = 0; i < toDelete.Count; i++)
            {
                delete.Parameters.AddWithValue($"@d{i}", toDelete[i]);
            }
            delete.ExecuteNonQuery();
        }
        transaction.Commit();
        return toDelete.Count;
    }

    private static void AddParameters(SqliteCommand command, SampleFile sample)
    {
        command.Parameters.AddWithValue("@lib", sample.LibraryId);
        command.Parameters.AddWithValue("@path", sample.Path);
        command.Parameters.AddWithValue("@name", sample.FileName);
        command.Parameters.AddWithValue("@size", sample.FileSize);
        command.Parameters.AddWithValue("@modified", sample.LastWriteTimeUtc.ToString("o"));
        command.Parameters.AddWithValue("@indexed", sample.IndexedAtUtc.ToString("o"));
        command.Parameters.AddWithValue("@dur", sample.DurationSeconds);
        command.Parameters.AddWithValue("@rate", sample.SampleRate);
        command.Parameters.AddWithValue("@ch", sample.Channels);
        command.Parameters.AddWithValue("@bits", sample.BitsPerSample);
        command.Parameters.AddWithValue("@fmt", sample.Format);
        command.Parameters.AddWithValue("@tags", sample.Tags);
        command.Parameters.AddWithValue("@bpm", (object?)sample.Bpm ?? DBNull.Value);
        command.Parameters.AddWithValue("@key", sample.MusicalKey);
        command.Parameters.AddWithValue("@comment", sample.Comment);
    }

    internal static SampleFile Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        LibraryId = reader.GetInt64(1),
        Path = reader.GetString(2),
        FileName = reader.GetString(3),
        FileSize = reader.GetInt64(4),
        LastWriteTimeUtc = DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
        IndexedAtUtc = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
        DurationSeconds = reader.GetDouble(7),
        SampleRate = reader.GetInt32(8),
        Channels = reader.GetInt32(9),
        BitsPerSample = reader.GetInt32(10),
        Format = reader.GetString(11),
        Tags = reader.GetString(12),
        Bpm = reader.IsDBNull(13) ? null : reader.GetDouble(13),
        MusicalKey = reader.GetString(14),
        Comment = reader.GetString(15)
    };
}
