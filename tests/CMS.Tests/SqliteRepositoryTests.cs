using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Infrastructure.Data;
using Xunit;

namespace CMS.Tests;

public class SqliteRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;

    public SqliteRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"cms_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. SqliteCameraRepository CRUD Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CameraRepo_AddAndGetById_RoundtripSuccessful()
    {
        var repo = new SqliteCameraRepository(_tempDbPath);
        var cam = new CameraDevice
        {
            Id = "cam-01",
            Name = "Gate Camera 1",
            Code = "CAM01",
            GroupName = "Main Gate",
            IpAddress = "192.168.1.150",
            HttpPort = 80,
            RtspPort = 554,
            OnvifPort = 8000,
            Username = "admin",
            Password = "pass123@#$",
            Channel = 1,
            Brand = CameraBrand.Hikvision,
            CustomMainRtspUrl = "rtsp://main/stream",
            CustomSubRtspUrl = "rtsp://sub/stream",
            HasPtz = true,
            HasAudio = true,
            EnabledAiFeatures = AiCapability.PlateRecognition | AiCapability.VehicleClassification,
            RecordMode = RecordingMode.Continuous247,
            SegmentMinutes = 30,
            RecordSubStream = false,
            Status = DeviceStatus.Online,
            LastOnlineTime = DateTime.UtcNow,
            LastError = ""
        };

        bool added = await repo.AddAsync(cam);
        Assert.True(added);

        var retrieved = await repo.GetByIdAsync("cam-01");
        Assert.NotNull(retrieved);
        Assert.Equal("cam-01", retrieved.Id);
        Assert.Equal("Gate Camera 1", retrieved.Name);
        Assert.Equal("CAM01", retrieved.Code);
        Assert.Equal("Main Gate", retrieved.GroupName);
        Assert.Equal("192.168.1.150", retrieved.IpAddress);
        Assert.Equal(CameraBrand.Hikvision, retrieved.Brand);
        Assert.True(retrieved.HasPtz);
        Assert.True(retrieved.HasAudio);
        Assert.Equal(AiCapability.PlateRecognition | AiCapability.VehicleClassification, retrieved.EnabledAiFeatures);
        Assert.Equal(RecordingMode.Continuous247, retrieved.RecordMode);
        Assert.Equal(30, retrieved.SegmentMinutes);
        Assert.Equal(DeviceStatus.Online, retrieved.Status);
    }

    [Fact]
    public async Task CameraRepo_GetAll_ReturnsAllRecordsSortedByName()
    {
        var repo = new SqliteCameraRepository(_tempDbPath);
        await repo.AddAsync(new CameraDevice { Id = "c2", Name = "Zebra Crossing", IpAddress = "192.168.1.20" });
        await repo.AddAsync(new CameraDevice { Id = "c1", Name = "Alpha Entrance", IpAddress = "192.168.1.10" });

        var all = await repo.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal("Alpha Entrance", all[0].Name);
        Assert.Equal("Zebra Crossing", all[1].Name);
    }

    [Fact]
    public async Task CameraRepo_Update_UpdatesFieldsCorrectly()
    {
        var repo = new SqliteCameraRepository(_tempDbPath);
        var cam = new CameraDevice { Id = "c1", Name = "Before Update", IpAddress = "192.168.1.1" };
        await repo.AddAsync(cam);

        cam.Name = "After Update";
        cam.IpAddress = "192.168.1.2";
        cam.Status = DeviceStatus.Offline;
        await repo.UpdateAsync(cam);

        var updated = await repo.GetByIdAsync("c1");
        Assert.NotNull(updated);
        Assert.Equal("After Update", updated.Name);
        Assert.Equal("192.168.1.2", updated.IpAddress);
        Assert.Equal(DeviceStatus.Offline, updated.Status);
    }

    [Fact]
    public async Task CameraRepo_Delete_RemovesRecord()
    {
        var repo = new SqliteCameraRepository(_tempDbPath);
        await repo.AddAsync(new CameraDevice { Id = "to-delete", Name = "Temporary Cam", IpAddress = "10.0.0.1" });

        bool deleted = await repo.DeleteAsync("to-delete");
        Assert.True(deleted);

        var after = await repo.GetByIdAsync("to-delete");
        Assert.Null(after);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. SqliteStorageConfigRepository Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StorageConfigRepo_GetConfigAsync_CreatesDefaultIfNotPresent()
    {
        var repo = new SqliteStorageConfigRepository(_tempDbPath);
        var cfg = await repo.GetConfigAsync();

        Assert.NotNull(cfg);
        Assert.False(string.IsNullOrEmpty(cfg.RootStoragePath));
        Assert.True(cfg.SegmentDurationMinutes > 0);
        Assert.True(cfg.MinFreeSpaceGigabytes > 0);
    }

    [Fact]
    public async Task StorageConfigRepo_SaveAndGetConfig_PersistsValues()
    {
        var repo = new SqliteStorageConfigRepository(_tempDbPath);
        var newCfg = new StorageConfig
        {
            RootStoragePath = @"D:\CMS_VideoRecordings",
            SegmentDurationMinutes = 60,
            MinFreeSpaceGigabytes = 50,
            RetentionDays = 90,
            AutoOverwriteOldest = false
        };

        bool saved = await repo.SaveConfigAsync(newCfg);
        Assert.True(saved);

        var retrieved = await repo.GetConfigAsync();
        Assert.Equal(@"D:\CMS_VideoRecordings", retrieved.RootStoragePath);
        Assert.Equal(60, retrieved.SegmentDurationMinutes);
        Assert.Equal(50, retrieved.MinFreeSpaceGigabytes);
        Assert.Equal(90, retrieved.RetentionDays);
        Assert.False(retrieved.AutoOverwriteOldest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. SqliteVideoSegmentRepository Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task VideoSegmentRepo_AddAndQueryByTimeRange_ReturnsMatchingSegments()
    {
        var repo = new SqliteVideoSegmentRepository(_tempDbPath);
        var now = DateTime.UtcNow;

        var seg1 = new VideoSegment
        {
            Id = "seg-01",
            CameraId = "cam-01",
            CameraName = "Cam 1",
            StartTime = now.AddMinutes(-30),
            EndTime = now.AddMinutes(-15),
            FilePath = @"C:\recordings\seg01.mp4",
            FileSizeBytes = 104857600, // 100 MB
            RecordingType = RecordingMode.Continuous247,
            StreamType = StreamType.MainStream,
            IsLocked = false
        };

        var seg2 = new VideoSegment
        {
            Id = "seg-02",
            CameraId = "cam-01",
            CameraName = "Cam 1",
            StartTime = now.AddMinutes(-15),
            EndTime = now,
            FilePath = @"C:\recordings\seg02.mp4",
            FileSizeBytes = 104857600,
            RecordingType = RecordingMode.MotionTriggered,
            StreamType = StreamType.MainStream,
            IsLocked = true
        };

        await repo.AddSegmentAsync(seg1);
        await repo.AddSegmentAsync(seg2);

        // Query during seg1 only
        var query = await repo.GetSegmentsAsync("cam-01", now.AddMinutes(-40), now.AddMinutes(-20));
        Assert.Single(query);
        Assert.Equal("seg-01", query[0].Id);

        // Query full range
        var queryFull = await repo.GetSegmentsAsync("cam-01", now.AddMinutes(-60), now);
        Assert.Equal(2, queryFull.Count);

        // Total storage used
        long totalBytes = await repo.GetTotalStorageUsedBytesAsync();
        Assert.Equal(209715200, totalBytes);
    }

    [Fact]
    public async Task VideoSegmentRepo_GetOldestUnlockedSegments_RespectsLockedStatus()
    {
        var repo = new SqliteVideoSegmentRepository(_tempDbPath);
        var now = DateTime.UtcNow;

        // Oldest segment is LOCKED (e.g., marked as evidence)
        await repo.AddSegmentAsync(new VideoSegment
        {
            Id = "seg-locked",
            CameraId = "cam-01",
            StartTime = now.AddHours(-10),
            EndTime = now.AddHours(-9),
            FilePath = "locked.mp4",
            FileSizeBytes = 1000,
            IsLocked = true
        });

        // Unlocked newer segment
        await repo.AddSegmentAsync(new VideoSegment
        {
            Id = "seg-unlocked",
            CameraId = "cam-01",
            StartTime = now.AddHours(-8),
            EndTime = now.AddHours(-7),
            FilePath = "unlocked.mp4",
            FileSizeBytes = 2000,
            IsLocked = false
        });

        var oldest = await repo.GetOldestUnlockedSegmentsAsync(5);
        Assert.Single(oldest);
        Assert.Equal("seg-unlocked", oldest[0].Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. SqliteAlarmRepository Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AlarmRepo_AddAndGetRecentAlarms_ReturnsAlarmsInDescOrder()
    {
        var repo = new SqliteAlarmRepository(_tempDbPath);
        var t1 = DateTime.UtcNow.AddMinutes(-5);
        var t2 = DateTime.UtcNow;

        await repo.AddAlarmAsync(new AlarmEvent
        {
            Id = "alarm-1",
            CameraId = "cam-01",
            CameraName = "Cam 1",
            EventType = AlarmEventType.MotionDetected,
            Level = AlarmLevel.Warning,
            Timestamp = t1,
            Description = "Motion detected in zone 1"
        });

        await repo.AddAlarmAsync(new AlarmEvent
        {
            Id = "alarm-2",
            CameraId = "cam-01",
            CameraName = "Cam 1",
            EventType = AlarmEventType.Intrusion,
            Level = AlarmLevel.Critical,
            Timestamp = t2,
            Description = "Intrusion detected"
        });

        var recent = await repo.GetRecentAlarmsAsync(10);
        Assert.Equal(2, recent.Count);
        Assert.Equal("alarm-2", recent[0].Id); // Most recent first
        Assert.Equal("alarm-1", recent[1].Id);
        Assert.Equal(AlarmEventType.Intrusion, recent[0].EventType);
        Assert.Equal(AlarmLevel.Critical, recent[0].Level);
    }
}
