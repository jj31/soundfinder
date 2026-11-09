using Microsoft.Data.Sqlite;
using SoundFinder.Core.Data;

namespace SoundFinder.Core.Search;

public sealed class SearchService
{
    private readonly Database _database;

    public SearchService(Database database) => _database = database;

    public SearchResult Search(SearchFilter filter)
    {
        var where = new List<string>();
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            where.Add("s.Id IN (SELECT rowid FROM SamplesFts WHERE SamplesFts MATCH @match)");
            parameters.Add(new SqliteParameter("@match", BuildMatchQuery(filter.Query)));
        }
        if (filter.LibraryId is { } libraryId)
        {
            where.Add("s.LibraryId = @lib");
            parameters.Add(new SqliteParameter("@lib", libraryId));
        }
        if (filter.MinDurationSeconds is { } minDur)
        {
            where.Add("s.DurationSeconds >= @mindur");
            parameters.Add(new SqliteParameter("@mindur", minDur));
        }
        if (filter.MaxDurationSeconds is { } maxDur)
        {
            where.Add("s.DurationSeconds <= @maxdur");
            parameters.Add(new SqliteParameter("@maxdur", maxDur));
        }
        if (!string.IsNullOrWhiteSpace(filter.Format))
        {
            where.Add("s.Format LIKE @fmt OR s.FileName LIKE @fmtext");
            var ext = filter.Format.TrimStart('.').ToLowerInvariant();
            parameters.Add(new SqliteParameter("@fmt", $"%{ext}%"));
            parameters.Add(new SqliteParameter("@fmtext", $"%.{ext}"));
        }
        if (!string.IsNullOrWhiteSpace(filter.TagContains))
        {
            where.Add("s.Tags LIKE @tag");
            parameters.Add(new SqliteParameter("@tag", $"%{filter.TagContains}%"));
        }
        if (filter.MinBpm is { } minBpm)
        {
            where.Add("s.Bpm >= @minbpm");
            parameters.Add(new SqliteParameter("@minbpm", minBpm));
        }
        if (filter.MaxBpm is { } maxBpm)
        {
            where.Add("s.Bpm <= @maxbpm");
            parameters.Add(new SqliteParameter("@maxbpm", maxBpm));
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var result = new SearchResult();

        using var connection = _database.OpenConnection();

        using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = $"SELECT COUNT(*) FROM Samples s {whereClause}";
            foreach (var p in parameters)
            {
                countCommand.Parameters.Add(p);
            }
            result.Total = Convert.ToInt32(countCommand.ExecuteScalar());
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT s.* FROM Samples s {whereClause} ORDER BY s.FileName LIMIT @limit OFFSET @offset";
        foreach (var p in parameters)
        {
            command.Parameters.Add(p);
        }
        command.Parameters.Add(new SqliteParameter("@limit", filter.Limit));
        command.Parameters.Add(new SqliteParameter("@offset", filter.Offset));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Items.Add(SampleRepository.Map(reader));
        }
        return result;
    }

    public static string BuildMatchQuery(string query)
    {
        var tokens = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .Select(t => "\"" + t.Replace("\"", "\"\"") + "\"*");
        return string.Join(" ", tokens);
    }
}
