using CMS.Core.Entities;
using Microsoft.Data.Sqlite;

namespace CMS.Infrastructure.Data;

public class SqliteStorageConfigRepository
{
    private readonly string _connectionString;

    public SqliteStorageConfigRepository(string? dbPath = null)
    {
        dbPath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cms_data.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        string sql = @"
CREATE TABLE IF NOT EXISTS StorageSettings (
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    RootStoragePath TEXT NOT NULL,
    SegmentDurationMinutes INTEGER NOT NULL,
    MinFreeSpaceGigabytes INTEGER NOT NULL,
    RetentionDays INTEGER NOT NULL,
    AutoOverwriteOldest INTEGER NOT NULL
);";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public async Task<StorageConfig> GetConfigAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT RootStoragePath, SegmentDurationMinutes, MinFreeSpaceGigabytes, RetentionDays, AutoOverwriteOldest FROM StorageSettings WHERE Id = 1";
        using var cmd = new SqliteCommand(sql, conn);
        using var r = await cmd.ExecuteReaderAsync();

        if (await r.ReadAsync())
        {
            return new StorageConfig
            {
                RootStoragePath = r.GetString(0),
                SegmentDurationMinutes = r.GetInt32(1),
                MinFreeSpaceGigabytes = r.GetInt32(2),
                RetentionDays = r.GetInt32(3),
                AutoOverwriteOldest = r.GetInt32(4) == 1
            };
        }

        // Mặc định nếu chưa có
        var defaultConfig = new StorageConfig();
        await SaveConfigAsync(defaultConfig);
        return defaultConfig;
    }

    public async Task<bool> SaveConfigAsync(StorageConfig cfg)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
INSERT INTO StorageSettings (Id, RootStoragePath, SegmentDurationMinutes, MinFreeSpaceGigabytes, RetentionDays, AutoOverwriteOldest)
VALUES (1, @Path, @Duration, @MinFree, @Retention, @AutoOverwrite)
ON CONFLICT(Id) DO UPDATE SET
    RootStoragePath = excluded.RootStoragePath,
    SegmentDurationMinutes = excluded.SegmentDurationMinutes,
    MinFreeSpaceGigabytes = excluded.MinFreeSpaceGigabytes,
    RetentionDays = excluded.RetentionDays,
    AutoOverwriteOldest = excluded.AutoOverwriteOldest;";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Path", cfg.RootStoragePath);
        cmd.Parameters.AddWithValue("@Duration", cfg.SegmentDurationMinutes);
        cmd.Parameters.AddWithValue("@MinFree", cfg.MinFreeSpaceGigabytes);
        cmd.Parameters.AddWithValue("@Retention", cfg.RetentionDays);
        cmd.Parameters.AddWithValue("@AutoOverwrite", cfg.AutoOverwriteOldest ? 1 : 0);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
