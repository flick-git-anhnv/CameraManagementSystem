using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace CMS.Infrastructure.Data;

public class SqliteVideoSegmentRepository : IVideoSegmentRepository
{
    private readonly string _connectionString;

    public SqliteVideoSegmentRepository(string? dbPath = null)
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
CREATE TABLE IF NOT EXISTS VideoSegments (
    Id TEXT PRIMARY KEY,
    CameraId TEXT NOT NULL,
    CameraName TEXT,
    StartTime TEXT NOT NULL,
    EndTime TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileSizeBytes INTEGER,
    RecordingType INTEGER,
    StreamType INTEGER,
    IsLocked INTEGER DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IDX_VideoSegments_Cam_Time ON VideoSegments (CameraId, StartTime, EndTime);
";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> AddSegmentAsync(VideoSegment s)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
INSERT INTO VideoSegments (
    Id, CameraId, CameraName, StartTime, EndTime, FilePath,
    FileSizeBytes, RecordingType, StreamType, IsLocked
) VALUES (
    @Id, @CameraId, @CameraName, @StartTime, @EndTime, @FilePath,
    @FileSizeBytes, @RecordingType, @StreamType, @IsLocked
);";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", s.Id);
        cmd.Parameters.AddWithValue("@CameraId", s.CameraId);
        cmd.Parameters.AddWithValue("@CameraName", (object?)s.CameraName ?? "");
        cmd.Parameters.AddWithValue("@StartTime", s.StartTime.ToString("O"));
        cmd.Parameters.AddWithValue("@EndTime", s.EndTime.ToString("O"));
        cmd.Parameters.AddWithValue("@FilePath", s.FilePath);
        cmd.Parameters.AddWithValue("@FileSizeBytes", s.FileSizeBytes);
        cmd.Parameters.AddWithValue("@RecordingType", (int)s.RecordingType);
        cmd.Parameters.AddWithValue("@StreamType", (int)s.StreamType);
        cmd.Parameters.AddWithValue("@IsLocked", s.IsLocked ? 1 : 0);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<VideoSegment>> GetSegmentsAsync(string cameraId, DateTime startTime, DateTime endTime)
    {
        var list = new List<VideoSegment>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
SELECT Id, CameraId, CameraName, StartTime, EndTime, FilePath, FileSizeBytes, RecordingType, StreamType, IsLocked
FROM VideoSegments
WHERE CameraId = @CameraId AND EndTime >= @StartTime AND StartTime <= @EndTime
ORDER BY StartTime ASC;";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CameraId", cameraId);
        cmd.Parameters.AddWithValue("@StartTime", startTime.ToString("O"));
        cmd.Parameters.AddWithValue("@EndTime", endTime.ToString("O"));

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new VideoSegment
            {
                Id = r.GetString(0),
                CameraId = r.GetString(1),
                CameraName = r.IsDBNull(2) ? "" : r.GetString(2),
                StartTime = DateTime.Parse(r.GetString(3)),
                EndTime = DateTime.Parse(r.GetString(4)),
                FilePath = r.GetString(5),
                FileSizeBytes = r.GetInt64(6),
                RecordingType = (RecordingMode)r.GetInt32(7),
                StreamType = (StreamType)r.GetInt32(8),
                IsLocked = r.GetInt32(9) == 1
            });
        }

        return list;
    }

    public async Task<List<VideoSegment>> GetOldestUnlockedSegmentsAsync(int count)
    {
        var list = new List<VideoSegment>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
SELECT Id, CameraId, CameraName, StartTime, EndTime, FilePath, FileSizeBytes, RecordingType, StreamType, IsLocked
FROM VideoSegments
WHERE IsLocked = 0
ORDER BY StartTime ASC
LIMIT @Count;";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Count", count);

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new VideoSegment
            {
                Id = r.GetString(0),
                CameraId = r.GetString(1),
                CameraName = r.IsDBNull(2) ? "" : r.GetString(2),
                StartTime = DateTime.Parse(r.GetString(3)),
                EndTime = DateTime.Parse(r.GetString(4)),
                FilePath = r.GetString(5),
                FileSizeBytes = r.GetInt64(6),
                RecordingType = (RecordingMode)r.GetInt32(7),
                StreamType = (StreamType)r.GetInt32(8),
                IsLocked = r.GetInt32(9) == 1
            });
        }

        return list;
    }

    public async Task<bool> DeleteSegmentAsync(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "DELETE FROM VideoSegments WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<long> GetTotalStorageUsedBytesAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT COALESCE(SUM(FileSizeBytes), 0) FROM VideoSegments";
        using var cmd = new SqliteCommand(sql, conn);
        var val = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(val);
    }
}
