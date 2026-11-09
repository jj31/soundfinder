using SoundFinder.Core.Data;
using SoundFinder.Core.Models;

namespace SoundFinder.Core.Metadata;

public sealed class MetadataService
{
    private readonly Database _database;
    private readonly Action<SampleFile, MetadataPatch>? _fileTagWriter;

    public MetadataService(Database database, Action<SampleFile, MetadataPatch>? fileTagWriter = null)
    {
        _database = database;
        _fileTagWriter = fileTagWriter;
    }

    public SampleFile? Update(long sampleId, MetadataPatch patch, bool writeToFile = false)
    {
        var repository = new SampleRepository(_database);
        var sample = repository.GetById(sampleId) ?? return null;

        if (patch.Tags is not null)
        {
            sample.Tags = patch.Tags;
        }
        if (patch.ClearBpm)
        {
            sample.Bpm = null;
        }
        else if (patch.Bpm is { } bpm)
        {
            sample.Bpm = bpm;
        }
        if (patch.MusicalKey is not null)
        {
            sample.MusicalKey = patch.MusicalKey;
        }
        if (patch.Comment is not null)
        {
            sample.Comment = patch.Comment;
        }
        if (!string.IsNullOrWhiteSpace(patch.NewFileName))
        {
            sample.FileName = patch.NewFileName;
        }

        UpdateRow(sample);

        if (writeToFile)
        {
            _fileTagWriter?.Invoke(sample, patch);
        }
        return sample;
    }

    private void UpdateRow(SampleFile sample)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Samples
            SET FileName = @name, Tags = @tags, Bpm = @bpm, MusicalKey = @key, Comment = @comment
            WHERE Id = @id
            """;
        command.Parameters.AddWithValue("@id", sample.Id);
        command.Parameters.AddWithValue("@name", sample.FileName);
        command.Parameters.AddWithValue("@tags", sample.Tags);
        command.Parameters.AddWithValue("@bpm", (object?)sample.Bpm ?? DBNull.Value);
        command.Parameters.AddWithValue("@key", sample.MusicalKey);
        command.Parameters.AddWithValue("@comment", sample.Comment);
        command.ExecuteNonQuery();
    }
}
