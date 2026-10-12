using CMS.Core.Entities;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using System.Collections.Concurrent;

namespace CMS.MediaServer.Storage;

public class StorageManager : IStorageEngine, IDisposable
{
    private StorageConfig _config;
    private readonly IVideoSegmentRepository _segmentRepo;
    private readonly ConcurrentDictionary<string, ZeroTranscodeRecorder> _recorders = new();
    private readonly Timer _cleanupTimer;
    private bool _isDisposed;

    public StorageConfig Configuration => _config;

    public StorageManager(StorageConfig? config = null, IVideoSegmentRepository? segmentRepo = null)
    {
        _config = config ?? new StorageConfig();
        _segmentRepo = segmentRepo ?? new SqliteVideoSegmentRepository();

        Directory.CreateDirectory(_config.RootStoragePath);

        // Chạy kiểm tra dung lượng ổ đĩa và dọn dẹp FIFO định kỳ mỗi 5 phút
        _cleanupTimer = new Timer(async _ => await RunDiskCleanupAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
    }

    public async Task<bool> StartRecordingAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        if (_recorders.TryGetValue(camera.Id, out var existing) && existing.IsRecording)
        {
            return true;
        }

        var recorder = new ZeroTranscodeRecorder(camera, _config, _segmentRepo);
        bool started = await recorder.StartAsync(cancellationToken);

        if (started)
        {
            _recorders[camera.Id] = recorder;
            return true;
        }

        return false;
    }

    public async Task StopRecordingAsync(string cameraId)
    {
        if (_recorders.TryRemove(cameraId, out var recorder))
        {
            await recorder.StopAsync();
            recorder.Dispose();
        }
    }

    public bool IsRecording(string cameraId)
    {
        return _recorders.TryGetValue(cameraId, out var recorder) && recorder.IsRecording;
    }

    public IEnumerable<string> GetActiveRecordingCameraIds()
    {
        return _recorders.Where(kv => kv.Value.IsRecording).Select(kv => kv.Key).ToList();
    }

    public async Task StopAllRecordingAsync()
    {
        var tasks = _recorders.Values.Select(r => r.StopAsync());
        await Task.WhenAll(tasks);
        _recorders.Clear();
    }

    public void UpdateConfiguration(StorageConfig config)
    {
        _config = config;
        Directory.CreateDirectory(_config.RootStoragePath);
    }

    private async Task RunDiskCleanupAsync()
    {
        if (_isDisposed) return;

        try
        {
            string rootPath = Path.GetFullPath(_config.RootStoragePath);
            string? driveLetter = Path.GetPathRoot(rootPath);
            if (string.IsNullOrEmpty(driveLetter)) return;

            var drive = new DriveInfo(driveLetter);
            long minFreeBytes = (long)_config.MinFreeSpaceGigabytes * 1024 * 1024 * 1024;

            // 1. Kiểm tra dung lượng trống để kích hoạt ghi đè FIFO
            if (_config.AutoOverwriteOldest && drive.AvailableFreeSpace < minFreeBytes)
            {
                // Cần giải phóng dung lượng: lấy 20 file cũ nhất mỗi vòng lặp
                var oldestSegments = await _segmentRepo.GetOldestUnlockedSegmentsAsync(20);

                foreach (var seg in oldestSegments)
                {
                    try
                    {
                        if (File.Exists(seg.FilePath))
                        {
                            File.Delete(seg.FilePath);
                        }
                    }
                    catch
                    {
                    }

                    await _segmentRepo.DeleteSegmentAsync(seg.Id);

                    // Kiểm tra lại sau khi xóa
                    drive = new DriveInfo(driveLetter);
                    if (drive.AvailableFreeSpace >= minFreeBytes)
                    {
                        break;
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi kiểm tra dung lượng ổ đĩa tạm thời
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cleanupTimer.Dispose();
        _ = StopAllRecordingAsync();
    }
}
