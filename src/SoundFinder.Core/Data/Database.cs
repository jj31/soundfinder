using Microsoft.Data.Sqlite;

namespace SoundFinder.Core.Data;

public sealed class Database : IDisposable
{
    private readonly Func<SqliteConnection> _connectionFactory;
    private readonly SqliteConnection? _anchorConnection;
    private bool _disposed;

    public Database(string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        SQLitePCL.Batteries_V2.Init();
        var connectionString = $"Data Source={filePath}";
        _connectionFactory = () =>
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        };
        using var connection = _connectionFactory();
        Initialize(connection);
    }

    private Database(string sharedName, SqliteConnection anchorConnection)
    {
        SQLitePCL.Batteries_V2.Init();
        _anchorConnection = anchorConnection;
        var connectionString = $"Data Source={sharedName};Mode=Memory;Cache=Shared";
        _connectionFactory = () =>
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        };
        using var connection = _connectionFactory();
        Initialize(connection);
    }

    public static Database CreateInMemory(string name = "soundfinder_test")
    {
        SQLitePCL.Batteries_V2.Init();
        var anchor = new SqliteConnection($"Data Source={name};Mode=Memory;Cache=Shared");
        anchor.Open();
        return new Database(name, anchor);
    }

    public SqliteConnection OpenConnection() => _connectionFactory();

    private static void Initialize(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;

            CREATE TABLE IF NOT EXISTS Libraries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                RootPath TEXT NOT NULL,
                SourceType INTEGER NOT NULL DEFAULT 0,
                SourceUri TEXT NOT NULL DEFAULT '',
                UserName TEXT NOT NULL DEFAULT '',
                Enabled INTEGER NOT NULL DEFAULT 1,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Samples (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LibraryId INTEGER NOT NULL REFERENCES Libraries(Id) ON DELETE CASCADE,
                Path TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FileSize INTEGER NOT NULL DEFAULT 0,
                LastWriteTimeUtc TEXT NOT NULL,
                IndexedAtUtc TEXT NOT NULL,
                DurationSeconds REAL NOT NULL DEFAULT 0,
                SampleRate INTEGER NOT NULL DEFAULT 0,
                Channels INTEGER NOT NULL DEFAULT 0,
                BitsPerSample INTEGER NOT NULL DEFAULT 0,
                Format TEXT NOT NULL DEFAULT '',
                Tags TEXT NOT NULL DEFAULT '',
                Bpm REAL,
                MusicalKey TEXT NOT NULL DEFAULT '',
                Comment TEXT NOT NULL DEFAULT '',
                UNIQUE(LibraryId, Path)
            );

            CREATE INDEX IF NOT EXISTS IX_Samples_LibraryId ON Samples(LibraryId);
            CREATE INDEX IF NOT EXISTS IX_Samples_FileName ON Samples(FileName);

            CREATE VIRTUAL TABLE IF NOT EXISTS SamplesFts USING fts5(
                FileName,
                Tags,
                Comment,
                tokenize='unicode61'
            );

            CREATE TRIGGER IF NOT EXISTS Samples_ai AFTER INSERT ON Samples BEGIN
                INSERT INTO SamplesFts(rowid, FileName, Tags, Comment)
                VALUES (new.Id, new.FileName, new.Tags, new.Comment);
            END;

            CREATE TRIGGER IF NOT EXISTS Samples_ad AFTER DELETE ON Samples BEGIN
                DELETE FROM SamplesFts WHERE rowid = old.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS Samples_au AFTER UPDATE ON Samples BEGIN
                DELETE FROM SamplesFts WHERE rowid = old.Id;
                INSERT INTO SamplesFts(rowid, FileName, Tags, Comment)
                VALUES (new.Id, new.FileName, new.Tags, new.Comment);
            END;
            """;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _anchorConnection?.Dispose();
    }
}
