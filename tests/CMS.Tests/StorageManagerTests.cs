using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Infrastructure.Data;
using CMS.MediaServer.Storage;
using Xunit;

namespace CMS.Tests;

public class StorageManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempDbPath;

    public StorageManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cms_storage_test_{Guid.NewGuid():N}");
        _tempDbPath = Path.Combine(_tempDir, "test_repo.db");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void StorageManager_InitializesAndCreatesStorageDirectory()
    {
        var config = new StorageConfig
        {
            RootStoragePath = Path.Combine(_tempDir, "recordings"),
            SegmentDurationMinutes = 15,
            MinFreeSpaceGigabytes = 20,
            RetentionDays = 30,
            AutoOverwriteOldest = true
        };

        var segmentRepo = new SqliteVideoSegmentRepository(_tempDbPath);
        using var manager = new StorageManager(config, segmentRepo);

        Assert.True(Directory.Exists(config.RootStoragePath));
        Assert.Equal(15, manager.Configuration.SegmentDurationMinutes);
        Assert.Empty(manager.GetActiveRecordingCameraIds());
    }

    [Fact]
    public void StorageManager_UpdateConfiguration_UpdatesProperties()
    {
        var config = new StorageConfig { RootStoragePath = Path.Combine(_tempDir, "rec1") };
        var segmentRepo = new SqliteVideoSegmentRepository(_tempDbPath);
        using var manager = new StorageManager(config, segmentRepo);

        var newConfig = new StorageConfig
        {
            RootStoragePath = Path.Combine(_tempDir, "rec2"),
            SegmentDurationMinutes = 60
        };

        manager.UpdateConfiguration(newConfig);

        Assert.Equal(Path.Combine(_tempDir, "rec2"), manager.Configuration.RootStoragePath);
        Assert.Equal(60, manager.Configuration.SegmentDurationMinutes);
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "rec2")));
    }

    [Fact]
    public async Task StorageManager_NonExistentCameraStop_DoesNotThrow()
    {
        var config = new StorageConfig { RootStoragePath = Path.Combine(_tempDir, "recordings") };
        var segmentRepo = new SqliteVideoSegmentRepository(_tempDbPath);
        using var manager = new StorageManager(config, segmentRepo);

        // Stopping an unstarted camera must not throw
        await manager.StopRecordingAsync("non-existent-camera-id");
        Assert.False(manager.IsRecording("non-existent-camera-id"));
    }

    [Fact]
    public void ZeroTranscodeRecorder_InstantiatesWithValidProperties()
    {
        var cam = new CameraDevice
        {
            Id = "cam-99",
            Name = "Test Cam 99",
            IpAddress = "192.168.1.99",
            Brand = CameraBrand.Hikvision
        };

        var config = new StorageConfig
        {
            RootStoragePath = Path.Combine(_tempDir, "recordings"),
            SegmentDurationMinutes = 30
        };

        var repo = new SqliteVideoSegmentRepository(_tempDbPath);
        using var recorder = new ZeroTranscodeRecorder(cam, config, repo);

        Assert.Equal("cam-99", recorder.CameraId);
        Assert.False(recorder.IsRecording);
    }
}
