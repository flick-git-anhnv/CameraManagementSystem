using CMS.Core.Entities;
using CMS.Core.Enums;
using Microsoft.Data.Sqlite;

namespace CMS.Infrastructure.Data;

public class SqliteAlarmRepository
{
    private readonly string _connectionString;

    public SqliteAlarmRepository(string? dbPath = null)
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
CREATE TABLE IF NOT EXISTS AlarmEvents (
    Id TEXT PRIMARY KEY,
    CameraId TEXT NOT NULL,
    CameraName TEXT,
    EventType INTEGER NOT NULL,
    Level INTEGER NOT NULL,
    Timestamp TEXT NOT NULL,
    Description TEXT,
    SnapshotPath TEXT,
    IsHandled INTEGER DEFAULT 0,
    HandledBy TEXT,
    HandledAt TEXT
);
CREATE INDEX IF NOT EXISTS IDX_Alarms_Time ON AlarmEvents (Timestamp DESC);
";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> AddAlarmAsync(AlarmEvent e)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
INSERT INTO AlarmEvents (Id, CameraId, CameraName, EventType, Level, Timestamp, Description, SnapshotPath, IsHandled, HandledBy, HandledAt)
VALUES (@Id, @CameraId, @CameraName, @EventType, @Level, @Timestamp, @Description, @SnapshotPath, @IsHandled, @HandledBy, @HandledAt);";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", e.Id);
        cmd.Parameters.AddWithValue("@CameraId", e.CameraId);
        cmd.Parameters.AddWithValue("@CameraName", (object?)e.CameraName ?? "");
        cmd.Parameters.AddWithValue("@EventType", (int)e.EventType);
        cmd.Parameters.AddWithValue("@Level", (int)e.Level);
        cmd.Parameters.AddWithValue("@Timestamp", e.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@Description", (object?)e.Description ?? "");
        cmd.Parameters.AddWithValue("@SnapshotPath", (object?)e.SnapshotPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsHandled", e.IsHandled ? 1 : 0);
        cmd.Parameters.AddWithValue("@HandledBy", (object?)e.HandledBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HandledAt", e.HandledAt.HasValue ? e.HandledAt.Value.ToString("O") : (object)DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<AlarmEvent>> GetRecentAlarmsAsync(int limit = 50)
    {
        var list = new List<AlarmEvent>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT Id, CameraId, CameraName, EventType, Level, Timestamp, Description, SnapshotPath, IsHandled, HandledBy, HandledAt FROM AlarmEvents ORDER BY Timestamp DESC LIMIT @Limit";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Limit", limit);

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new AlarmEvent
            {
                Id = r.GetString(0),
                CameraId = r.GetString(1),
                CameraName = r.IsDBNull(2) ? "" : r.GetString(2),
                EventType = (AlarmEventType)r.GetInt32(3),
                Level = (AlarmLevel)r.GetInt32(4),
                Timestamp = DateTime.Parse(r.GetString(5)),
                Description = r.IsDBNull(6) ? "" : r.GetString(6),
                SnapshotPath = r.IsDBNull(7) ? null : r.GetString(7),
                IsHandled = r.GetInt32(8) == 1,
                HandledBy = r.IsDBNull(9) ? null : r.GetString(9),
                HandledAt = r.IsDBNull(10) ? null : DateTime.Parse(r.GetString(10))
            });
        }

        return list;
    }
}
