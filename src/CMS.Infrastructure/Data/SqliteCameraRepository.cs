using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace CMS.Infrastructure.Data;

public class SqliteCameraRepository : ICameraRepository
{
    private readonly string _connectionString;

    public SqliteCameraRepository(string? dbPath = null)
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
CREATE TABLE IF NOT EXISTS Cameras (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Code TEXT,
    GroupName TEXT,
    IpAddress TEXT NOT NULL,
    HttpPort INTEGER,
    RtspPort INTEGER,
    OnvifPort INTEGER,
    Username TEXT,
    Password TEXT,
    Channel INTEGER,
    Brand INTEGER,
    CustomMainRtspUrl TEXT,
    CustomSubRtspUrl TEXT,
    HasPtz INTEGER,
    HasAudio INTEGER,
    EnabledAiFeatures INTEGER,
    RecordMode INTEGER,
    SegmentMinutes INTEGER,
    RecordSubStream INTEGER,
    Status INTEGER,
    LastOnlineTime TEXT,
    LastError TEXT
);";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public async Task<List<CameraDevice>> GetAllAsync()
    {
        var list = new List<CameraDevice>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT * FROM Cameras ORDER BY Name ASC";
        using var cmd = new SqliteCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(ReadCamera(reader));
        }

        return list;
    }

    public async Task<CameraDevice?> GetByIdAsync(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT * FROM Cameras WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadCamera(reader);
        }

        return null;
    }

    public async Task<bool> AddAsync(CameraDevice cam)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
INSERT OR REPLACE INTO Cameras (
    Id, Name, Code, GroupName, IpAddress, HttpPort, RtspPort, OnvifPort,
    Username, Password, Channel, Brand, CustomMainRtspUrl, CustomSubRtspUrl,
    HasPtz, HasAudio, EnabledAiFeatures, RecordMode, SegmentMinutes, RecordSubStream,
    Status, LastOnlineTime, LastError
) VALUES (
    @Id, @Name, @Code, @GroupName, @IpAddress, @HttpPort, @RtspPort, @OnvifPort,
    @Username, @Password, @Channel, @Brand, @CustomMainRtspUrl, @CustomSubRtspUrl,
    @HasPtz, @HasAudio, @EnabledAiFeatures, @RecordMode, @SegmentMinutes, @RecordSubStream,
    @Status, @LastOnlineTime, @LastError
);";

        using var cmd = new SqliteCommand(sql, conn);
        AddParameters(cmd, cam);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateAsync(CameraDevice cam) => await AddAsync(cam);

    public async Task<bool> DeleteAsync(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "DELETE FROM Cameras WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static CameraDevice ReadCamera(SqliteDataReader r)
    {
        return new CameraDevice
        {
            Id = r.GetString(0),
            Name = r.GetString(1),
            Code = r.IsDBNull(2) ? "" : r.GetString(2),
            GroupName = r.IsDBNull(3) ? "Default" : r.GetString(3),
            IpAddress = r.GetString(4),
            HttpPort = r.GetInt32(5),
            RtspPort = r.GetInt32(6),
            OnvifPort = r.GetInt32(7),
            Username = r.IsDBNull(8) ? "" : r.GetString(8),
            Password = r.IsDBNull(9) ? "" : r.GetString(9),
            Channel = r.GetInt32(10),
            Brand = (CameraBrand)r.GetInt32(11),
            CustomMainRtspUrl = r.IsDBNull(12) ? "" : r.GetString(12),
            CustomSubRtspUrl = r.IsDBNull(13) ? "" : r.GetString(13),
            HasPtz = r.GetInt32(14) == 1,
            HasAudio = r.GetInt32(15) == 1,
            EnabledAiFeatures = (AiCapability)r.GetInt32(16),
            RecordMode = (RecordingMode)r.GetInt32(17),
            SegmentMinutes = r.GetInt32(18),
            RecordSubStream = r.GetInt32(19) == 1,
            Status = (DeviceStatus)r.GetInt32(20),
            LastOnlineTime = r.IsDBNull(21) ? null : DateTime.Parse(r.GetString(21)),
            LastError = r.IsDBNull(22) ? "" : r.GetString(22)
        };
    }

    private static void AddParameters(SqliteCommand cmd, CameraDevice cam)
    {
        cmd.Parameters.AddWithValue("@Id", cam.Id);
        cmd.Parameters.AddWithValue("@Name", cam.Name);
        cmd.Parameters.AddWithValue("@Code", (object?)cam.Code ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GroupName", (object?)cam.GroupName ?? "Default");
        cmd.Parameters.AddWithValue("@IpAddress", cam.IpAddress);
        cmd.Parameters.AddWithValue("@HttpPort", cam.HttpPort);
        cmd.Parameters.AddWithValue("@RtspPort", cam.RtspPort);
        cmd.Parameters.AddWithValue("@OnvifPort", cam.OnvifPort);
        cmd.Parameters.AddWithValue("@Username", (object?)cam.Username ?? "");
        cmd.Parameters.AddWithValue("@Password", (object?)cam.Password ?? "");
        cmd.Parameters.AddWithValue("@Channel", cam.Channel);
        cmd.Parameters.AddWithValue("@Brand", (int)cam.Brand);
        cmd.Parameters.AddWithValue("@CustomMainRtspUrl", (object?)cam.CustomMainRtspUrl ?? "");
        cmd.Parameters.AddWithValue("@CustomSubRtspUrl", (object?)cam.CustomSubRtspUrl ?? "");
        cmd.Parameters.AddWithValue("@HasPtz", cam.HasPtz ? 1 : 0);
        cmd.Parameters.AddWithValue("@HasAudio", cam.HasAudio ? 1 : 0);
        cmd.Parameters.AddWithValue("@EnabledAiFeatures", (int)cam.EnabledAiFeatures);
        cmd.Parameters.AddWithValue("@RecordMode", (int)cam.RecordMode);
        cmd.Parameters.AddWithValue("@SegmentMinutes", cam.SegmentMinutes);
        cmd.Parameters.AddWithValue("@RecordSubStream", cam.RecordSubStream ? 1 : 0);
        cmd.Parameters.AddWithValue("@Status", (int)cam.Status);
        cmd.Parameters.AddWithValue("@LastOnlineTime", cam.LastOnlineTime.HasValue ? cam.LastOnlineTime.Value.ToString("O") : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@LastError", (object?)cam.LastError ?? "");
    }
}
