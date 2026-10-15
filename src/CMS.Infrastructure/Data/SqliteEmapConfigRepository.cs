using CMS.Core.Entities;
using CMS.Core.Interfaces;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace CMS.Infrastructure.Data;

public class SqliteEmapConfigRepository : IEmapConfigRepository
{
    private readonly string _connectionString;

    public SqliteEmapConfigRepository(string? dbPath = null)
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
CREATE TABLE IF NOT EXISTS EmapSettings (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    ImagePath TEXT,
    CameraNodesJson TEXT
);";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public async Task<EmapConfig> GetConfigAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT Id, Name, ImagePath, CameraNodesJson FROM EmapSettings LIMIT 1";
        using var cmd = new SqliteCommand(sql, conn);
        using var r = await cmd.ExecuteReaderAsync();

        if (await r.ReadAsync())
        {
            var config = new EmapConfig
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                ImagePath = r.IsDBNull(2) ? string.Empty : r.GetString(2)
            };

            if (!r.IsDBNull(3))
            {
                var json = r.GetString(3);
                try
                {
                    var nodes = JsonSerializer.Deserialize<List<CameraMapNode>>(json);
                    if (nodes != null)
                    {
                        config.CameraNodes = nodes;
                    }
                }
                catch
                {
                    // Ignore json parsing issues
                }
            }

            return config;
        }

        return new EmapConfig();
    }

    public async Task<bool> SaveConfigAsync(EmapConfig config)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
INSERT INTO EmapSettings (Id, Name, ImagePath, CameraNodesJson)
VALUES (@Id, @Name, @ImagePath, @CameraNodesJson)
ON CONFLICT(Id) DO UPDATE SET
    Name = excluded.Name,
    ImagePath = excluded.ImagePath,
    CameraNodesJson = excluded.CameraNodesJson;";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", string.IsNullOrEmpty(config.Id) ? "default_emap" : config.Id);
        cmd.Parameters.AddWithValue("@Name", config.Name ?? "Floor Plan 1");
        cmd.Parameters.AddWithValue("@ImagePath", config.ImagePath ?? string.Empty);

        string nodesJson = JsonSerializer.Serialize(config.CameraNodes ?? new List<CameraMapNode>());
        cmd.Parameters.AddWithValue("@CameraNodesJson", nodesJson);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
