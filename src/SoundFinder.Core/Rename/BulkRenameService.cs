using SoundFinder.Core.Data;
using SoundFinder.Core.Models;

namespace SoundFinder.Core.Rename;

public sealed class BulkRenameService
{
    private readonly Database _database;

    public BulkRenameService(Database database) => _database = database;

    public List<RenamePreviewItem> Preview(RenameRequest request)
    {
        var repository = new SampleRepository(_database);
        var samples = repository.GetByIds(request.SampleIds);
        var usedNames = new Dictionary<long, HashSet<string>>(samples.Count);
        foreach (var group in samples.GroupBy(s => s.LibraryId))
        {
            usedNames[group.Key] = new HashSet<string>(
                samples.Where(s => s.LibraryId == group.Key).Select(NormalizeName),
                StringComparer.OrdinalIgnoreCase);
        }

        var results = new List<RenamePreviewItem>();
        foreach (var sample in samples)
        {
            var newFileName = ExpandPattern(request.Pattern, sample);
            var item = new RenamePreviewItem
            {
                SampleId = sample.Id,
                OldPath = sample.Path,
                NewFileName = newFileName
            };
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                item.Error = "Pattern produced an empty file name.";
            }
            else
            {
                var directory = sample.Path.Replace('\\', '/') is var p && p.Contains('/')
                    ? p[..p.LastIndexOf('/')]
                    : string.Empty;
                var candidate = string.IsNullOrEmpty(directory) ? newFileName : directory + "/" + newFileName;
                item.NewPath = candidate;
                if (usedNames[sample.LibraryId].Contains(NormalizeName(candidate)) &&
                    !string.Equals(NormalizeName(candidate), NormalizeName(sample.Path), StringComparison.OrdinalIgnoreCase))
                {
                    item.Error = "A file with this name already exists in the library.";
                }
            }
            results.Add(item);
        }
        return results;
    }

    public List<RenamePreviewItem> Apply(RenameRequest request, Func<SampleFile, string, bool> moveFile)
    {
        var previews = Preview(request);
        var repository = new SampleRepository(_database);
        foreach (var preview in previews)
        {
            if (preview.Error is not null)
            {
                continue;
            }
            var sample = repository.GetById(preview.SampleId);
            if (sample is null)
            {
                continue;
            }
            var oldPath = sample.Path;
            if (moveFile(sample, preview.NewPath))
            {
                var newName = preview.NewPath.Replace('\\', '/').Split('/').Last();
                sample.Path = preview.NewPath;
                sample.FileName = newName;
                UpdatePath(sample);
            }
            else
            {
                preview.Error = "The file could not be moved on disk.";
            }
        }
        return previews;
    }

    public static string ExpandPattern(string pattern, SampleFile sample)
    {
        var extension = sample.Extension;
        var nameWithoutExtension = sample.FileName.Length > extension.Length
            ? sample.FileName[..^extension.Length]
            : sample.FileName;
        var replacement = pattern
            .Replace("{name}", nameWithoutExtension, StringComparison.OrdinalIgnoreCase)
            .Replace("{filename}", sample.FileName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ext}", extension, StringComparison.OrdinalIgnoreCase)
            .Replace("{tags}", Sanitize(sample.Tags.Replace(';', '-')), StringComparison.OrdinalIgnoreCase)
            .Replace("{bpm}", sample.Bpm?.ToString("0") ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{key}", Sanitize(sample.MusicalKey), StringComparison.OrdinalIgnoreCase)
            .Replace("{comment}", Sanitize(sample.Comment), StringComparison.OrdinalIgnoreCase)
            .Replace("{library}", sample.LibraryId.ToString(), StringComparison.OrdinalIgnoreCase);
        return Sanitize(replacement).Trim();
    }

    public static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static string NormalizeName(string path) => path.Replace('\\', '/').TrimEnd('/').ToLowerInvariant();

    private void UpdatePath(SampleFile sample)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Samples SET Path = @path, FileName = @name WHERE Id = @id";
        command.Parameters.AddWithValue("@id", sample.Id);
        command.Parameters.AddWithValue("@path", sample.Path);
        command.Parameters.AddWithValue("@name", sample.FileName);
        command.ExecuteNonQuery();
    }
}
