using Microsoft.Data.Sqlite;
using SoundFinder.Core.Models;

namespace SoundFinder.Core.Data;

public sealed class LibraryRepository
{
    private readonly Database _database;

    public LibraryRepository(Database database) => _database = database;

    public List<Library> GetAll(bool enabledOnly = false)
    {
        var libraries = new List<Library>();
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = enabledOnly
            ? "SELECT * FROM Libraries WHERE Enabled = 1 ORDER BY Name"
            : "SELECT * FROM Libraries ORDER BY Name";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            libraries.Add(Map(reader));
        }
        return libraries;
    }

    public Library? GetById(long id)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Libraries WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public long Add(Library library)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Libraries (Name, RootPath, SourceType, SourceUri, UserName, Enabled, CreatedAtUtc)
            VALUES (@name, @root, @type, @uri, @user, @enabled, @created);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@name", library.Name);
        command.Parameters.AddWithValue("@root", library.RootPath);
        command.Parameters.AddWithValue("@type", (int)library.SourceType);
        command.Parameters.AddWithValue("@uri", library.SourceUri);
        command.Parameters.AddWithValue("@user", library.UserName);
        command.Parameters.AddWithValue("@enabled", library.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        library.Id = (long)(command.ExecuteScalar() ?? 0L);
        return library.Id;
    }

    public void Update(Library library)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Libraries
            SET Name = @name, RootPath = @root, SourceType = @type, SourceUri = @uri,
                UserName = @user, Enabled = @enabled
            WHERE Id = @id
            """;
        command.Parameters.AddWithValue("@id", library.Id);
        command.Parameters.AddWithValue("@name", library.Name);
        command.Parameters.AddWithValue("@root", library.RootPath);
        command.Parameters.AddWithValue("@type", (int)library.SourceType);
        command.Parameters.AddWithValue("@uri", library.SourceUri);
        command.Parameters.AddWithValue("@user", library.UserName);
        command.Parameters.AddWithValue("@enabled", library.Enabled ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Libraries WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    internal static Library Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Name = reader.GetString(1),
        RootPath = reader.GetString(2),
        SourceType = (SourceType)reader.GetInt32(3),
        SourceUri = reader.GetString(4),
        UserName = reader.GetString(5),
        Enabled = reader.GetInt32(6) == 1,
        CreatedAtUtc = DateTime.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind)
    };
}
