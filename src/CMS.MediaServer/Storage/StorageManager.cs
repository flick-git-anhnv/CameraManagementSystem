using CMS.Core.Entities;
using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Infrastructure.Data;
using System.Collections.Concurrent;

namespace CMS.MediaServer.Storage;

public class StorageManager : IStorageEngine, IDisposable
{
    private StorageConfig _config;
    private readonly IVideoSegmentRepository _segmentRepo;
    private readonly ConcurrentDictionary<string, ZeroTranscodeRecorder> _recorders = new();
    private readonly ConcurrentDictionary<string, CameraDevice> _cameraPolicies = new();
    private readonly Timer _cleanupTimer;
    private readonly Timer _scheduleTimer;
    private bool _isDisposed;

    public StorageConfig Configuration => _config;

    public StorageManager(StorageConfig? config = null, IVideoSegmentRepository? segmentRepo = null)
    {
        if (config != null)
        {
            _config = config;
        }
        else
        {
            try
            {
                _config = new SqliteStorageConfigRepository().GetConfigAsync().GetAwaiter().GetResult();
            }
            catch
            {
                _config = new StorageConfig();
            }
        }
        _segmentRepo = segmentRepo ?? new SqliteVideoSegmentRepository();

        Directory.CreateDirectory(_config.RootStoragePath);

        // Chạy kiểm tra dung lượng ổ đĩa và dọn dẹp FIFO định kỳ mỗi 5 phút
        _cleanupTimer = new Timer(async _ => await RunDiskCleanupAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

        // Kiểm tra lịch trình ghi hình định kỳ mỗi 30 giây
        _scheduleTimer = new Timer(async _ => await EvaluateScheduledPoliciesAsync(), null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
    }

    public async Task<bool> StartRecordingAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        _cameraPolicies[camera.Id] = camera;

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

    /// <summary>
    /// Áp dụng ngay lập tức chế độ ghi hình của danh sách camera (kích hoạt hoặc dừng dịch vụ ghi hình nền)
    /// </summary>
    public async Task<(int ActiveCount, int StoppedCount)> ApplyCameraPoliciesAsync(
        IEnumerable<CameraDevice> cameras,
        CancellationToken cancellationToken = default)
    {
        int activeCount = 0;
        int stoppedCount = 0;
        DateTime now = DateTime.Now;

        foreach (var camera in cameras)
        {
            _cameraPolicies[camera.Id] = camera;
            bool shouldRecord = ShouldCameraRecordNow(camera, now);

            if (shouldRecord)
            {
                if (_recorders.TryGetValue(camera.Id, out var existing) && existing.IsRecording)
                {
                    // Nếu thay đổi luồng chính/phụ hoặc chế độ ghi hình thì khởi động lại tiến trình ghi
                    if (existing.Camera.RecordSubStream != camera.RecordSubStream ||
                        existing.Camera.RecordMode != camera.RecordMode)
                    {
                        await StopRecordingAsync(camera.Id);
                        if (await StartRecordingAsync(camera, cancellationToken))
                        {
                            activeCount++;
                        }
                    }
                    else
                    {
                        activeCount++;
                    }
                }
                else
                {
                    if (await StartRecordingAsync(camera, cancellationToken))
                    {
                        activeCount++;
                    }
                }
            }
            else
            {
                if (IsRecording(camera.Id))
                {
                    await StopRecordingAsync(camera.Id);
                }
                stoppedCount++;
            }
        }

        return (activeCount, stoppedCount);
    }

    public static bool ShouldCameraRecordNow(CameraDevice camera, DateTime now)
    {
        return camera.RecordMode switch
        {
            RecordingMode.None => false,
            RecordingMode.Continuous247 => true,
            RecordingMode.Schedule => IsWithinSchedule(camera, now),
            RecordingMode.MotionTriggered => true,
            RecordingMode.AiTriggered => true,
            _ => false
        };
    }

    public static bool IsWithinSchedule(CameraDevice camera, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(camera.ScheduleDays))
            return false;

        var dayTokens = camera.ScheduleDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int currentDay = (int)now.DayOfWeek;
        bool dayMatched = dayTokens.Any(t => int.TryParse(t, out int d) && d == currentDay);
        if (!dayMatched)
            return false;

        if (!TimeSpan.TryParse(camera.ScheduleStartTime, out var start))
            start = new TimeSpan(7, 0, 0);
        if (!TimeSpan.TryParse(camera.ScheduleEndTime, out var end))
            end = new TimeSpan(19, 0, 0);

        var currentTime = new TimeSpan(now.Hour, now.Minute, 0);

        if (start <= end)
        {
            return currentTime >= start && currentTime <= end;
        }
        else
        {
            // Khung giờ qua đêm (ví dụ 22:00 -> 06:00 sáng hôm sau)
            return currentTime >= start || currentTime <= end;
        }
    }

    private async Task EvaluateScheduledPoliciesAsync()
    {
        if (_isDisposed || _cameraPolicies.IsEmpty) return;

        try
        {
            DateTime now = DateTime.Now;
            foreach (var camera in _cameraPolicies.Values)
            {
                if (camera.RecordMode != RecordingMode.Schedule)
                    continue;

                bool shouldRecord = IsWithinSchedule(camera, now);
                bool currentlyRecording = IsRecording(camera.Id);

                if (shouldRecord && !currentlyRecording)
                {
                    await StartRecordingAsync(camera);
                }
                else if (!shouldRecord && currentlyRecording)
                {
                    await StopRecordingAsync(camera.Id);
                }
            }
        }
        catch
        {
        }
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
        _scheduleTimer.Dispose();
        _ = StopAllRecordingAsync();
    }
}
